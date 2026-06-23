using STS2RitsuLib.Interop.AutoRegistration;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfCheatDraw")]
public class WhatIfCheatDraw : WhatIfRelicModel
{
    public WhatIfCheatDraw() : base(true)
    {
    }
}
