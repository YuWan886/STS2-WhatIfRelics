using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(CardPileCmd))]
internal static class WhatIfCheatDrawPatch
{
    private const string SelectPromptKey = "WHAT_IF_RELICS_WHAT_IF_CHEAT_DRAW_SELECT_PROMPT";

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CardPileCmd.Draw), typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool))]
    private static bool Draw_Prefix(
        PlayerChoiceContext choiceContext,
        decimal count,
        Player player,
        bool fromHandDraw,
        ref Task<IEnumerable<CardModel>> __result)
    {
        if (!fromHandDraw || player.GetRelic<WhatIfCheatDraw>() == null)
        {
            return true;
        }

        __result = DrawChosenCardsAsync(choiceContext, count, player, fromHandDraw);
        return false;
    }

    private static async Task<IEnumerable<CardModel>> DrawChosenCardsAsync(
        PlayerChoiceContext choiceContext,
        decimal count,
        Player player,
        bool fromHandDraw)
    {
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return Array.Empty<CardModel>();
        }

        if (player.Creature.CombatState is not { } combatState)
        {
            return Array.Empty<CardModel>();
        }

        if (!Hook.ShouldDraw(combatState, player, fromHandDraw, out AbstractModel? modifier))
        {
            if (modifier != null)
            {
                await Hook.AfterPreventingDraw(combatState, modifier);
            }

            return Array.Empty<CardModel>();
        }

        List<CardModel> result = [];
        CardPile hand = PileType.Hand.GetPile(player);
        CardPile drawPile = PileType.Draw.GetPile(player);

        int drawsRequested = count > 0m ? (int)Math.Ceiling(count) : 0;
        if (drawsRequested == 0)
        {
            return result;
        }

        int handSpace = Math.Max(0, CardPile.MaxCardsInHand - hand.Cards.Count);
        if (handSpace == 0)
        {
            ShowHandFullThought(player);
            return result;
        }

        while (result.Count < drawsRequested && handSpace > 0 && !CombatManager.Instance.IsOverOrEnding)
        {
            if (!IsDrawPossible(player))
            {
                break;
            }

            await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
            if (!IsDrawPossible(player))
            {
                break;
            }

            int needed = Math.Min(drawsRequested - result.Count, handSpace);
            int selectableCount = Math.Min(needed, drawPile.Cards.Count);
            if (selectableCount <= 0)
            {
                break;
            }

            CardSelectorPrefs prefs = new(new LocString("relics", SelectPromptKey), selectableCount)
            {
                Cancelable = false
            };

            List<CardModel> selectedCards = (await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    drawPile.Cards.ToList(),
                    player,
                    prefs))
                .Distinct()
                .Where(drawPile.Cards.Contains)
                .Take(selectableCount)
                .ToList();

            if (selectedCards.Count == 0)
            {
                break;
            }

            foreach (CardModel card in selectedCards)
            {
                if (CombatManager.Instance.IsOverOrEnding || hand.Cards.Count >= CardPile.MaxCardsInHand || card.Pile != drawPile)
                {
                    break;
                }

                result.Add(card);
                await CardPileCmd.Add(card, hand);
                CombatManager.Instance.History.CardDrawn(combatState, card, fromHandDraw);
                await Hook.AfterCardDrawn(combatState, choiceContext, card, fromHandDraw);
                card.InvokeDrawn();
                NDebugAudioManager.Instance?.Play("card_deal.mp3", 0.25f, PitchVariance.Small);
            }

            handSpace = Math.Max(0, CardPile.MaxCardsInHand - hand.Cards.Count);
        }

        return result;
    }

    private static bool IsDrawPossible(Player player)
    {
        if (PileType.Draw.GetPile(player).Cards.Count + PileType.Discard.GetPile(player).Cards.Count == 0)
        {
            ThinkCmd.Play(new LocString("combat_messages", "NO_DRAW"), player.Creature, 2.0);
            return false;
        }

        if (PileType.Hand.GetPile(player).Cards.Count >= CardPile.MaxCardsInHand)
        {
            ShowHandFullThought(player);
            return false;
        }

        return true;
    }

    private static void ShowHandFullThought(Player player)
    {
        ThinkCmd.Play(new LocString("combat_messages", "HAND_FULL"), player.Creature, 2.0);
    }
}
