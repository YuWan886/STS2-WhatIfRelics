using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfNineLives")]
public class WhatIfNineLives : WhatIfRelicModel
{
    private const int MaxLives = 9;

    private int _triggersUsed;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Math.Max(0, MaxLives - TriggersUsed);

    [SavedProperty]
    public int TriggersUsed
    {
        get => _triggersUsed;
        set
        {
            AssertMutable();
            _triggersUsed = Math.Clamp(value, 0, MaxLives);
            base.Status = _triggersUsed >= MaxLives ? RelicStatus.Disabled : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public WhatIfNineLives() : base(true)
    {
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner?.Creature)
        {
            return true;
        }

        return TriggersUsed >= MaxLives;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        TriggersUsed++;
        await CreatureCmd.SetCurrentHp(creature, 1m);
    }
}
