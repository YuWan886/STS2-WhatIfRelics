namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfCheatDraw")]
public class WhatIfCheatDraw : WhatIfRelicModel
{
    public WhatIfCheatDraw() : base(true)
    {
    }
}
