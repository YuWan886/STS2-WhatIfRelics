using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Interop;
using WhatIfRelics.WhatIfRelicsCode.Relics.YuWan;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfHeartsteel")]
public class WhatIfHeartsteel : WhatIfRelicModel, IWhatIfUniformRelicSource, IYuWanWhatIfRelic
{
    public WhatIfHeartsteel() : base(true)
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

        var heartsteelModel = YuWanInteropResolver.ResolveRelic(YuWanInterop.GetHeartsteelRelicEntry());
        if (heartsteelModel == null)
        {
            return false;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                var heartsteelRelic = heartsteelModel.ToMutable();
                rewards[i] = new RelicReward(heartsteelRelic, player);
            }
        }

        return true;
    }

    public RelicModel GetUniformRelic(IRunState runState)
    {
        return YuWanInteropResolver.ResolveRelic(YuWanInterop.GetHeartsteelRelicEntry()) ?? this;
    }

    public RelicModel? GetUniformRelicForHoverTips()
    {
        return YuWanInteropResolver.ResolveRelic(YuWanInterop.GetHeartsteelRelicEntry());
    }
}




