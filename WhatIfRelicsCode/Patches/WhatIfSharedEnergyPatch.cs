using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(CombatManager))]
internal static class WhatIfSharedEnergyCombatPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CombatManager.SetUpCombat))]
    private static void SetUpCombat_Postfix(CombatState state)
    {
        WhatIfSharedEnergyState.Reset(state);
    }
}

[HarmonyPatch(typeof(PlayerCombatState))]
internal static class WhatIfSharedEnergyPlayerCombatStatePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerCombatState.ResetEnergy))]
    private static bool ResetEnergy_Prefix(PlayerCombatState __instance, Player ____player)
    {
        if (!WhatIfSharedEnergyState.TryGetActiveState(____player, out ICombatState combatState, out var state))
        {
            return true;
        }

        if (state.LastResetRound == combatState.RoundNumber)
        {
            return false;
        }

        state.LastResetRound = combatState.RoundNumber;
        int totalMaxEnergy = combatState.Players
            .Where(static player => !player.Creature.IsDead)
            .Sum(static player => player.PlayerCombatState?.MaxEnergy ?? 0);
        WhatIfSharedEnergyState.SetForAll(combatState, totalMaxEnergy);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerCombatState.Energy), MethodType.Setter)]
    private static void Energy_Set_Postfix(PlayerCombatState __instance, Player ____player)
    {
        if (WhatIfSharedEnergyState.IsSynchronizing
            || !WhatIfSharedEnergyState.TryGetActiveState(____player, out ICombatState combatState, out _))
        {
            return;
        }

        WhatIfSharedEnergyState.SetForAll(combatState, __instance.Energy);
    }
}

[HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy))]
internal static class WhatIfSharedEnergyPlayerCmdPatch
{
    [HarmonyPostfix]
    private static void GainEnergy_Postfix(ref Task __result, Player player)
    {
        if (!WhatIfSharedEnergyState.TryGetActiveState(player, out _, out _))
        {
            return;
        }

        __result = SynchronizeAfterGainAsync(__result, player);
    }

    private static async Task SynchronizeAfterGainAsync(Task gainEnergyTask, Player player)
    {
        await gainEnergyTask;

        if (WhatIfSharedEnergyState.TryGetActiveState(player, out ICombatState combatState, out _)
            && player.PlayerCombatState is { } playerCombatState)
        {
            WhatIfSharedEnergyState.SetForAll(combatState, playerCombatState.Energy);
        }
    }
}

internal static class WhatIfSharedEnergyState
{
    internal sealed class State
    {
        public int LastResetRound = int.MinValue;
    }

    private static readonly ConditionalWeakTable<ICombatState, State> States = new();

    [ThreadStatic]
    private static int _synchronizationDepth;

    public static bool IsSynchronizing => _synchronizationDepth > 0;

    public static void Reset(ICombatState combatState)
    {
        States.Remove(combatState);
        States.Add(combatState, new State());
    }

    public static bool TryGetActiveState(Player player, out ICombatState combatState, out State state)
    {
        ICombatState? currentCombatState = player.Creature.CombatState;
        if (currentCombatState == null
            || player.RunState.Players.Count <= 1
            || !player.RunState.Players.Any(static candidate => candidate.GetRelic<WhatIfSharedEnergy>() != null))
        {
            combatState = null!;
            state = null!;
            return false;
        }

        combatState = currentCombatState;
        state = States.GetValue(combatState, static _ => new State());
        return true;
    }

    public static void SetForAll(ICombatState combatState, int energy)
    {
        _synchronizationDepth++;
        try
        {
            foreach (Player player in combatState.Players)
            {
                if (player.PlayerCombatState != null)
                {
                    player.PlayerCombatState.Energy = energy;
                }
            }
        }
        finally
        {
            _synchronizationDepth--;
        }
    }
}
