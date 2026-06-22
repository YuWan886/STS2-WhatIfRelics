using STS2RitsuLib.Interop;

namespace WhatIfRelics.WhatIfRelicsCode.Interop;

[ModInterop("YuWanCard", "YuWanCard.WhatIfRelicsCode.Interop.YuWanWhatIfInterop")]
public static class YuWanInterop
{
    public static bool IsAvailable() => false;

    public static string[] GetRegisteredWhatIfRelicTypeNames() => [];

    public static string[] GetSupplementalWhatIfRelicTypeNames() => [];

    public static string? GetShaCardEntry() => null;

    public static string? GetSadArmyWinCardEntry() => null;

    public static string? GetHeartsteelRelicEntry() => null;

    public static string? GetTenYearBambooRelicEntry() => null;

    public static string? GetTriplePlayRelicEntry() => null;

    public static string[] GetSeriesRelicEntries() => [];
}


