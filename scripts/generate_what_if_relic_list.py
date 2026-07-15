"""Generate the Chinese What If relic list from registrations and localization."""

from __future__ import annotations

import argparse
import json
import os
import re
from dataclasses import dataclass
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parent.parent
RELIC_SOURCE_ROOT = PROJECT_ROOT / "WhatIfRelicsCode" / "Relics" / "WhatIf"
LOCALIZATION_PATH = PROJECT_ROOT / "WhatIfRelics" / "localization" / "zhs" / "relics.json"
ATTRIBUTE_PATTERN = re.compile(
    r"\[WhatIfRegisterRelic\((?P<arguments>.*?)\)\]", re.DOTALL
)
CLASS_PATTERN = re.compile(
    r"^\s*public\s+(?:(?:abstract|sealed|partial|static)\s+)*class\s+"
    r"(?P<name>[A-Za-z_]\w*)\b",
    re.MULTILINE,
)
STABLE_ENTRY_STEM_PATTERN = re.compile(
    r'StableEntryStem\s*=\s*"(?P<stem>[^"]+)"'
)
WORD_PATTERN = re.compile(r"[A-Z]+(?=[A-Z][a-z]|\d|$)|[A-Z]?[a-z]+|\d+")


@dataclass(frozen=True)
class RelicDocumentation:
    class_name: str
    class_path: str
    title: str
    description: str
    flavor: str


def to_localization_stem(value: str) -> str:
    return "_".join(word.upper() for word in WORD_PATTERN.findall(value))


def to_markdown_cell(value: str) -> str:
    if not value.strip():
        return "-"
    return value.replace("|", r"\|").replace("\r\n", "<br>").replace("\n", "<br>").replace("\r", "<br>")


def load_localization() -> dict[str, str]:
    if not LOCALIZATION_PATH.is_file():
        raise FileNotFoundError(f"Missing Chinese relic localization: {LOCALIZATION_PATH}")

    with LOCALIZATION_PATH.open(encoding="utf-8") as localization_file:
        return json.load(localization_file)


def find_relics(localization: dict[str, str], output_path: Path) -> list[RelicDocumentation]:
    relics: list[RelicDocumentation] = []

    for source_file in sorted(RELIC_SOURCE_ROOT.rglob("*.cs")):
        source = source_file.read_text(encoding="utf-8")
        for attribute_match in ATTRIBUTE_PATTERN.finditer(source):
            arguments = attribute_match.group("arguments")
            if not re.search(r"\btypeof\s*\(\s*WhatIfRelicPool\s*\)", arguments):
                continue

            stem_match = STABLE_ENTRY_STEM_PATTERN.search(arguments)
            if stem_match is None:
                raise ValueError(f"Missing StableEntryStem in {source_file}")

            class_match = CLASS_PATTERN.search(source, attribute_match.end())
            if class_match is None:
                raise ValueError(f"Missing relic class declaration in {source_file}")

            class_name = class_match.group("name")
            key_prefix = f"WHAT_IF_RELICS_RELIC_{to_localization_stem(stem_match.group('stem'))}"
            keys = {field: f"{key_prefix}.{field}" for field in ("title", "description", "flavor")}
            missing_keys = [key for key in keys.values() if key not in localization]
            if missing_keys:
                raise KeyError(f"{class_name} is missing localization keys: {', '.join(missing_keys)}")

            class_path = Path(os.path.relpath(source_file, output_path.parent)).as_posix()
            relics.append(
                RelicDocumentation(
                    class_name=class_name,
                    class_path=class_path,
                    title=localization[keys["title"]],
                    description=localization[keys["description"]],
                    flavor=localization[keys["flavor"]],
                )
            )

    if not relics:
        raise ValueError("No WhatIfRelicPool registrations were found.")

    return sorted(relics, key=lambda relic: relic.class_name)


def write_document(relics: list[RelicDocumentation], output_path: Path) -> None:
    lines = [
        "# 假如遗物列表",
        "",
        "> 此文件由 `scripts/generate_what_if_relic_list.py` 自动生成，请勿手动编辑。",
        "",
        f"共 {len(relics)} 个遗物。",
        "",
        "| 遗物 | 介绍 | 风味文本 | 类 |",
        "| --- | --- | --- | --- |",
    ]

    for relic in relics:
        lines.append(
            "| {title} | {description} | {flavor} | [{class_name}]({class_path}) |".format(
                title=to_markdown_cell(relic.title),
                description=to_markdown_cell(relic.description),
                flavor=to_markdown_cell(relic.flavor),
                class_name=relic.class_name,
                class_path=relic.class_path,
            )
        )

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output",
        type=Path,
        default=PROJECT_ROOT / "docs" / "what-if-relics.md",
        help="Markdown output path (default: %(default)s)",
    )
    return parser.parse_args()


def main() -> None:
    output_path = parse_args().output.resolve()
    relics = find_relics(load_localization(), output_path)
    write_document(relics, output_path)
    print(f"Generated {len(relics)} What If relics: {output_path}")


if __name__ == "__main__":
    main()
