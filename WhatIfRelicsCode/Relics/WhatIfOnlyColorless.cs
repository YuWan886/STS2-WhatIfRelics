using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfOnlyColorless")]
public class WhatIfOnlyColorless : WhatIfUniformCardRelicModel
{
    protected override bool Matches(CardModel card) =>
        card.VisualCardPool.IsColorless
        && card.Type is CardType.Attack or CardType.Skill or CardType.Power;
}




