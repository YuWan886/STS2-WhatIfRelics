using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfIAmTransparent")]
public class WhatIfIAmTransparent : WhatIfRelicModel
{
    private bool _hasAttackedThisCombat;
    private bool _hasFlashedSinceLastAttack;

    public WhatIfIAmTransparent() : base(true)
    {
    }

    public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        Creature? ownerCreature = Owner?.Creature;
        if (ownerCreature == null || command.Attacker != ownerCreature || command.TargetSide != CombatSide.Enemy || !command.DamageProps.IsPoweredAttack())
        {
            return Task.CompletedTask;
        }

        _hasAttackedThisCombat = true;
        _hasFlashedSinceLastAttack = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _hasAttackedThisCombat = false;
        _hasFlashedSinceLastAttack = false;
        return Task.CompletedTask;
    }

    internal bool ShouldAvoidEnemyAttack(Creature target, AttackCommand command)
    {
        if (target != Owner?.Creature || target.IsDead)
        {
            return false;
        }

        if (_hasAttackedThisCombat || command.Attacker is not { IsEnemy: true } || !command.DamageProps.IsPoweredAttack())
        {
            return false;
        }

        if (!_hasFlashedSinceLastAttack)
        {
            _hasFlashedSinceLastAttack = true;
            Flash();
        }

        return true;
    }
}
