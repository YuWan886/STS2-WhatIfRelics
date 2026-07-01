using System.Reflection;

namespace WhatIfRelics.WhatIfRelicsCode.Interop;

public static class YuWanInterop
{
    private const string RemoteInteropTypeName = "YuWanCard.WhatIfRelicsCode.Interop.YuWanWhatIfInterop, YuWanCard";

    public static bool IsAvailable() => InvokeRemote(nameof(IsAvailable), false);

    public static string[] GetRegisteredWhatIfRelicTypeNames() => InvokeRemote<string[]>(nameof(GetRegisteredWhatIfRelicTypeNames), []);

    public static string[] GetSupplementalWhatIfRelicTypeNames() => InvokeRemote<string[]>(nameof(GetSupplementalWhatIfRelicTypeNames), []);

    public static string? GetShaCardEntry() => InvokeRemote<string?>(nameof(GetShaCardEntry), null);

    public static string? GetSadArmyWinCardEntry() => InvokeRemote<string?>(nameof(GetSadArmyWinCardEntry), null);

    public static string? GetHeartsteelRelicEntry() => InvokeRemote<string?>(nameof(GetHeartsteelRelicEntry), null);

    public static string? GetTenYearBambooRelicEntry() => InvokeRemote<string?>(nameof(GetTenYearBambooRelicEntry), null);

    public static string? GetTriplePlayRelicEntry() => InvokeRemote<string?>(nameof(GetTriplePlayRelicEntry), null);

    public static string[] GetSeriesRelicEntries() => InvokeRemote<string[]>(nameof(GetSeriesRelicEntries), []);

    private static T InvokeRemote<T>(string methodName, T fallback)
    {
        try
        {
            Type? remoteType = Type.GetType(RemoteInteropTypeName, throwOnError: false);
            MethodInfo? method = remoteType?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method?.Invoke(null, null) is T value)
            {
                return value;
            }
        }
        catch
        {
        }

        return fallback;
    }
}


