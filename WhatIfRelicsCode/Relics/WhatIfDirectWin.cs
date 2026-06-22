using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using WhatIfRelics.WhatIfRelicsCode.Interop;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfDirectWin")]
public class WhatIfDirectWin : WhatIfRelicModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        ResolveHoverTips();

    public WhatIfDirectWin() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner?.Creature == null)
        {
            return;
        }

        var sadArmyWinModel = YuWanInteropResolver.ResolveCard(YuWanInterop.GetSadArmyWinCardEntry());
        if (sadArmyWinModel == null)
        {
            return;
        }

        decimal targetHp = Math.Max(1m, Math.Ceiling(Owner.Creature.MaxHp * 0.1m));
        if (Owner.Creature.CurrentHp > targetHp)
        {
            await CreatureCmd.SetCurrentHp(Owner.Creature, targetHp);
        }

        var sadArmyWin = Owner.RunState.CreateCard(sadArmyWinModel, Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(sadArmyWin, PileType.Deck));
    }

    private static IEnumerable<IHoverTip> ResolveHoverTips()
    {
        return YuWanInteropResolver.BuildCardHoverTips(
            YuWanInteropResolver.ResolveCard(YuWanInterop.GetSadArmyWinCardEntry()));
    }
}




