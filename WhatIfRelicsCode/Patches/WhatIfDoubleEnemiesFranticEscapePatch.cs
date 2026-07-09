using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(FranticEscape), "OnPlay")]
internal static class WhatIfDoubleEnemiesFranticEscapePatch
{
    [HarmonyPrefix]
    private static bool FranticEscape_OnPlay_Prefix(
        FranticEscape __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result)
    {
        __result = ResolveAllSandpitsAsync(__instance, choiceContext);
        return false;
    }

    private static async Task ResolveAllSandpitsAsync(
        FranticEscape card,
        PlayerChoiceContext choiceContext)
    {
        Creature? ownerCreature = card.Owner?.Creature;
        if (ownerCreature?.CombatState != null)
        {
            // Vanilla assumes a single Insatiable and only touches the first Sandpit enemy it finds.
            foreach ((Creature enemy, SandpitPower sandpit) in ownerCreature.CombatState.Enemies
                         .SelectMany(
                             static enemy => enemy.Powers.OfType<SandpitPower>(),
                             static (enemy, sandpit) => (enemy, sandpit))
                         .Where(pair => pair.sandpit.Target == ownerCreature))
            {
                await PowerCmd.ModifyAmount(choiceContext, sandpit, 1m, enemy, card);
            }
        }

        card.EnergyCost.AddThisCombat(1);
    }
}
