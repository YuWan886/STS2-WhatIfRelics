using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfEnergyOverdraft")]
public class WhatIfEnergyOverdraft : WhatIfRelicModel
{
    public WhatIfEnergyOverdraft() : base(true)
    {
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState is not { } combatState)
        {
            return;
        }

        int debt = Math.Max(0, -combatState.Energy);
        if (debt == 0)
        {
            return;
        }

        combatState.Energy = 0;
        Flash();
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            debt,
            DamageProps.nonCardHpLoss,
            dealer: null,
            cardSource: null);
    }
}
