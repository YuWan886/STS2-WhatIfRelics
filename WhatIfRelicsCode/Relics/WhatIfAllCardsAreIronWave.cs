using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfAllCardsAreIronWave")]
public class WhatIfAllCardsAreIronWave : WhatIfRelicModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DamageVar(5m, ValueProp.Move),
            new BlockVar(5m, ValueProp.Move)
        ];


    public WhatIfAllCardsAreIronWave() : base(true)
    {
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null || cardPlay.Card.Owner != Owner)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, null);

        var target = Owner.RunState.Rng.CombatTargets.NextItem(Owner.Creature.CombatState.HittableEnemies);
        if (target == null)
        {
            return;
        }

        await CreatureCmd.Damage(context, target, DynamicVars.Damage.BaseValue, ValueProp.Move, Owner.Creature, null);
    }
}




