using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Utils;
using WhatIfRelics.WhatIfRelicsCode.Networking;
using WhatIfRelics.WhatIfRelicsCode.Powers;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfScorchingSpire")]
public sealed class WhatIfScorchingSpire : WhatIfRelicModel
{
    private static readonly SavedAttachedState<WhatIfScorchingSpire, int> HeatStacksState =
        new("HeatStacks", _ => 1);

    private static readonly SavedAttachedState<WhatIfScorchingSpire, int> GrantedMilestonesState =
        new("GrantedMilestones", _ => 0);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("FloorInterval", GetConfiguredFloorInterval())
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WhatIfHeatPower>()
    ];

    public override bool ShowCounter => CurrentHeatStacks > 0;

    public override int DisplayAmount => CurrentHeatStacks;

    private int CurrentHeatStacks
    {
        get => HeatStacksState.GetValueOrDefault(this, 0);
        set
        {
            HeatStacksState[this] = Math.Max(0, value);
            InvokeDisplayAmountChanged();
        }
    }

    private int GrantedMilestones
    {
        get => GrantedMilestonesState.GetValueOrDefault(this, 0);
        set => GrantedMilestonesState[this] = Math.Max(0, value);
    }

    public WhatIfScorchingSpire() : base(true)
    {
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        ApplyHeatFromFloorMilestones();

        if (room is CombatRoom)
        {
            await RefreshHeatPowerAsync();
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner?.Creature == null)
        {
            return;
        }

        WhatIfHeatPower? existingPower = FindHeatPower(Owner.Creature);
        if (existingPower != null)
        {
            await PowerCmd.Remove(existingPower);
        }
    }

    public static WhatIfScorchingSpire? FindOwned(Player? player)
    {
        return player?.Relics.OfType<WhatIfScorchingSpire>().FirstOrDefault();
    }

    public async Task ReduceHeatAsync(int amount, PlayerChoiceContext? choiceContext = null)
    {
        if (amount <= 0 || CurrentHeatStacks <= 0)
        {
            return;
        }

        CurrentHeatStacks -= Math.Min(amount, CurrentHeatStacks);
        Flash();
        await RefreshHeatPowerAsync(choiceContext);
    }

    private void ApplyHeatFromFloorMilestones()
    {
        if (Owner?.RunState == null)
        {
            return;
        }

        int floorInterval = GetConfiguredFloorInterval();
        int reachedMilestones = Owner.RunState.TotalFloor / floorInterval;
        int pendingMilestones = reachedMilestones - GrantedMilestones;
        if (pendingMilestones <= 0)
        {
            return;
        }

        GrantedMilestones = reachedMilestones;
        CurrentHeatStacks += pendingMilestones;
        Flash();
    }

    private async Task RefreshHeatPowerAsync(PlayerChoiceContext? choiceContext = null)
    {
        if (Owner?.Creature == null)
        {
            return;
        }

        WhatIfHeatPower? existingPower = FindHeatPower(Owner.Creature);
        int targetAmount = CurrentHeatStacks;
        if (targetAmount <= 0)
        {
            if (existingPower != null)
            {
                await PowerCmd.Remove(existingPower);
            }

            return;
        }

        if (existingPower == null)
        {
            await PowerCmd.Apply<WhatIfHeatPower>(
                choiceContext ?? new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                targetAmount,
                Owner.Creature,
                null);
            return;
        }

        decimal delta = targetAmount - existingPower.Amount;
        if (delta != 0)
        {
            await PowerCmd.ModifyAmount(choiceContext ?? new ThrowingPlayerChoiceContext(), existingPower, delta, null, null);
        }
    }

    private static WhatIfHeatPower? FindHeatPower(Creature creature)
    {
        return creature.Powers.OfType<WhatIfHeatPower>().FirstOrDefault();
    }

    private static int GetConfiguredFloorInterval()
    {
        return Math.Max(1, WhatIfRelicsConfigSync.EffectiveScorchingSpireFloorInterval());
    }
}
