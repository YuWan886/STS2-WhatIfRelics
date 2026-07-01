using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using WhatIfRelics.WhatIfRelicsCode.Content;
using WhatIfRelics.WhatIfRelicsCode.Jumping;
using WhatIfRelics.WhatIfRelicsCode.Relics;
using WhatIfRelics.WhatIfRelicsCode.Networking;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace WhatIfRelics.WhatIfRelicsCode;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    public const string ModId = "WhatIfRelics";
    public const string ResPath = $"res://{ModId}";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        WhatIfAuxContentRegistration.Initialize(ModId, Logger);
        WhatIfRelicRegistration.Initialize(ModId, Logger, assembly);

        WhatIfRelicsSettingsPage.Register();
        WhatIfRelicsConfigSync.Register();
        WhatIfJumpController.Register();

        Harmony harmony = new(ModId);
        harmony.PatchAll(assembly);

        Logger.Info("WhatIfRelics initialized.");
    }
}


