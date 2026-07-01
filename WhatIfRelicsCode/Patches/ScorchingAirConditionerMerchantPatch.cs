using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(MerchantInventory), "PopulateRelicEntries")]
internal static class ScorchingAirConditionerMerchantPatch
{
    [HarmonyPostfix]
    private static void MerchantInventory_PopulateRelicEntries_Postfix(MerchantInventory __instance)
    {
        if (!ShouldAddAirConditioner(__instance))
        {
            return;
        }

        __instance.AddRelicEntry(new MerchantRelicEntry(ModelDb.Relic<ScorchingAirConditioner>().ToMutable(), __instance.Player));
    }

    private static bool ShouldAddAirConditioner(MerchantInventory inventory)
    {
        if (WhatIfScorchingSpire.FindOwned(inventory.Player) == null)
        {
            return false;
        }

        if (inventory.Player.Relics.OfType<ScorchingAirConditioner>().Any())
        {
            return false;
        }

        ModelId airConditionerId = ModelDb.Relic<ScorchingAirConditioner>().Id;
        return inventory.RelicEntries.All(entry => entry.Model?.CanonicalInstance?.Id != airConditionerId);
    }
}
