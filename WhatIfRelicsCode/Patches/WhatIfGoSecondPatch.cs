using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(CombatManager))]
internal static class WhatIfGoSecondPatch
{
    private static readonly ConditionalWeakTable<CombatState, State> States = new();

    private sealed class State
    {
        public bool ForcedEnemyTurn;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CombatManager.SetUpCombat))]
    private static void CombatManager_SetUpCombat_Postfix(CombatState state)
    {
        States.Remove(state);
        States.Add(state, new State());
    }

    [HarmonyPostfix]
    [HarmonyPatch("StartTurn")]
    private static void CombatManager_StartTurn_Postfix(CombatManager __instance, ref Task __result)
    {
        __result = WrapStartTurnAsync(__instance, __result);
    }

    private static async Task WrapStartTurnAsync(CombatManager combatManager, Task originalTask)
    {
        await originalTask;

        CombatState? state = combatManager.DebugOnlyGetState();
        if (state == null || !combatManager.IsInProgress || state.CurrentSide != CombatSide.Player)
        {
            return;
        }

        if (!States.TryGetValue(state, out State? combatState))
        {
            return;
        }

        if (combatState.ForcedEnemyTurn)
        {
            return;
        }

        if (RunManager.Instance?.NetService?.Type != NetGameType.Singleplayer)
        {
            return;
        }

        bool shouldForce = state.Players.Any(player =>
            player.GetRelic<WhatIfGoSecond>() != null
            && player.PlayerCombatState?.TurnNumber == 1
            && !player.Creature.IsDead);

        if (!shouldForce)
        {
            return;
        }

        Player? localPlayer = LocalContext.GetMe(state);
        if (localPlayer == null)
        {
            return;
        }

        combatState.ForcedEnemyTurn = true;
        Entry.Logger.Info("[WhatIfGoSecond] Forcing enemy side to act before the first player play phase.");
        combatManager.SetReadyToBeginEnemyTurn(localPlayer);
    }
}
