using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(AttackCommand), "GetPossibleTargets")]
internal static class WhatIfIAmTransparentPatch
{
    [HarmonyPostfix]
    private static void AttackCommand_GetPossibleTargets_Postfix(AttackCommand __instance, ref IReadOnlyList<Creature> __result)
    {
        if (__result.Count == 0 || __instance.Attacker is not { IsEnemy: true } || !__instance.DamageProps.IsPoweredAttack())
        {
            return;
        }

        List<Creature>? filteredTargets = null;
        foreach (Creature target in __result)
        {
            WhatIfIAmTransparent? relic = target.Player?.GetRelic<WhatIfIAmTransparent>();
            if (relic?.ShouldAvoidEnemyAttack(target, __instance) == true)
            {
                filteredTargets ??= __result.ToList();
                filteredTargets.Remove(target);
            }
        }

        if (filteredTargets != null)
        {
            __result = filteredTargets;
        }
    }
}
