using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfBlasphemy")]
public sealed class WhatIfBlasphemy : WhatIfRelicModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Cards.WhatIfBlasphemy>();

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

        var blasphemy = Owner.RunState.CreateCard(ModelDb.Card<Cards.WhatIfBlasphemy>(), Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(blasphemy, PileType.Deck));
    }
}
