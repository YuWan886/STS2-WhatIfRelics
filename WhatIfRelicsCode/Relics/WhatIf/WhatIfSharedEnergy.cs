namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfSharedEnergy")]
public sealed class WhatIfSharedEnergy : WhatIfRelicModel
{
    public WhatIfSharedEnergy() : base(true)
    {
    }
}
