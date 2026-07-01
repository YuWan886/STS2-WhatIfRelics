namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfGoSecond")]
public class WhatIfGoSecond : WhatIfRelicModel
{
    public WhatIfGoSecond() : base(true)
    {
    }
}
