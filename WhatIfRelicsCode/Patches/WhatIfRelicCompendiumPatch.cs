using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using WhatIfRelics.WhatIfRelicsCode.Relics;
using WhatIfRelics.WhatIfRelicsCode.Utils;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch]
internal static class WhatIfRelicCompendiumPatch
{
    private const string WhatIfRelicCollectionTable = "relic_collection";
    private const string WhatIfRelicSubcategoryKey = "WHATIF_SUBCATEGORY";

    private static readonly ConditionalWeakTable<NRelicCollection, NRelicCollectionCategory> ManagedWhatIfRootCategories = new();
    private static readonly ConditionalWeakTable<NRelicCollectionCategory, object> ManagedWhatIfCategoryMarkers = new();

    private static readonly FieldInfo? HeaderLabelField =
        AccessTools.Field(typeof(NRelicCollectionCategory), "_headerLabel");

    private static readonly FieldInfo? SpacerField =
        AccessTools.Field(typeof(NRelicCollectionCategory), "_spacer");

    private static readonly FieldInfo? SubCategoriesField =
        AccessTools.Field(typeof(NRelicCollectionCategory), "_subCategories");

    private static readonly FieldInfo? RelicsField =
        AccessTools.Field(typeof(NRelicCollection), "_relics");

    private static readonly FieldInfo? CategoriesField =
        AccessTools.Field(typeof(NRelicCollection), "_categories");

    private static readonly MethodInfo? LoadSubcategoryMethod =
        WhatIfReflectionHelper.GetPrivateMethod(
            typeof(NRelicCollectionCategory),
            "LoadSubcategory",
            [typeof(NRelicCollection), typeof(LocString), typeof(IEnumerable<RelicModel>), typeof(HashSet<RelicModel>), typeof(HashSet<RelicModel>)]);

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllSharedRelicPools), MethodType.Getter)]
    [HarmonyPostfix]
    private static IEnumerable<RelicPoolModel> AddWhatIfPool(IEnumerable<RelicPoolModel> __result)
    {
        var pool = TryGetWhatIfPool();
        return pool == null ? __result : [.. __result, pool];
    }

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllRelics), MethodType.Getter)]
    [HarmonyPostfix]
    private static IEnumerable<RelicModel> AddWhatIfRelics(IEnumerable<RelicModel> __result)
    {
        var pool = TryGetWhatIfPool();
        return pool == null
            ? __result
            : __result.Concat(pool.AllRelics).DistinctBy(static relic => relic.Id);
    }

    [HarmonyPatch(typeof(NRelicCollection), "LoadRelics")]
    [HarmonyPostfix]
    private static void AddWhatIfCategoryAsync(
        NRelicCollection __instance,
        NRelicCollectionCategory ____starter,
        NRelicCollectionCategory ____common,
        NRelicCollectionCategory ____uncommon,
        NRelicCollectionCategory ____rare,
        NRelicCollectionCategory ____shop,
        NRelicCollectionCategory ____ancient,
        NRelicCollectionCategory ____event,
        ref Task __result)
    {
        __result = ContinueLoadRelicsAsync(
            __result,
            __instance,
            ____rare,
            ____event,
            ____ancient);
    }

    private static async Task ContinueLoadRelicsAsync(
        Task originalTask,
        NRelicCollection collection,
        NRelicCollectionCategory rareCategory,
        NRelicCollectionCategory eventCategory,
        NRelicCollectionCategory ancientCategory)
    {
        await originalTask;

        try
        {
            AddWhatIfEventCategory(collection, rareCategory, eventCategory);
            BackfillWhatIfAncientSubcategories(collection, ancientCategory);
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"[WhatIfRelicCompendiumPatch] Failed to build WhatIf compendium categories: {ex}");
        }
    }

    [HarmonyPatch(typeof(NRelicCollection), "ClearRelics")]
    [HarmonyPostfix]
    private static void ClearWhatIfRootCategory(NRelicCollection __instance)
    {
        if (!ManagedWhatIfRootCategories.TryGetValue(__instance, out NRelicCollectionCategory? category))
        {
            return;
        }

        if (GodotObject.IsInstanceValid(category))
        {
            category.ClearRelics();
            category.Visible = false;
        }
    }

    [HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelicNodes")]
    [HarmonyPrefix]
    private static void FilterWhatIfRelicsFromVanillaCategories(
        NRelicCollectionCategory __instance,
        ref IEnumerable<RelicModel> relics)
    {
        if (IsManagedWhatIfCategory(__instance))
        {
            return;
        }

        relics = relics.Where(static relic => relic is not WhatIfRelicModel);
    }

    private static void AddWhatIfEventCategory(
        NRelicCollection collection,
        NRelicCollectionCategory rareCategory,
        NRelicCollectionCategory eventCategory)
    {
        var pool = TryGetWhatIfPool();
        if (pool == null)
        {
            return;
        }

        var whatIfRelics = pool.AllRelics
            .Select(static relic => relic.CanonicalInstance)
            .DistinctBy(static relic => relic.Id)
            .OrderBy(static relic => relic.Title.GetFormattedText(), LocManager.Instance.StringComparer)
            .ToArray();

        if (whatIfRelics.Length == 0)
        {
            return;
        }

        NRelicCollectionCategory? rootCategory = EnsureWhatIfRootCategory(collection, rareCategory, eventCategory);
        if (rootCategory == null)
        {
            return;
        }

        rootCategory.ClearRelics();
        rootCategory.Visible = false;

        if (RelicsField?.GetValue(collection) is List<RelicModel> visibleRelics)
        {
            foreach (var relic in whatIfRelics)
            {
                if (!visibleRelics.Contains(relic))
                {
                    visibleRelics.Add(relic);
                }
            }
        }

        LocString whatIfHeader = new(WhatIfRelicCollectionTable, WhatIfRelicSubcategoryKey);
        var discoveredRelics = SaveManager.Instance.Progress.DiscoveredRelics
            .Select(ModelDb.GetByIdOrNull<RelicModel>)
            .OfType<RelicModel>()
            .ToHashSet();
        discoveredRelics.UnionWith(whatIfRelics);
        var unlockedRelics = SaveManager.Instance.GenerateUnlockStateFromProgress().Relics.ToHashSet();

        rootCategory.Visible = true;
        LoadSubcategoryMethod?.Invoke(
            rootCategory,
            [collection, whatIfHeader, whatIfRelics, discoveredRelics, unlockedRelics]);
    }

    private static void BackfillWhatIfAncientSubcategories(
        NRelicCollection collection,
        NRelicCollectionCategory ancientCategory)
    {
        var sharedAncients = ModelDb.AllSharedAncients
            .Where(IsWhatIfAncient)
            .ToArray();
        if (sharedAncients.Length == 0)
        {
            return;
        }

        UnlockState unlockState = SaveManager.Instance.GenerateUnlockStateFromProgress();
        var seenRelics = SaveManager.Instance.Progress.DiscoveredRelics
            .Select(ModelDb.GetByIdOrNull<RelicModel>)
            .OfType<RelicModel>()
            .ToHashSet();
        var unlockedRelics = unlockState.Relics.ToHashSet();

        foreach (AncientEventModel ancient in sharedAncients)
        {
            if (!unlockState.SharedAncients.Contains(ancient))
            {
                continue;
            }

            if (HasExistingSubcategory(ancientCategory, ancient.Title.GetFormattedText()))
            {
                continue;
            }

            RelicModel[] relics = ancient.AllPossibleOptions
                .Select(static option => option.Relic?.CanonicalInstance)
                .OfType<RelicModel>()
                .Where(static relic => relic is WhatIfRelicModel)
                .DistinctBy(static relic => relic.Id)
                .OrderBy(static relic => relic.Title.GetFormattedText(), LocManager.Instance.StringComparer)
                .ToArray();
            if (relics.Length == 0)
            {
                continue;
            }

            seenRelics.UnionWith(relics);

            NRelicCollectionCategory? subcategory = CreateSubcategoryNode();
            if (subcategory == null)
            {
                return;
            }

            RegisterSubcategory(ancientCategory, subcategory);

            bool revealed = SaveManager.Instance.Progress.AncientStats.ContainsKey(ancient.Id)
                || relics.Any(seenRelics.Contains);
            LocString unknownAncient = new("relic_collection", "UNKNOWN_ANCIENT");
            LocString header = new("relic_collection", "ANCIENT_SUBCATEGORY");
            header.Add("Ancient", revealed ? ancient.Title : unknownAncient);

            LoadSubcategoryMethod?.Invoke(subcategory, [collection, header, relics, seenRelics, unlockedRelics]);
            subcategory.LoadIcon(ancient.RunHistoryIcon);
        }
    }

    private static bool IsWhatIfAncient(AncientEventModel ancient)
    {
        return ancient.AllPossibleOptions
            .Select(static option => option.Relic?.CanonicalInstance)
            .OfType<RelicModel>()
            .Any(static relic => relic is WhatIfRelicModel);
    }

    private static bool HasExistingSubcategory(NRelicCollectionCategory category, string expectedHeaderText)
    {
        if (SubCategoriesField?.GetValue(category) is not IEnumerable<NRelicCollectionCategory> subcategories)
        {
            return false;
        }

        foreach (NRelicCollectionCategory subcategory in subcategories)
        {
            if (!GodotObject.IsInstanceValid(subcategory))
            {
                continue;
            }

            if (HeaderLabelField?.GetValue(subcategory) is not MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel headerLabel)
            {
                continue;
            }

            if (headerLabel.Text.Contains(expectedHeaderText, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static NRelicCollectionCategory? EnsureWhatIfRootCategory(
        NRelicCollection collection,
        NRelicCollectionCategory rareCategory,
        NRelicCollectionCategory eventCategory)
    {
        if (ManagedWhatIfRootCategories.TryGetValue(collection, out NRelicCollectionCategory? category)
            && GodotObject.IsInstanceValid(category))
        {
            category.ClearRelics();
            category.Visible = true;
            return category;
        }

        NRelicCollectionCategory? newCategory = CreateSubcategoryNode();
        if (newCategory == null)
        {
            return null;
        }

        newCategory.Name = "WhatIfRelicCollectionCategory";
        newCategory.Visible = false;
        ManagedWhatIfRootCategories.Add(collection, newCategory);
        ManagedWhatIfCategoryMarkers.Add(newCategory, new object());
        RegisterCollectionCategory(collection, newCategory, rareCategory, eventCategory);
        return newCategory;
    }

    private static NRelicCollectionCategory? CreateSubcategoryNode()
    {
        PackedScene? scene = PreloadManager.Cache.GetScene(NRelicCollectionCategory.scenePath);
        return scene?.Instantiate<NRelicCollectionCategory>(PackedScene.GenEditState.Disabled);
    }

    private static void RegisterSubcategory(NRelicCollectionCategory parent, NRelicCollectionCategory subcategory)
    {
        parent.AddChildSafely(subcategory);
        ManagedWhatIfCategoryMarkers.Add(subcategory, new object());

        if (SubCategoriesField?.GetValue(parent) is List<NRelicCollectionCategory> subcategories)
        {
            subcategories.Add(subcategory);
        }

        int insertIndex = HeaderLabelField?.GetValue(parent) is Control headerLabel
            ? Math.Min(parent.GetChildCount() - 1, headerLabel.GetIndex() + parent.GetChildren().OfType<NRelicCollectionCategory>().Count())
            : parent.GetChildCount() - 1;
        parent.MoveChildSafely(subcategory, insertIndex);

        if (SpacerField?.GetValue(subcategory) is Control spacer)
        {
            spacer.Visible = true;
        }
    }

    private static void RegisterCollectionCategory(
        NRelicCollection collection,
        NRelicCollectionCategory category,
        NRelicCollectionCategory rareCategory,
        NRelicCollectionCategory eventCategory)
    {
        if (CategoriesField?.GetValue(collection) is List<NRelicCollectionCategory> categories
            && !categories.Contains(category))
        {
            int rareIndex = categories.IndexOf(rareCategory);
            int eventIndex = categories.IndexOf(eventCategory);
            int insertIndex = rareIndex >= 0
                ? Math.Min(categories.Count, rareIndex + 1)
                : (eventIndex >= 0 ? eventIndex : categories.Count);
            categories.Insert(insertIndex, category);
        }

        Control anchor = rareCategory.GetParent() == collection ? rareCategory : eventCategory;
        anchor.AddSibling(category, forceReadableName: true);

        if (SpacerField?.GetValue(category) is Control spacer)
        {
            spacer.Visible = true;
            spacer.CustomMinimumSize = new Vector2(spacer.CustomMinimumSize.X, Math.Max(spacer.CustomMinimumSize.Y, 28f));
        }
    }

    private static bool IsManagedWhatIfCategory(NRelicCollectionCategory category)
    {
        return ManagedWhatIfCategoryMarkers.TryGetValue(category, out _);
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
