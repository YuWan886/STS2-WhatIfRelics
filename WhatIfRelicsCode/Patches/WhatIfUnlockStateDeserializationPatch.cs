using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(UnlockState), nameof(UnlockState.FromSerializable))]
internal static class WhatIfUnlockStateDeserializationPatch
{
    [HarmonyPrefix]
    private static void RemoveCorruptedWhatIfRelicEntries(SerializableUnlockState unlockState)
    {
        if (unlockState?.EncountersSeen == null || unlockState.EncountersSeen.Count == 0)
        {
            return;
        }

        List<ModelId> corruptedEntries = unlockState.EncountersSeen
            .Where(IsCorruptedWhatIfRelicEntry)
            .Distinct()
            .ToList();
        if (corruptedEntries.Count == 0)
        {
            return;
        }

        unlockState.EncountersSeen = unlockState.EncountersSeen
            .Where(id => !corruptedEntries.Contains(id))
            .ToList();
        Entry.Logger.Info(
            $"[WhatIfUnlockStateDeserializationPatch] Removed {corruptedEntries.Count} corrupted WhatIf relic ids from serialized unlock-state encounters: {string.Join(", ", corruptedEntries.Select(static id => id.ToString()))}");
    }

    private static bool IsCorruptedWhatIfRelicEntry(ModelId id)
    {
        if (!string.Equals(id.Category, "RELIC", StringComparison.Ordinal))
        {
            return false;
        }

        return ModelDb.GetByIdOrNull<RelicModel>(id) is WhatIfRelicModel;
    }
}
