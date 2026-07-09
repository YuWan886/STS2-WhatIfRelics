using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using WhatIfRelics.WhatIfRelicsCode.Utils;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfInfinitePotions")]
public class WhatIfInfinitePotions : WhatIfRelicModel
{
    private bool _isRefillingPotionSlots;

    public WhatIfInfinitePotions() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await FillEmptyPotionSlots(inCombat: Owner?.Creature.CombatState != null);
    }

    public override async Task AfterPotionProcured(PotionModel potion)
    {
        if (potion.Owner != Owner)
        {
            return;
        }

        await FillEmptyPotionSlots(inCombat: potion.Owner.Creature.CombatState != null);
    }

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner)
        {
            return;
        }

        await FillEmptyPotionSlots(inCombat: true);
    }

    private async Task FillEmptyPotionSlots(bool inCombat)
    {
        if (Owner == null || !Owner.HasOpenPotionSlots || _isRefillingPotionSlots)
        {
            return;
        }

        _isRefillingPotionSlots = true;
        try
        {
            foreach (PotionModel potion in WhatIfRandomPotionHelper.CreateRandomPotionsForOpenSlots(Owner, inCombat))
            {
                Flash();
                await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
            }
        }
        finally
        {
            _isRefillingPotionSlots = false;
        }
    }
}
