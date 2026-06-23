using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(NMerchantCard), nameof(NMerchantCard.OnInventoryOpened))]
internal static class WhatIfMerchantCardFlashPatch
{
    private sealed class FlashMarker
    {
    }

    private static readonly ConditionalWeakTable<CardCreationResult, FlashMarker> FlashedResults = new();

    [HarmonyPrefix]
    private static bool NMerchantCard_OnInventoryOpened_Prefix(NMerchantCard __instance)
    {
        if (__instance.Entry is not MerchantCardEntry { CreationResult: { } creationResult })
        {
            return true;
        }

        if (!creationResult.HasBeenModified)
        {
            return true;
        }

        if (!creationResult.ModifyingRelics.Any(static relic => relic is WhatIfRelicModel))
        {
            return true;
        }

        if (FlashedResults.TryGetValue(creationResult, out _))
        {
            return false;
        }

        FlashedResults.Add(creationResult, new FlashMarker());
        return true;
    }
}
