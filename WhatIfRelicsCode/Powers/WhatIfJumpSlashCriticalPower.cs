using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace WhatIfRelics.WhatIfRelicsCode.Powers;

[RegisterPower]
public sealed class WhatIfJumpSlashCriticalPower : WhatIfPowerTemplate
{
    private const decimal DamageMultiplier = 1.5m;

    private CardModel? _attackToBoost;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Amount <= 0 ||
            _attackToBoost != null ||
            cardPlay.Card.Type != CardType.Attack ||
            Owner.Player == null ||
            cardPlay.Card.Owner != Owner.Player)
        {
            return Task.CompletedTask;
        }

        _attackToBoost = cardPlay.Card;
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay = null)
    {
        if (Amount <= 0 ||
            !props.IsPoweredAttack() ||
            cardSource == null ||
            (dealer != Owner && dealer != Owner.Player?.Osty))
        {
            return 1m;
        }

        if (_attackToBoost == null)
        {
            CardPile? pile = cardSource.Pile;
            if ((pile == null || pile.Type != PileType.Play) &&
                cardSource.Type == CardType.Attack &&
                cardSource.Owner == Owner.Player)
            {
                return DamageMultiplier;
            }

            return 1m;
        }

        if (cardSource != _attackToBoost)
        {
            return 1m;
        }

        return DamageMultiplier;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != _attackToBoost)
        {
            return;
        }

        _attackToBoost = null;
        await PowerCmd.ModifyAmount(choiceContext, this, -1m, null, null);
    }
}
