using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using WhatIfRelics.WhatIfRelicsCode.Localization;

namespace WhatIfRelics.WhatIfRelicsCode;

public static class WhatIfRelicsSettingsPage
{
    public const string DataKey = "settings";
    private const int MinScorchingSpireFloorInterval = 1;
    private const int MaxScorchingSpireFloorInterval = 50;

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, bool> EnableWhatIfRelicsBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.EnableWhatIfRelics,
        static (s, v) => s.EnableWhatIfRelics = v);

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, int> StartingWhatIfRelicChoiceCountBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.StartingWhatIfRelicChoiceCount,
        static (s, v) => s.StartingWhatIfRelicChoiceCount = Math.Clamp(
            v,
            WhatIfRelicsSettings.MinStartingWhatIfRelicChoiceCount,
            WhatIfRelicsSettings.MaxStartingWhatIfRelicChoiceCount));

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, bool> ReplaceStartingDeckBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.ReplaceStartingDeck,
        static (s, v) => s.ReplaceStartingDeck = v);

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, bool> ReplaceCardRewardsBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.ReplaceCardRewards,
        static (s, v) => s.ReplaceCardRewards = v);

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, bool> ReplaceRelicRewardsBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.ReplaceRelicRewards,
        static (s, v) => s.ReplaceRelicRewards = v);

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, bool> ReplacePotionRewardsBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.ReplacePotionRewards,
        static (s, v) => s.ReplacePotionRewards = v);

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, bool> ReplaceTreasureRelicsBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.ReplaceTreasureRelics,
        static (s, v) => s.ReplaceTreasureRelics = v);

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, bool> ReplaceShopCardsBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.ReplaceShopCards,
        static (s, v) => s.ReplaceShopCards = v);

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, bool> ReplaceShopRelicsBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.ReplaceShopRelics,
        static (s, v) => s.ReplaceShopRelics = v);

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, bool> ReplaceShopPotionsBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.ReplaceShopPotions,
        static (s, v) => s.ReplaceShopPotions = v);

    private static readonly ModSettingsValueBinding<WhatIfRelicsSettings, int> ScorchingSpireFloorIntervalBinding = new(
        Entry.ModId,
        DataKey,
        SaveScope.Global,
        static s => s.ScorchingSpireFloorInterval,
        static (s, v) => s.ScorchingSpireFloorInterval = Math.Max(MinScorchingSpireFloorInterval, v));

    public static WhatIfRelicsSettings Current =>
        RitsuLibFramework.GetDataStore(Entry.ModId).Get<WhatIfRelicsSettings>(DataKey);

    public static void Register()
    {
        ModDataStore.For(Entry.ModId).Register(
            key: DataKey,
            fileName: "settings.json",
            scope: SaveScope.Global,
            defaultFactory: static () => new WhatIfRelicsSettings(),
            autoCreateIfMissing: true);

        RitsuLibFramework.RegisterModSettings(
            Entry.ModId,
            page => page
                .WithTitle(WhatIfRelicsLocalization.SettingsPageTitleText())
                .WithModDisplayName(WhatIfRelicsLocalization.ModTitleText())
                .WithVisibleOnHostSurfaces(ModSettingsHostSurface.All)
                .AddSection(
                    "general",
                    section => section
                        .WithTitle(WhatIfRelicsLocalization.GeneralSectionTitleText())
                        .AddToggle(
                            "enable_what_if_relics",
                            WhatIfRelicsLocalization.EnableAtStartText(),
                            EnableWhatIfRelicsBinding)
                        .AddIntSlider(
                            "starting_what_if_relic_choice_count",
                            WhatIfRelicsLocalization.StartingWhatIfRelicChoiceCountText(),
                            StartingWhatIfRelicChoiceCountBinding,
                            WhatIfRelicsSettings.MinStartingWhatIfRelicChoiceCount,
                            WhatIfRelicsSettings.MaxStartingWhatIfRelicChoiceCount,
                            1,
                            static value => value.ToString())
                        .AddIntSlider(
                            "scorching_spire_floor_interval",
                            WhatIfRelicsLocalization.ScorchingSpireFloorIntervalText(),
                            ScorchingSpireFloorIntervalBinding,
                            MinScorchingSpireFloorInterval,
                            MaxScorchingSpireFloorInterval,
                            1,
                            static value => value.ToString()))
                .AddSection(
                    "replacement_scope",
                    section => section
                        .WithTitle(WhatIfRelicsLocalization.ReplacementSectionTitleText())
                        .AddToggle(
                            "replace_starting_deck",
                            WhatIfRelicsLocalization.ReplaceStartingDeckText(),
                            ReplaceStartingDeckBinding)
                        .AddToggle(
                            "replace_card_rewards",
                            WhatIfRelicsLocalization.ReplaceCardRewardsText(),
                            ReplaceCardRewardsBinding)
                        .AddToggle(
                            "replace_relic_rewards",
                            WhatIfRelicsLocalization.ReplaceRelicRewardsText(),
                            ReplaceRelicRewardsBinding)
                        .AddToggle(
                            "replace_potion_rewards",
                            WhatIfRelicsLocalization.ReplacePotionRewardsText(),
                            ReplacePotionRewardsBinding)
                        .AddToggle(
                            "replace_treasure_relics",
                            WhatIfRelicsLocalization.ReplaceTreasureRelicsText(),
                            ReplaceTreasureRelicsBinding)
                        .AddToggle(
                            "replace_shop_cards",
                            WhatIfRelicsLocalization.ReplaceShopCardsText(),
                            ReplaceShopCardsBinding)
                        .AddToggle(
                            "replace_shop_relics",
                            WhatIfRelicsLocalization.ReplaceShopRelicsText(),
                            ReplaceShopRelicsBinding)
                        .AddToggle(
                            "replace_shop_potions",
                            WhatIfRelicsLocalization.ReplaceShopPotionsText(),
                            ReplaceShopPotionsBinding))
                .AddSection(
                    "reset",
                    section => section
                        .WithTitle(WhatIfRelicsLocalization.ResetSectionTitleText())
                        .AddButton(
                            "reset_defaults",
                            WhatIfRelicsLocalization.ResetButtonLabelText(),
                            WhatIfRelicsLocalization.ResetButtonText(),
                            ResetToDefaults,
                            ModSettingsButtonTone.Danger,
                            WhatIfRelicsLocalization.ResetButtonDescriptionText())));
    }

    private static void ResetToDefaults(IModSettingsUiActionHost host)
    {
        Current.ResetToDefaults();
        ModDataStore.For(Entry.ModId).Save(DataKey);
        host.RequestRefreshAfterDataModelBatchChange();
    }
}
