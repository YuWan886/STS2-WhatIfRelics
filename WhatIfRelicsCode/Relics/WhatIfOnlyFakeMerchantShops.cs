using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfOnlyFakeMerchantShops")]
public class WhatIfOnlyFakeMerchantShops : WhatIfRelicModel
{
    public WhatIfOnlyFakeMerchantShops() : base(true)
    {
    }

    public static bool HasFakeMerchantShops(IRunState? runState)
    {
        return runState?.Players.Any(player => player.Relics.Any(relic => relic is WhatIfOnlyFakeMerchantShops)) == true;
    }
}
