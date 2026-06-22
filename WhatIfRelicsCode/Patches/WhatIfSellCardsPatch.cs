using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using WhatIfRelics.WhatIfRelicsCode.Relics;
using WhatIfRelics.WhatIfRelicsCode.Utils;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(NCardHolder))]
internal static class WhatIfSellCardsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnMouseReleased")]
    private static bool NCardHolder_OnMouseReleased_Prefix(NCardHolder __instance, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Right } inputEventMouseButton
            || inputEventMouseButton.Pressed)
        {
            return true;
        }

        if (NCapstoneContainer.Instance?.CurrentCapstoneScreen is not NDeckViewScreen screen)
        {
            return true;
        }

        Player? player = Traverse.Create(screen).Field("_player").GetValue<Player>();
        CardModel? card = __instance.CardModel;
        if (player == null || player.GetRelic<WhatIfSellCards>() == null || !WhatIfMerchantSellHelper.CanSellCard(player, card))
        {
            return true;
        }

        TaskHelper.RunSafely(TrySellCardAsync(player, card!));
        return false;
    }

    private static async Task TrySellCardAsync(Player player, CardModel card)
    {
        int gold = WhatIfMerchantSellHelper.GetSellPrice(card);
        await CardPileCmd.RemoveFromDeck(card);
        await PlayerCmd.GainGold(gold, player);
        Entry.Logger.Info($"[WhatIfSellCards] Sold deck card {card.Id.Entry} for {gold} gold.");
    }
}
