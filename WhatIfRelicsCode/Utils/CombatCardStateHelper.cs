using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Utils;

/// <summary>
/// Resolves a card returned from a card-selection command back to the live hand-pile instance for its owner.
/// For the local player the selection already returns the live instance; for a remote player the card is
/// reconstructed from the network choice and must be re-anchored before it is mutated (exhausted).
/// 将卡牌选择命令返回的卡牌解析回其拥有者手牌堆中的实时实例。本地玩家的选择已是实时实例；远程玩家的卡牌是依据网络选择重建的，
/// 在被改动（消耗）之前必须重新锚定。
///
/// Ported from YuWanCard's CombatCardStateHelper (base-game types only). 移植自 YuWanCard 的同名工具（仅依赖原版类型）。
/// </summary>
internal static class CombatCardStateHelper
{
    public static CardModel? ResolveSelectedHandCard(Player? owner, CardModel? selectedCard)
    {
        if (owner == null || selectedCard == null)
        {
            return null;
        }

        IReadOnlyList<CardModel> handCards = PileType.Hand.GetPile(owner).Cards;
        if (handCards.Any(card => ReferenceEquals(card, selectedCard)))
        {
            return selectedCard;
        }

        if (NetCombatCardDb.Instance.TryGetCardId(selectedCard, out uint combatCardId)
            && NetCombatCardDb.Instance.TryGetCard(combatCardId, out CardModel? combatCard)
            && combatCard != null
            && handCards.Any(card => ReferenceEquals(card, combatCard)))
        {
            return combatCard;
        }

        SerializableCard serializedCard = selectedCard.ToSerializable();
        return handCards.FirstOrDefault(card => MatchesSelection(card, serializedCard, selectedCard));
    }

    private static bool MatchesSelection(CardModel candidate, SerializableCard serializedCard, CardModel selectedCard)
    {
        if (!candidate.IsMutable || candidate.Pile?.Type != PileType.Hand)
        {
            return false;
        }

        if (!candidate.ToSerializable().Equals(serializedCard))
        {
            return false;
        }

        return candidate.EnergyCost?.GetResolved() == selectedCard.EnergyCost?.GetResolved();
    }
}


