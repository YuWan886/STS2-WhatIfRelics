using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfWhistle")]
public class WhatIfWhistle : WhatIfRelicModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Whistle>();

    public WhatIfWhistle() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (!WhatIfReplacementContext.ShouldReplaceStartingDeck())
        {
            return;
        }

        if (Owner?.Deck == null)
        {
            return;
        }

        var originalCards = Owner.Deck.Cards
            .Where(c => c.IsTransformable)
            .ToList();

        if (originalCards.Count == 0)
        {
            return;
        }

        var whistleModel = ModelDb.Card<Whistle>();

        var transformations = originalCards.Select(card =>
            new CardTransformation(card, Owner.RunState.CreateCard(whistleModel, Owner)));

        await CardCmd.Transform(transformations, null, CardPreviewStyle.None);

    }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (player != Owner)
        {
            return false;
        }

        if (!WhatIfReplacementContext.ShouldReplaceCardRewards(creationOptions.Source))
        {
            return false;
        }

        var whistleModel = ModelDb.Card<Whistle>();
        for (int i = 0; i < cardRewardOptions.Count; i++)
        {
            var whistleCard = Owner.RunState.CreateCard(whistleModel, Owner);
            cardRewardOptions[i] = new CardCreationResult(whistleCard);
        }

        return true;
    }
}




