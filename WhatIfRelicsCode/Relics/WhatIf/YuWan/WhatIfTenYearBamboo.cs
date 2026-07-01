using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Interop;
using WhatIfRelics.WhatIfRelicsCode.Relics.YuWan;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfTenYearBamboo")]
public class WhatIfTenYearBamboo : WhatIfRelicModel, IWhatIfUniformRelicSource, IYuWanWhatIfRelic
{
    public WhatIfTenYearBamboo() : base(true)
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

        var bambooModel = YuWanInteropResolver.ResolveRelic(YuWanInterop.GetTenYearBambooRelicEntry());
        if (bambooModel == null)
        {
            return false;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                var bambooRelic = bambooModel.ToMutable();
                rewards[i] = new RelicReward(bambooRelic, player);
            }
        }

        return true;
    }

    public RelicModel GetUniformRelic(IRunState runState)
    {
        return YuWanInteropResolver.ResolveRelic(YuWanInterop.GetTenYearBambooRelicEntry()) ?? this;
    }

    public RelicModel? GetUniformRelicForHoverTips()
    {
        return YuWanInteropResolver.ResolveRelic(YuWanInterop.GetTenYearBambooRelicEntry());
    }
}




