using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

public abstract class WhatIfRelicModel : RelicModel
{
    private const string PlaceholderIconPath = "res://WhatIfRelics/images/relics/what_if_placeholder.png";

    public sealed override RelicRarity Rarity => RelicRarity.Event;

    public override int MerchantCost => 999999999;

    public override bool IsAllowedInShops => false;

    protected override string BigIconPath => PlaceholderIconPath;

    public override string PackedIconPath => PlaceholderIconPath;

    protected override string PackedIconOutlinePath => PlaceholderIconPath;

    protected WhatIfRelicModel()
    {
    }

    protected WhatIfRelicModel(bool autoAdd) : this()
    {
    }
}




