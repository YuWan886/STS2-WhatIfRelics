using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace WhatIfRelics.WhatIfRelicsCode.Utils;

internal static class WhatIfMerchantSellHelper
{
    public static bool CanSellCard(Player player, CardModel? card)
    {
        return player.RunState.CurrentRoom is MerchantRoom
            && card != null
            && card.Owner == player
            && card.Pile?.Type == PileType.Deck
            && card.IsRemovable;
    }

    public static int GetSellPrice(CardModel card)
    {
        int basePrice = card.Rarity switch
        {
            CardRarity.Rare => 150,
            CardRarity.Uncommon => 75,
            _ => 50
        };

        if (card.Pool is ColorlessCardPool)
        {
            basePrice = (int)MathF.Round(basePrice * 1.15f);
        }

        return Math.Max(1, basePrice / 2);
    }
}
