using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using System.Security.Cryptography;
using System.Text;
using WhatIfRelics.WhatIfRelicsCode.Interop;
using WhatIfRelics.WhatIfRelicsCode.Relics.YuWan;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfSeriesRelics")]
public class WhatIfSeriesRelics : WhatIfRelicModel, IWhatIfUniformRelicSource, IYuWanWhatIfRelic
{
    private static readonly Lazy<RelicModel[]> SevenSinRelics = new(() =>
        YuWanInteropResolver.ResolveRelics(YuWanInterop.GetSeriesRelicEntries()));

    public WhatIfSeriesRelics() : base(true)
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

        var relics = SevenSinRelics.Value;
        if (relics.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is RelicReward)
            {
                var sortedRelics = relics
                    .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
                    .ToArray();
                RelicModel sinRelic = sortedRelics[player.PlayerRng.Rewards.NextInt(sortedRelics.Length)];
                if (sinRelic != null)
                {
                    rewards[i] = new RelicReward(sinRelic.ToMutable(), player);
                }
            }
        }
        return true;
    }

    public RelicModel GetUniformRelic(IRunState runState)
    {
        var relics = SevenSinRelics.Value;
        if (relics.Length == 0)
        {
            return this;
        }

        var seedKey = $"{runState.Rng.StringSeed}|{Id.Entry}";
        var seedBytes = Encoding.UTF8.GetBytes(seedKey);
        var hashBytes = SHA256.HashData(seedBytes);
        var index = (int)(BitConverter.ToUInt32(hashBytes, 0) % (uint)relics.Length);
        return relics[index];
    }
}




