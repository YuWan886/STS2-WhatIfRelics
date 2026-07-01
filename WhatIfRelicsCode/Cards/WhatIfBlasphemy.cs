using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using WhatIfRelics.WhatIfRelicsCode.Powers;

namespace WhatIfRelics.WhatIfRelicsCode.Cards;

[RegisterCard(typeof(EventCardPool), StableEntryStem = "WhatIfBlasphemy")]
public sealed class WhatIfBlasphemy : WhatIfCardTemplate
{
    private const int BaseEnergyCost = 1;

    public override CardPoolModel VisualCardPool => ModelDb.CardPool<EventCardPool>();

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<WhatIfBlasphemyPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(3)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    public WhatIfBlasphemy()
        : base(BaseEnergyCost, CardType.Skill, CardRarity.Event, TargetType.Self, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await PowerCmd.Apply<WhatIfBlasphemyPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
