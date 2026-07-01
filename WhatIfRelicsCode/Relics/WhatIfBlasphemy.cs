using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using WhatIfRelics.WhatIfRelicsCode.Cards;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfBlasphemy")]
public sealed class WhatIfBlasphemy : WhatIfRelicModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => BuildHoverTips();

    public WhatIfBlasphemy() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
        {
            return;
        }

        var blasphemy = Owner.RunState.CreateCard(ModelDb.Card<WhatIfBlasphemyCard>(), Owner);
        blasphemy.AddKeyword(CardKeyword.Innate);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(blasphemy, PileType.Deck));
    }

    private static IEnumerable<IHoverTip> BuildHoverTips()
    {
        var previewCard = (CardModel)ModelDb.Card<WhatIfBlasphemyCard>().MutableClone();
        previewCard.AddKeyword(CardKeyword.Innate);

        return
        [
            HoverTipFactory.FromCard(previewCard),
            HoverTipFactory.FromKeyword(CardKeyword.Innate),
            .. ModelDb.Card<WhatIfBlasphemyCard>().HoverTips
        ];
    }
}
