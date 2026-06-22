using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode;

internal static class WhatIfUniformSourceResolver
{
    public static IWhatIfUniformRelicSource? FindUniformRelicSource(IRunState runState)
    {
        foreach (Player player in runState.Players)
        {
            foreach (var relic in player.Relics)
            {
                if (relic is IWhatIfUniformRelicSource source)
                {
                    return source;
                }
            }
        }

        return null;
    }

    public static IWhatIfUniformPotionSource? FindUniformPotionSource(IRunState runState)
    {
        foreach (Player player in runState.Players)
        {
            foreach (var relic in player.Relics)
            {
                if (relic is IWhatIfUniformPotionSource source)
                {
                    return source;
                }
            }
        }

        return null;
    }
}
