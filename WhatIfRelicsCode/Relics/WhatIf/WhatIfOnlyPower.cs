using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfOnlyPower")]
public class WhatIfOnlyPower : WhatIfUniformCardRelicModel
{
    protected override bool Matches(CardModel card) => card.Type == CardType.Power;
}




