namespace WhatIfRelics.WhatIfRelicsCode;

public sealed class WhatIfRelicsSettings
{
    public const int DefaultStartingWhatIfRelicChoiceCount = 3;
    public const int MinStartingWhatIfRelicChoiceCount = 0;
    public const int MaxStartingWhatIfRelicChoiceCount = 10;
    public const int DefaultScorchingSpireFloorInterval = 10;

    public bool EnableWhatIfRelics { get; set; } = false;

    public int StartingWhatIfRelicChoiceCount { get; set; } = DefaultStartingWhatIfRelicChoiceCount;

    public bool ReplaceStartingDeck { get; set; } = true;

    public bool ReplaceCardRewards { get; set; } = true;

    public bool ReplaceRelicRewards { get; set; } = true;

    public bool ReplacePotionRewards { get; set; } = true;

    public bool ReplaceTreasureRelics { get; set; } = true;

    public bool ReplaceShopCards { get; set; } = true;

    public bool ReplaceShopRelics { get; set; } = true;

    public bool ReplaceShopPotions { get; set; } = true;

    public int ScorchingSpireFloorInterval { get; set; } = DefaultScorchingSpireFloorInterval;

    public void ResetToDefaults()
    {
        WhatIfRelicsSettings defaults = new();
        EnableWhatIfRelics = defaults.EnableWhatIfRelics;
        StartingWhatIfRelicChoiceCount = defaults.StartingWhatIfRelicChoiceCount;
        ReplaceStartingDeck = defaults.ReplaceStartingDeck;
        ReplaceCardRewards = defaults.ReplaceCardRewards;
        ReplaceRelicRewards = defaults.ReplaceRelicRewards;
        ReplacePotionRewards = defaults.ReplacePotionRewards;
        ReplaceTreasureRelics = defaults.ReplaceTreasureRelics;
        ReplaceShopCards = defaults.ReplaceShopCards;
        ReplaceShopRelics = defaults.ReplaceShopRelics;
        ReplaceShopPotions = defaults.ReplaceShopPotions;
        ScorchingSpireFloorInterval = defaults.ScorchingSpireFloorInterval;
    }
}
