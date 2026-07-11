using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfFullHand")]
public class WhatIfFullHand : WhatIfRelicModel
{
    public WhatIfFullHand() : base(true)
    {
    }

    public override decimal ModifyHandDrawLate(Player player, decimal count)
    {
        if (player != Owner || player.PlayerCombatState == null)
        {
            return count;
        }

        return Math.Max(0, CardPile.MaxCardsInHand - player.PlayerCombatState.Hand.Cards.Count);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.IsAutoPlay || !cardPlay.IsFirstInSeries)
        {
            return;
        }

        Flash();
        await CardPileCmd.Draw(choiceContext, Owner);
    }
}
