namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfSellCards")]
public class WhatIfSellCards : WhatIfRelicModel
{
    public WhatIfSellCards() : base(true)
    {
    }
}
