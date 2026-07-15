using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch]
internal static class WhatIfCardsDoNotExhaustPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardModel), "GetResultPileTypeAndPositionForCardPlay")]
    private static bool GetResultPileTypeAndPositionForCardPlay_Prefix(CardModel __instance, ref (PileType pileType, CardPilePosition position) __result)
    {
        if (!ShouldDiscardInstead(__instance))
        {
            return true;
        }

        __instance.ExhaustOnNextPlay = false;
        __result = (PileType.Discard, CardPilePosition.Bottom);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Exhaust))]
    private static bool Exhaust_Prefix(
        CardModel card,
        bool skipVisuals,
        ref Task __result)
    {
        if (!ShouldDiscardInstead(card))
        {
            return true;
        }

        __result = CardPileCmd.Add(card, PileType.Discard, CardPilePosition.Bottom, null, skipVisuals);
        return false;
    }

    private static bool ShouldDiscardInstead(CardModel card)
    {
        return card.Keywords.Contains(CardKeyword.Exhaust)
            && card.Owner?.GetRelic<WhatIfCardsDoNotExhaust>() != null;
    }
}
