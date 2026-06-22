using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Unlocks;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.GenerateUnlockStateFromProgress))]
internal static class WhatIfUnlockStatePatch
{
    [HarmonyPostfix]
    private static void AddWhatIfRelicsToUnlockState(ref UnlockState __result)
    {
        WhatIfRelicPool? pool = TryGetWhatIfPool();
        if (pool == null)
        {
            return;
        }

        __result = new UnlockState([__result, CreateWhatIfRelicUnlockState(pool)]);
    }

    private static UnlockState CreateWhatIfRelicUnlockState(WhatIfRelicPool pool)
    {
        // Use a synthetic "all epochs revealed" state so Relics only contributes the WhatIf pool,
        // while the merged result preserves the player's original encounter and run-count progress.
        return new UnlockState(EpochModel.AllEpochIds, pool.AllRelics.Select(static relic => relic.Id), 0);
    }

    private static WhatIfRelicPool? TryGetWhatIfPool()
    {
        try
        {
            return ModelDb.RelicPool<WhatIfRelicPool>();
        }
        catch
        {
            return null;
        }
    }
}
