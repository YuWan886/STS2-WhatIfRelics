using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfFreeShopping")]
public sealed class WhatIfFreeShopping : WhatIfRelicModel
{
    public WhatIfFreeShopping() : base(true)
    {
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (player != Owner)
        {
            return originalPrice;
        }

        return entry is MerchantCardEntry or MerchantRelicEntry or MerchantPotionEntry or MerchantCardRemovalEntry
            ? 0m
            : originalPrice;
    }
}
