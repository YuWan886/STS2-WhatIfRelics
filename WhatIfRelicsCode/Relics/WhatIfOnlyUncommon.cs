using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfOnlyUncommon")]
public class WhatIfOnlyUncommon : WhatIfUniformCardRelicModel
{
    protected override bool Matches(CardModel card) => card.Rarity == CardRarity.Uncommon;
}




