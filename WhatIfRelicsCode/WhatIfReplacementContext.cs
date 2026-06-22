using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Networking;

namespace WhatIfRelics.WhatIfRelicsCode;

internal static class WhatIfReplacementContext
{
    public static bool IsWhatIfSelectionEnabled()
    {
        return WhatIfRelicsConfigSync.EffectiveEnableWhatIfRelics();
    }

    public static bool ShouldReplaceStartingDeck()
    {
        return WhatIfRelicsConfigSync.EffectiveReplaceStartingDeck();
    }

    public static bool ShouldReplaceCardRewards(CardCreationSource source)
    {
        return source == CardCreationSource.Shop
            ? WhatIfRelicsConfigSync.EffectiveReplaceShopCards()
            : WhatIfRelicsConfigSync.EffectiveReplaceCardRewards();
    }

    public static bool ShouldReplaceRelicRewards(AbstractRoom? room)
    {
        return WhatIfRelicsConfigSync.EffectiveReplaceRelicRewards();
    }

    public static bool ShouldReplacePotionRewards()
    {
        return WhatIfRelicsConfigSync.EffectiveReplacePotionRewards();
    }

    public static bool ShouldReplaceTreasureRelics()
    {
        return WhatIfRelicsConfigSync.EffectiveReplaceTreasureRelics();
    }

    public static bool ShouldReplaceShopRelics()
    {
        return WhatIfRelicsConfigSync.EffectiveReplaceShopRelics();
    }

    public static bool ShouldReplaceShopPotions()
    {
        return WhatIfRelicsConfigSync.EffectiveReplaceShopPotions();
    }
}
