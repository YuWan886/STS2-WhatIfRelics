using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Utils;
using WhatIfRelics.WhatIfRelicsCode.Powers;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfAuxRelicPool))]
public sealed class ScorchingAirConditioner : WhatIfAuxRelicTemplate
{
    private const int TurnsPerCooling = 2;

    private static readonly SavedAttachedState<ScorchingAirConditioner, int> TurnCounterState =
        new("TurnCounter", _ => 0);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<WhatIfHeatPower>()
    ];

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool IsAllowedInShops => false;

    private int TurnCounter
    {
        get => TurnCounterState.GetValueOrDefault(this, 0);
        set => TurnCounterState[this] = Math.Max(0, value);
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            TurnCounter = 0;
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        TurnCounter = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner != player)
        {
            return;
        }

        WhatIfScorchingSpire? scorchingSpire = WhatIfScorchingSpire.FindOwned(player);
        if (scorchingSpire == null)
        {
            return;
        }

        TurnCounter++;
        if (TurnCounter < TurnsPerCooling)
        {
            return;
        }

        TurnCounter = 0;
        await scorchingSpire.ReduceHeatAsync(1, choiceContext);
    }
}
