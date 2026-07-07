using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch]
internal static class WhatIfMaxHpEqualsGoldPatch
{
    private static readonly HashSet<Creature> SyncingCreatures = [];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Gold), MethodType.Setter)]
    private static void Player_Gold_Setter_Postfix(Player __instance)
    {
        if (__instance.GetRelic<WhatIfMaxHpEqualsGold>() == null)
        {
            return;
        }

        TaskHelper.RunSafely(WhatIfMaxHpEqualsGold.SyncPlayerMaxHpAsync(__instance));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.SetMaxHp))]
    private static void CreatureCmd_SetMaxHp_Postfix(Creature creature, ref Task<decimal> __result)
    {
        __result = WrapSetMaxHpAsync(creature, __result);
    }

    private static async Task<decimal> WrapSetMaxHpAsync(Creature creature, Task<decimal> originalTask)
    {
        decimal delta = await originalTask;

        Player? player = creature.Player;
        if (player == null || player.GetRelic<WhatIfMaxHpEqualsGold>() == null)
        {
            return delta;
        }

        int targetMaxHp = Math.Max(0, player.Gold);
        if (creature.MaxHp == targetMaxHp)
        {
            return delta;
        }

        lock (SyncingCreatures)
        {
            if (!SyncingCreatures.Add(creature))
            {
                return delta;
            }
        }

        try
        {
            await CreatureCmd.SetMaxHp(creature, targetMaxHp);
        }
        finally
        {
            lock (SyncingCreatures)
            {
                SyncingCreatures.Remove(creature);
            }
        }

        return delta;
    }
}
