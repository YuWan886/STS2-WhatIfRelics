using System.Text;
using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace WhatIfRelics.WhatIfRelicsCode.Assets;

internal static class WhatIfAssetPathHelper
{
    public const string PlaceholderRelicIconPath = "res://WhatIfRelics/images/relics/what_if_placeholder.png";

    public static string BuildSnakeCasePngFileName(Type assetOwnerType)
    {
        return $"{ToSnakeCase(assetOwnerType.Name)}.png";
    }

    public static string BuildAutoImagePath(Type assetOwnerType, string imageRoot)
    {
        return $"{imageRoot}/{BuildSnakeCasePngFileName(assetOwnerType)}";
    }

    public static RelicAssetProfile BuildRelicAssetProfile(Type relicType, string imageRoot)
    {
        string iconPath = ResolveExistingPath(
            BuildAutoImagePath(relicType, imageRoot),
            PlaceholderRelicIconPath);
        return new RelicAssetProfile(
            IconPath: iconPath,
            IconOutlinePath: iconPath,
            BigIconPath: iconPath);
    }

    public static string ResolveExistingPath(string path, string fallbackPath)
    {
        return ResourceLoader.Exists(path) ? path : fallbackPath;
    }

    public static string? ResolveExistingPathOrNull(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && ResourceLoader.Exists(path) ? path : null;
    }

    public static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (char.IsUpper(current))
            {
                if (i > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
