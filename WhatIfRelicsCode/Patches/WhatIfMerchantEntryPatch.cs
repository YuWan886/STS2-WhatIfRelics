using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves;
using System.Runtime.CompilerServices;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch]
internal static class WhatIfMerchantEntryPatch
{
    private const string HextechModId = "HextechRunes";
    private const string HextechRandomForgeShopRelicTypeName = "HextechRunes.RandomForgeShopRelic";
    private static readonly ConditionalWeakTable<MerchantRelicEntry, Marker> HextechMerchantEntries = new();

    private static readonly AccessTools.FieldRef<MerchantEntry, Player> PlayerField =
        AccessTools.FieldRefAccess<MerchantEntry, Player>("_player");

    private static readonly AccessTools.FieldRef<MerchantRelicEntry, RelicModel?> MerchantRelicModelField =
        AccessTools.FieldRefAccess<MerchantRelicEntry, RelicModel?>("<Model>k__BackingField");

    private static readonly AccessTools.FieldRef<MerchantPotionEntry, PotionModel?> MerchantPotionModelField =
        AccessTools.FieldRefAccess<MerchantPotionEntry, PotionModel?>("<Model>k__BackingField");

    private static readonly AccessTools.FieldRef<PotionReward, PotionModel?> PotionRewardModelField =
        AccessTools.FieldRefAccess<PotionReward, PotionModel?>("<Potion>k__BackingField");

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MerchantRelicEntry), MethodType.Constructor, typeof(RelicModel), typeof(Player))]
    private static void MerchantRelicEntry_Constructor_Postfix(MerchantRelicEntry __instance, RelicModel relic)
    {
        if (IsHextechMerchantRelic(relic))
        {
            HextechMerchantEntries.GetValue(__instance, static _ => new Marker());
            Entry.Logger.Info($"[WhatIfMerchantEntryPatch] Marked Hextech merchant relic entry: type={relic.GetType().FullName} id={relic.Id.Entry}");
        }
    }

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
        if (HextechMerchantEntries.TryGetValue(__instance, out _) || IsHextechMerchantRelic(currentModel))
        {
            if (currentModel != null)
            {
                Entry.Logger.Info($"[WhatIfMerchantEntryPatch] Skipped Hextech merchant relic replacement: type={currentModel.GetType().FullName} id={currentModel.Id.Entry}");
            }
            return;
        }

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

    private static bool IsHextechMerchantRelic(RelicModel? relic)
    {
        if (relic == null)
        {
            return false;
        }

        string? typeName = relic.GetType().FullName;
        if (string.Equals(typeName, HextechRandomForgeShopRelicTypeName, StringComparison.Ordinal))
        {
            return true;
        }

        string? canonicalTypeName = relic.CanonicalInstance?.GetType().FullName;
        if (string.Equals(canonicalTypeName, HextechRandomForgeShopRelicTypeName, StringComparison.Ordinal))
        {
            return true;
        }

        ModelId modelId = relic.CanonicalInstance?.Id ?? relic.Id;
        return string.Equals(modelId.Category, HextechModId, StringComparison.Ordinal);
    }

    private sealed class Marker;
}
