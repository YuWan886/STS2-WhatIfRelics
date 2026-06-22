using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using WhatIfRelics.WhatIfRelicsCode.Networking;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace WhatIfRelics.WhatIfRelicsCode;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    public const string ModId = "WhatIfRelics";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        WhatIfRelicsSettingsPage.Register();
        WhatIfRelicsConfigSync.Register();

        Harmony harmony = new(ModId);
        harmony.PatchAll(assembly);

        Logger.Info("WhatIfRelics initialized.");
    }
}


