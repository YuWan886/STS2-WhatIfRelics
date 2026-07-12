namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfCardsDoNotExhaust")]
public sealed class WhatIfCardsDoNotExhaust : WhatIfRelicModel
{
    public WhatIfCardsDoNotExhaust() : base(true)
    {
    }
}
