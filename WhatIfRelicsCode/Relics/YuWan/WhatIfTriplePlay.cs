using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Interop;
using WhatIfRelics.WhatIfRelicsCode.Relics.YuWan;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfTriplePlay")]
public class WhatIfTriplePlay : WhatIfRelicModel, IWhatIfUniformRelicSource, IYuWanWhatIfRelic
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        YuWanInteropResolver.BuildRelicHoverTips(YuWanInterop.GetTriplePlayRelicEntry());
    
    public WhatIfTriplePlay() : base(true)
    {
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        if (!WhatIfReplacementContext.ShouldReplaceRelicRewards(room))
        {
            return false;
        }

        var triplePlayModel = YuWanInteropResolver.ResolveRelic(YuWanInterop.GetTriplePlayRelicEntry());
        if (triplePlayModel == null)
        {
            return false;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                rewards[i] = new RelicReward(triplePlayModel.ToMutable(), player);
            }
        }
        return true;
    }

    public RelicModel GetUniformRelic(IRunState runState)
    {
        return YuWanInteropResolver.ResolveRelic(YuWanInterop.GetTriplePlayRelicEntry()) ?? this;
    }
}




