using STS2RitsuLib.Interop.AutoRegistration;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfGoSecond")]
public class WhatIfGoSecond : WhatIfRelicModel
{
    public WhatIfGoSecond() : base(true)
    {
    }
}
