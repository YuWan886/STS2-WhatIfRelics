using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(PlayerCombatState))]
internal static class WhatIfEnergyOverdraftPlayerCombatStatePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerCombatState.HasEnoughResourcesFor))]
    private static void HasEnoughResourcesFor_Postfix(
        PlayerCombatState __instance,
        CardModel card,
        ref UnplayableReason reason,
        ref bool __result)
    {
        if (card.Owner.PlayerCombatState != __instance || card.Owner.GetRelic<WhatIfEnergyOverdraft>() == null)
        {
            return;
        }

        reason &= ~UnplayableReason.EnergyCostTooHigh;
        __result = reason == UnplayableReason.None;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerCombatState.ResetEnergy))]
    private static bool ResetEnergy_Prefix(PlayerCombatState __instance, Player ____player)
    {
        if (____player.GetRelic<WhatIfEnergyOverdraft>() == null)
        {
            return true;
        }

        __instance.Energy = __instance.MaxEnergy + Math.Min(0, __instance.Energy);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerCombatState.GainEnergy))]
    private static bool GainEnergy_Prefix(PlayerCombatState __instance, Player ____player, decimal amount)
    {
        if (____player.GetRelic<WhatIfEnergyOverdraft>() == null
            || __instance.Energy >= 0
            || amount <= 0m)
        {
            return true;
        }

        __instance.Energy = (int)Math.Clamp(
            (decimal)__instance.Energy + amount,
            (decimal)int.MinValue,
            999999999m);
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), "SpendEnergy")]
internal static class WhatIfEnergyOverdraftCardPatch
{
    [HarmonyPrefix]
    private static bool SpendEnergy_Prefix(CardModel __instance, int amount, ref Task __result)
    {
        if (__instance.Owner.GetRelic<WhatIfEnergyOverdraft>() == null
            || __instance.Owner.PlayerCombatState is not { } playerCombatState
            || __instance.CombatState is not { } combatState
            || (amount <= 0 && !__instance.EnergyCost.CostsX))
        {
            return true;
        }

        __result = SpendEnergyOnCreditAsync(__instance, combatState, playerCombatState, amount);
        return false;
    }

    private static async Task SpendEnergyOnCreditAsync(
        CardModel card,
        ICombatState combatState,
        PlayerCombatState playerCombatState,
        int amount)
    {
        int energyToSpend = Math.Max(0, amount);
        if (card.EnergyCost.CostsX)
        {
            card.EnergyCost.CapturedXValue = energyToSpend;
        }

        if (energyToSpend > 0)
        {
            CombatManager.Instance.History.EnergySpent(combatState, energyToSpend, card.Owner);
            playerCombatState.Energy -= energyToSpend;
        }

        await Hook.AfterEnergySpent(combatState, card, energyToSpend);
    }
}
