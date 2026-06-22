using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfDragonFruit")]
public class WhatIfDragonFruit : WhatIfRelicModel, IWhatIfUniformRelicSource
{
    public WhatIfDragonFruit() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || Owner == null)
        {
            return false;
        }

        if (!WhatIfReplacementContext.ShouldReplaceRelicRewards(room))
        {
            return false;
        }

        var fruitModel = ModelDb.Relic<DragonFruit>();

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                rewards[i] = new RelicReward(fruitModel.ToMutable(), player);
            }
        }

        return true;
    }

    public RelicModel GetUniformRelic(IRunState runState)
    {
        return ModelDb.Relic<DragonFruit>();
    }

    public RelicModel? GetUniformRelicForHoverTips()
    {
        return ModelDb.Relic<DragonFruit>();
    }
}




