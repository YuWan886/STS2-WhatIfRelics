using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfOnlyOnePath")]
public class WhatIfOnlyOnePath : WhatIfRelicModel
{
    public WhatIfOnlyOnePath() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await RunManager.Instance.GenerateMap();
    }

    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        if (!runState.Players.Any(player => player.Relics.Any(relic => relic is WhatIfOnlyOnePath)))
        {
            return map;
        }

        return new GoldenPathActMap(runState);
    }
}
