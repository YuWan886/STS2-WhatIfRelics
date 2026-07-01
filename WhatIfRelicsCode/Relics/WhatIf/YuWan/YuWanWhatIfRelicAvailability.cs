using WhatIfRelics.WhatIfRelicsCode.Interop;

namespace WhatIfRelics.WhatIfRelicsCode.Relics.YuWan;

public static class YuWanWhatIfRelicAvailability
{
    public static bool IsInteropAssemblyAvailable()
    {
        return YuWanInterop.IsAvailable();
    }

    public static bool CanRegister(Type relicType)
    {
        if (!IsInteropAssemblyAvailable())
        {
            return false;
        }

        if (relicType == typeof(WhatIfSha))
        {
            return !string.IsNullOrWhiteSpace(YuWanInterop.GetShaCardEntry());
        }

        if (relicType == typeof(WhatIfDirectWin))
        {
            return !string.IsNullOrWhiteSpace(YuWanInterop.GetSadArmyWinCardEntry());
        }

        if (relicType == typeof(WhatIfHeartsteel))
        {
            return !string.IsNullOrWhiteSpace(YuWanInterop.GetHeartsteelRelicEntry());
        }

        if (relicType == typeof(WhatIfTenYearBamboo))
        {
            return !string.IsNullOrWhiteSpace(YuWanInterop.GetTenYearBambooRelicEntry());
        }

        if (relicType == typeof(WhatIfTriplePlay))
        {
            return !string.IsNullOrWhiteSpace(YuWanInterop.GetTriplePlayRelicEntry());
        }

        if (relicType == typeof(WhatIfSeriesRelics))
        {
            return YuWanInterop.GetSeriesRelicEntries().Length > 0;
        }

        return true;
    }
}
