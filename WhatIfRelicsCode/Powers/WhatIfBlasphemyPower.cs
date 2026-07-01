using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace WhatIfRelics.WhatIfRelicsCode.Powers;

[RegisterPower]
public sealed class WhatIfBlasphemyPower : WhatIfPowerTemplate
{
    private const decimal DamageMultiplierPerStack = 3m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Amount <= 0 ||
            !props.IsPoweredAttack() ||
            cardSource == null ||
            dealer == null)
        {
            return 1m;
        }

        if (dealer != Owner && !Owner.Pets.Contains(dealer))
        {
            return 1m;
        }

        decimal multiplier = 1m;
        for (int i = 0; i < Amount; i++)
        {
            multiplier *= DamageMultiplierPerStack;
        }

        return multiplier;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Owner.IsDead)
        {
            return;
        }

        await CreatureCmd.Kill(Owner, force: true);
    }
}
