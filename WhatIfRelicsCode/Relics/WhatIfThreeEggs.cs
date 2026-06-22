using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfThreeEggs")]
public class WhatIfThreeEggs : WhatIfRelicModel
{
    public WhatIfThreeEggs() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
        {
            return;
        }

        RelicModel[] eggRelics =
        [
            ModelDb.Relic<MoltenEgg>(),
            ModelDb.Relic<ToxicEgg>(),
            ModelDb.Relic<FrozenEgg>()
        ];

        foreach (RelicModel eggRelic in eggRelics)
        {
            if (Owner.GetRelicById(eggRelic.Id) != null)
            {
                continue;
            }

            await RelicCmd.Obtain(eggRelic.ToMutable(), Owner);
        }
    }
}




