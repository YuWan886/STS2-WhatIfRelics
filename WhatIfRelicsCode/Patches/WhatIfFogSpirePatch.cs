using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Relics;
using WhatIfRelics.WhatIfRelicsCode.Utils;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap))]
public static class WhatIfFogSpireMapPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMapScreen __instance)
    {
        RunState? runState = WhatIfReflectionHelper.GetPrivateField<RunState>(__instance, "_runState");
        WhatIfFogSpire.ApplyFogToMapScreen(runState, __instance);
    }
}

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.RefreshAllPointVisuals))]
public static class WhatIfFogSpireMapRefreshPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMapScreen __instance)
    {
        RunState? runState = WhatIfReflectionHelper.GetPrivateField<RunState>(__instance, "_runState");
        WhatIfFogSpire.ApplyFogToMapScreen(runState, __instance);
    }
}

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Open))]
public static class WhatIfFogSpireMapOpenPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMapScreen __instance)
    {
        RunState? runState = WhatIfReflectionHelper.GetPrivateField<RunState>(__instance, "_runState");
        WhatIfFogSpire.ApplyFogToMapScreen(runState, __instance);
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class WhatIfFogSpireCreatureReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        RefreshIntentVisibility(__instance);
    }

    private static void RefreshIntentVisibility(NCreature creature)
    {
        if (creature.Entity?.Monster == null || creature.Entity.CombatState?.RunState is not { } runState)
        {
            return;
        }

        creature.IntentContainer.Visible = !WhatIfFogSpire.ShouldHideEnemyIntents(runState);
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.UpdateIntent))]
public static class WhatIfFogSpireCreatureIntentPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        if (__instance.Entity?.Monster == null || __instance.Entity.CombatState?.RunState is not { } runState)
        {
            return;
        }

        __instance.IntentContainer.Visible = !WhatIfFogSpire.ShouldHideEnemyIntents(runState);
    }
}

[HarmonyPatch(typeof(NSubmenu), nameof(NSubmenu.OnSubmenuOpened))]
public static class WhatIfFogSpireSubmenuOpenPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        WhatIfFogSpire.PushMapFogUiSuppression();
    }
}

[HarmonyPatch(typeof(NSubmenu), nameof(NSubmenu.OnSubmenuClosed))]
public static class WhatIfFogSpireSubmenuClosePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        WhatIfFogSpire.PopMapFogUiSuppression();
    }
}
