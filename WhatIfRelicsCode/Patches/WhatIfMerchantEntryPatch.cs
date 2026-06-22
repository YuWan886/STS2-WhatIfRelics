using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch]
internal static class WhatIfMerchantEntryPatch
{
    private static readonly AccessTools.FieldRef<MerchantEntry, Player> PlayerField =
        AccessTools.FieldRefAccess<MerchantEntry, Player>("_player");

    private static readonly AccessTools.FieldRef<MerchantRelicEntry, RelicModel?> MerchantRelicModelField =
        AccessTools.FieldRefAccess<MerchantRelicEntry, RelicModel?>("<Model>k__BackingField");

    private static readonly AccessTools.FieldRef<MerchantPotionEntry, PotionModel?> MerchantPotionModelField =
        AccessTools.FieldRefAccess<MerchantPotionEntry, PotionModel?>("<Model>k__BackingField");

    private static readonly AccessTools.FieldRef<PotionReward, PotionModel?> PotionRewardModelField =
        AccessTools.FieldRefAccess<PotionReward, PotionModel?>("<Potion>k__BackingField");

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MerchantRelicEntry), nameof(MerchantRelicEntry.CalcCost))]
    private static void MerchantRelicEntry_CalcCost_Prefix(MerchantRelicEntry __instance)
    {
        if (!WhatIfReplacementContext.ShouldReplaceShopRelics())
        {
            return;
        }

        Player player = PlayerField(__instance);
        var source = WhatIfUniformSourceResolver.FindUniformRelicSource(player.RunState);
        if (source == null)
        {
            return;
        }

        RelicModel? currentModel = MerchantRelicModelField(__instance);
        RelicModel replacement = source.GetUniformRelic(player.RunState).CanonicalInstance;
        if (currentModel?.CanonicalInstance?.Id == replacement.Id)
        {
            return;
        }

        RelicModel mutable = replacement.ToMutable();
        MerchantRelicModelField(__instance) = mutable;
        SaveManager.Instance.MarkRelicAsSeen(mutable);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MerchantPotionEntry), nameof(MerchantPotionEntry.CalcCost))]
    private static void MerchantPotionEntry_CalcCost_Prefix(MerchantPotionEntry __instance)
    {
        if (!WhatIfReplacementContext.ShouldReplaceShopPotions())
        {
            return;
        }

        Player player = PlayerField(__instance);
        var source = WhatIfUniformSourceResolver.FindUniformPotionSource(player.RunState);
        if (source == null)
        {
            return;
        }

        PotionModel? currentModel = MerchantPotionModelField(__instance);
        PotionModel replacement = source.GetUniformPotion(player.RunState).CanonicalInstance;
        if (currentModel?.CanonicalInstance?.Id == replacement.Id)
        {
            return;
        }

        PotionModel mutable = replacement.ToMutable();
        MerchantPotionModelField(__instance) = mutable;
        SaveManager.Instance.MarkPotionAsSeen(mutable);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PotionReward), nameof(PotionReward.Populate))]
    private static void PotionReward_Populate_Postfix(PotionReward __instance)
    {
        if (!WhatIfReplacementContext.ShouldReplacePotionRewards())
        {
            return;
        }

        Player player = __instance.Player;
        var source = WhatIfUniformSourceResolver.FindUniformPotionSource(player.RunState);
        if (source == null)
        {
            return;
        }

        PotionModel replacement = source.GetUniformPotion(player.RunState).ToMutable();
        PotionRewardModelField(__instance) = replacement;
    }
}
