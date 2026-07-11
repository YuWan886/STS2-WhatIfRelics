using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Interop.AutoRegistration;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(AttributeAutoRegistrationTypeDiscoveryContributor), "ProcessAssembly")]
internal static class WhatIfAutoRegistrationLogPatch
{
    private sealed record LogScope(LogLevel? OriginalGenericLevel, int Cards, int Powers, int AuxiliaryRelics);

    [HarmonyPrefix]
    private static void ProcessAssembly_Prefix(Assembly assembly, ref LogScope? __state)
    {
        if (assembly != typeof(Entry).Assembly)
        {
            return;
        }

        Type[] types = assembly.GetTypes();
        __state = new LogScope(
            Logger.logLevelTypeMap.TryGetValue(LogType.Generic, out var level) ? level : null,
            types.Sum(static type => type.GetCustomAttributes<RegisterCardAttribute>(false).Count()),
            types.Sum(static type => type.GetCustomAttributes<RegisterPowerAttribute>(false).Count()),
            types.Sum(static type => type.GetCustomAttributes<RegisterRelicAttribute>(false).Count()));
        Logger.SetLogLevelForType(LogType.Generic, LogLevel.Warn);
    }

    [HarmonyFinalizer]
    private static Exception? ProcessAssembly_Finalizer(Exception? __exception, LogScope? __state)
    {
        if (__state == null)
        {
            return __exception;
        }

        Logger.SetLogLevelForType(LogType.Generic, __state.OriginalGenericLevel);
        if (__exception == null)
        {
            Entry.Logger.Info(
                $"[Content] Registered WhatIf support content: cards={__state.Cards}, powers={__state.Powers}, auxiliaryRelics={__state.AuxiliaryRelics}");
        }

        return __exception;
    }
}
