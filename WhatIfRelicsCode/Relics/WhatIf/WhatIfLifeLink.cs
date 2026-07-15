using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfLifeLink")]
public sealed class WhatIfLifeLink : WhatIfRelicModel
{
    public WhatIfLifeLink() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner is not { } owner || owner.RunState.Players.Count <= 1)
        {
            return;
        }

        foreach (var teammate in owner.RunState.Players)
        {
            if (teammate == owner)
            {
                continue;
            }

            await CreatureCmd.SetMaxHp(teammate.Creature, owner.Creature.MaxHp);
            await CreatureCmd.SetCurrentHp(teammate.Creature, owner.Creature.CurrentHp);
        }
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (delta == 0 || Owner is not { } owner || creature != owner.Creature || owner.RunState.Players.Count <= 1)
        {
            return;
        }

        Flash();

        foreach (var teammate in owner.RunState.Players)
        {
            if (teammate == owner || teammate.Creature.IsDead)
            {
                continue;
            }

            decimal targetHp = Math.Max(0, teammate.Creature.CurrentHp + delta);
            await CreatureCmd.SetCurrentHp(teammate.Creature, targetHp);
        }
    }
}
