using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfRandomEncounters")]
public class WhatIfRandomEncounters : WhatIfRelicModel
{
    public WhatIfRandomEncounters() : base(true)
    {
    }

    public static bool HasRandomEncounters(IRunState? runState)
    {
        return runState?.Players.Any(player => player.Relics.Any(relic => relic is WhatIfRandomEncounters)) == true;
    }
}
