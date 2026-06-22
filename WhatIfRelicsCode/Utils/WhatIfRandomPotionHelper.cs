using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;

namespace WhatIfRelics.WhatIfRelicsCode.Utils;

internal static class WhatIfRandomPotionHelper
{
    public static IEnumerable<PotionModel> CreateRandomPotionsForOpenSlots(Player player, bool inCombat)
    {
        int openSlots = player.PotionSlots.Count(slot => slot == null);
        if (openSlots <= 0)
        {
            return [];
        }

        return inCombat
            ? CreateRandomCombatPotions(player, openSlots)
            : PotionFactory.CreateRandomPotionsOutOfCombat(player, openSlots, player.RunState.Rng.CombatPotionGeneration);
    }

    public static PotionModel CreateRandomPotion(Player player, bool inCombat)
    {
        return inCombat
            ? PotionFactory.CreateRandomPotionInCombat(player, player.RunState.Rng.CombatPotionGeneration)
            : PotionFactory.CreateRandomPotionOutOfCombat(player, player.RunState.Rng.CombatPotionGeneration);
    }

    private static IEnumerable<PotionModel> CreateRandomCombatPotions(Player player, int count)
    {
        var generated = new List<PotionModel>(count);
        var blacklist = new List<PotionModel>();
        for (int i = 0; i < count; i++)
        {
            PotionModel potion = PotionFactory.CreateRandomPotionInCombat(player, player.RunState.Rng.CombatPotionGeneration, blacklist);
            generated.Add(potion);
            blacklist.Add(potion);
        }

        return generated;
    }
}
