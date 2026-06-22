using STS2RitsuLib.Interop.AutoRegistration;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfSellCards")]
public class WhatIfSellCards : WhatIfRelicModel
{
    public WhatIfSellCards() : base(true)
    {
    }
}
