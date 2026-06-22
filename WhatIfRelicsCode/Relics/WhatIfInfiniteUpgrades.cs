using STS2RitsuLib.Interop.AutoRegistration;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfInfiniteUpgrades")]
public class WhatIfInfiniteUpgrades : WhatIfRelicModel
{
    public WhatIfInfiniteUpgrades() : base(true)
    {
    }
}




