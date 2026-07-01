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
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.enable_at_start", "Enable start-of-run What If relic options");

    public static ModSettingsText ScorchingSpireFloorIntervalText() =>
        ModSettingsText.LocString(
            UiTable,
            "whatifrelics.settings.scorching_spire_floor_interval",
            "Scorching Spire: floors per Heat stack");

    public static ModSettingsText ReplacementSectionTitleText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.section.replacement.title", "Replacement Scope");

    public static ModSettingsText ReplaceStartingDeckText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.replace_starting_deck", "Replace starting deck");

    public static ModSettingsText ReplaceCardRewardsText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.replace_card_rewards", "Replace card rewards");

    public static ModSettingsText ReplaceRelicRewardsText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.replace_relic_rewards", "Replace relic rewards");

    public static ModSettingsText ReplacePotionRewardsText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.replace_potion_rewards", "Replace potion rewards");

    public static ModSettingsText ReplaceTreasureRelicsText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.replace_treasure_relics", "Replace treasure relics");

    public static ModSettingsText ReplaceShopCardsText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.replace_shop_cards", "Replace shop cards");

    public static ModSettingsText ReplaceShopRelicsText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.replace_shop_relics", "Replace shop relics");

    public static ModSettingsText ReplaceShopPotionsText() =>
        ModSettingsText.LocString(UiTable, "whatifrelics.settings.replace_shop_potions", "Replace shop potions");

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
