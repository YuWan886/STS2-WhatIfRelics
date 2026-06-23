using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Entities.Players;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfGambleGod")]
public class WhatIfGambleGod : WhatIfUniformCardRelicModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        ..HoverTipFactory.FromRelic<SneckoEye>()
    ];

    public WhatIfGambleGod() : base()
    {
    }

    protected override bool Matches(CardModel card)
    {
        return !card.EnergyCost.CostsX && card.EnergyCost.Canonical >= 3;
    }

    protected override bool PreferCharacterPoolCandidates(Player player)
    {
        return false;
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
        {
            return;
        }

        if (Owner.GetRelicById(ModelDb.Relic<SneckoEye>().Id) != null)
        {
            return;
        }

        await RelicCmd.Obtain(ModelDb.Relic<SneckoEye>().ToMutable(), Owner);
    }
}
