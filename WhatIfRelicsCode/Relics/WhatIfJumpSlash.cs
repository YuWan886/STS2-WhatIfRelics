using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using WhatIfRelics.WhatIfRelicsCode.Powers;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfJumpSlash")]
public class WhatIfJumpSlash : WhatIfRelicModel
{
    private const int CriticalChancePercent = 5;
    private const int MaxCriticalsPerTurn = 2;

    private int _criticalsGrantedThisTurn;

    [SavedProperty]
    public int CriticalsGrantedThisTurn
    {
        get => _criticalsGrantedThisTurn;
        private set
        {
            AssertMutable();
            _criticalsGrantedThisTurn = Math.Clamp(value, 0, MaxCriticalsPerTurn);
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WhatIfJumpSlashCriticalPower>()
    ];

    public WhatIfJumpSlash() : base(true)
    {
    }

    public static bool HasJumpSlashRelic(IRunState? runState)
    {
        return runState?.Players.Any(static player => player.Relics.Any(static relic => relic is WhatIfJumpSlash)) == true;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            SetCriticalsGrantedThisTurn(0);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        SetCriticalsGrantedThisTurn(0);
        return Task.CompletedTask;
    }

    public bool CanGainCriticalThisTurn()
    {
        return Owner?.Creature != null && CriticalsGrantedThisTurn < MaxCriticalsPerTurn;
    }

    public async Task TryGrantCriticalFromJump(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null || !CanGainCriticalThisTurn())
        {
            return;
        }

        if (Owner.RunState.Rng.Niche.NextInt(100) >= CriticalChancePercent)
        {
            return;
        }

        await PowerCmd.Apply<WhatIfJumpSlashCriticalPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null);
        SetCriticalsGrantedThisTurn(CriticalsGrantedThisTurn + 1);
        Flash();
    }

    private void SetCriticalsGrantedThisTurn(int value)
    {
        CriticalsGrantedThisTurn = value;
    }
}
