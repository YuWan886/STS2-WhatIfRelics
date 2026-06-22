using STS2RitsuLib.Settings;

namespace WhatIfRelics.WhatIfRelicsCode.Localization;

internal static class WhatIfRelicsLocalization
{
    public const string UiTable = "settings_ui";

    public static ModSettingsText ModTitleText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.mod_title", "What If Relics");

    public static ModSettingsText SettingsPageTitleText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.page.title", "What If Relics");

    public static ModSettingsText GeneralSectionTitleText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.section.general.title", "General");

    public static ModSettingsText EnableAtStartText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.enable_at_start", "Enable start-of-run What If selection");

    public static ModSettingsText ResetSectionTitleText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.section.reset.title", "Reset");

    public static ModSettingsText ResetButtonLabelText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.reset.label", "Restore Defaults");

    public static ModSettingsText ResetButtonText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.reset.button", "Reset");

    public static ModSettingsText ResetButtonDescriptionText() =>
        ModSettingsText.LocString(
            UiTable,
            "whatifrelics.settings.reset.description",
            "Reset all What If Relics settings back to their default values.");
}
