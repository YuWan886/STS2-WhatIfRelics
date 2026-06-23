using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfWiderRandomMap")]
public class WhatIfWiderRandomMap : WhatIfRelicModel
{
    public WhatIfWiderRandomMap() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await RunManager.Instance.GenerateMap();
    }

    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        if (!runState.Players.Any(player => player.Relics.Any(relic => relic is WhatIfWiderRandomMap)))
        {
            return map;
        }

        return new RandomizedWiderActMap(runState, map);
    }
}
