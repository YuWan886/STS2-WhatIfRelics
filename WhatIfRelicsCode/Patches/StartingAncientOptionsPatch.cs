using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using WhatIfRelics.WhatIfRelicsCode.Localization;
using WhatIfRelics.WhatIfRelicsCode.Relics;
using WhatIfRelics.WhatIfRelicsCode.Utils;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch]
public static class StartingAncientOptionsPatch
{
    private const string WhatIfEntryDescriptionKey = "WHAT_IF_RELICS_WHAT_IF_ENTRY.description";
    private const string WhatIfSkipKey = "WHAT_IF_RELICS_WHAT_IF_SKIP";
    private const string WhatIfPreviousPageKey = "WHAT_IF_RELICS_WHAT_IF_PREVIOUS_PAGE";
    private const string WhatIfNextPageKey = "WHAT_IF_RELICS_WHAT_IF_NEXT_PAGE";
    private const int MaxRelicsPerPage = 5;
    private const int RelicsWithOneNavigation = 4;
    private const int RelicsWithTwoNavigations = 3;

    private static readonly Dictionary<AncientEventModel, List<EventOption>> OriginalOptions = [];
    private static readonly Dictionary<AncientEventModel, LocString> OriginalDescriptions = [];
    private static readonly Dictionary<AncientEventModel, int> RefreshCounts = [];
    private static readonly Dictionary<AncientEventModel, int> PageIndices = [];
    private static readonly HashSet<AncientEventModel> SuppressInjection = [];
    private static readonly HashSet<AncientEventModel> InjectedAncients = [];

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        return WhatIfReflectionHelper.GetPrivateMethod(
            typeof(EventModel),
            "SetEventState",
            [typeof(LocString), typeof(IEnumerable<EventOption>)]);
    }

    [HarmonyPostfix]
    private static void Postfix(EventModel __instance, LocString description, IEnumerable<EventOption> eventOptions)
    {
        if (__instance is not AncientEventModel ancient || !ShouldInjectWhatIfOptions(ancient))
        {
            return;
        }

        var originalOptions = eventOptions.ToList();
        if (originalOptions.Count == 0)
        {
            return;
        }

        OriginalOptions[ancient] = originalOptions;
        OriginalDescriptions[ancient] = description;
        RefreshCounts[ancient] = 0;
        PageIndices[ancient] = 0;
        InjectedAncients.Add(ancient);
        Entry.Logger.Info($"[StartingAncientOptionsPatch] Injecting WhatIf start options for {ancient.Id.Entry}");
        ShowWhatIfOptions(ancient);
    }

    private static bool ShouldInjectWhatIfOptions(AncientEventModel ancient)
    {
        if (SuppressInjection.Contains(ancient)
            || OriginalOptions.ContainsKey(ancient)
            || InjectedAncients.Contains(ancient)
            || !WhatIfReplacementContext.IsWhatIfSelectionEnabled()
            || WhatIfReplacementContext.GetWhatIfRelicChoiceCount() == 0
            || ancient.Owner?.RunState == null)
        {
            return false;
        }

        var runState = ancient.Owner.RunState;
        return runState.CurrentActIndex == 0 && runState.TotalFloor <= 1;
    }

    private static IReadOnlyList<EventOption> CreateWhatIfOptions(AncientEventModel ancient)
    {
        var selectedRelics = SelectDeterministicWhatIfRelics(ancient);
        int firstPageRelics = selectedRelics.Count <= MaxRelicsPerPage
            ? MaxRelicsPerPage
            : RelicsWithOneNavigation;
        int pageIndex = PageIndices.GetValueOrDefault(ancient);
        int firstRelicIndex = GetFirstRelicIndex(pageIndex, firstPageRelics);
        if (firstRelicIndex >= selectedRelics.Count)
        {
            pageIndex = 0;
            firstRelicIndex = 0;
            PageIndices[ancient] = 0;
        }

        int relicsOnPage = pageIndex == 0 ? firstPageRelics : RelicsWithTwoNavigations;
        bool hasPreviousPage = pageIndex > 0;
        bool hasNextPage = firstRelicIndex + relicsOnPage < selectedRelics.Count;
        var options = new List<EventOption>(MaxRelicsPerPage + 3);

        foreach (var relic in selectedRelics.Skip(firstRelicIndex).Take(relicsOnPage))
        {
            var mutable = relic.ToMutable();
            options.Add(new EventOption(
                ancient,
                async () =>
                {
                    await RelicCmd.Obtain(mutable, ancient.Owner!);
                    RestoreNormalOptions(ancient);
                },
                mutable.Title,
                BuildRelicOptionDescription(mutable),
                mutable.Id.Entry + ".NEOW",
                mutable.HoverTipsExcludingRelic).WithRelic(mutable));
        }

        if (hasPreviousPage)
        {
            options.Add(CreatePageNavigationOption(ancient, WhatIfPreviousPageKey, pageIndex - 1));
        }

        if (hasNextPage)
        {
            options.Add(CreatePageNavigationOption(ancient, WhatIfNextPageKey, pageIndex + 1));
        }

        options.Add(new EventOption(
            ancient,
            () =>
            {
                RestoreNormalOptions(ancient);
                return Task.CompletedTask;
            },
            new LocString("relics", $"{WhatIfSkipKey}.title"),
            new LocString("relics", $"{WhatIfSkipKey}.description"),
            WhatIfSkipKey,
            Array.Empty<IHoverTip>()));

        return options;
    }

    private static int GetFirstRelicIndex(int pageIndex, int firstPageRelics)
    {
        return pageIndex == 0
            ? 0
            : firstPageRelics + (pageIndex - 1) * RelicsWithTwoNavigations;
    }

    private static EventOption CreatePageNavigationOption(AncientEventModel ancient, string key, int destinationPage)
    {
        return new EventOption(
            ancient,
            () =>
            {
                PageIndices[ancient] = destinationPage;
                ShowWhatIfOptions(ancient);
                return Task.CompletedTask;
            },
            new LocString("relics", $"{key}.title"),
            new LocString("relics", $"{key}.description"),
            key,
            Array.Empty<IHoverTip>());
    }

    private static IReadOnlyList<RelicModel> SelectDeterministicWhatIfRelics(AncientEventModel ancient)
    {
        var candidates = GetWhatIfCandidates(ancient);
        var seed = ancient.Owner?.RunState?.Rng.StringSeed ?? string.Empty;
        var refreshCount = RefreshCounts.TryGetValue(ancient, out var value) ? value : 0;

        return candidates
            .OrderBy(relic => ComputeDeterministicSelectionKey(seed, relic.Id.Entry, refreshCount))
            .ThenBy(static relic => relic.Id.Entry, StringComparer.Ordinal)
            .Take(WhatIfReplacementContext.GetWhatIfRelicChoiceCount())
            .ToList();
    }

    private static List<RelicModel> GetWhatIfCandidates(AncientEventModel ancient)
    {
        var pool = ModelDb.RelicPool<WhatIfRelicPool>();
        var runState = ancient.Owner?.RunState;
        IEnumerable<RelicModel> candidates = pool.AllRelics
            .GroupBy(static relic => relic.Id.Entry, StringComparer.Ordinal)
            .Select(static group => group.First());

        if (runState?.Players.Count > 1)
        {
            var orderedPlayers = runState.Players.OrderBy(static player => player.NetId).ToList();
            var lifeLinkOwner = orderedPlayers[0];
            var sharedEnergyOwner = orderedPlayers[(int)(
                ComputeDeterministicSelectionKey(runState.Rng.StringSeed, "WHAT_IF_SHARED_ENERGY_OWNER", 0)
                % (ulong)orderedPlayers.Count)];
            bool hasLifeLink = runState.Players.Any(player => player.GetRelic<WhatIfLifeLink>() != null);
            bool hasSharedEnergy = runState.Players.Any(player => player.GetRelic<WhatIfSharedEnergy>() != null);

            candidates = candidates.Where(relic =>
                relic is not WhatIfAllRelics
                && relic is not WhatIfGoSecond
                && (relic is not WhatIfLifeLink || (!hasLifeLink && ancient.Owner == lifeLinkOwner))
                && (relic is not WhatIfSharedEnergy || (!hasSharedEnergy && ancient.Owner == sharedEnergyOwner)));
        }
        else
        {
            candidates = candidates.Where(static relic => relic is not WhatIfLifeLink and not WhatIfSharedEnergy);
        }

        return candidates.ToList();
    }

    private static LocString BuildRelicOptionDescription(RelicModel relic)
    {
        if (relic is WhatIfRelicModel whatIfRelic)
        {
            return WhatIfRelicDescriptionBuilder.BuildOptionLocString(whatIfRelic);
        }

        var description = relic.Description;
        foreach (var dynamicVar in relic.DynamicVars.Values)
        {
            description.Add(dynamicVar);
        }

        return description;
    }

    private static ulong ComputeDeterministicSelectionKey(string seed, string relicId, int refreshCount)
    {
        var bytes = Encoding.UTF8.GetBytes($"{seed}|WHAT_IF_RELICS_NEOW|{refreshCount}|{relicId}");
        var hash = SHA256.HashData(bytes);
        return BitConverter.ToUInt64(hash, 0);
    }

    internal static CmdResult RefreshCurrentWhatIfOptions()
    {
        if (RunManager.Instance?.State?.CurrentRoom is not EventRoom eventRoom)
        {
            return new CmdResult(success: false, "You are not currently in an event room.");
        }

        if (eventRoom.LocalMutableEvent is not AncientEventModel ancient)
        {
            return new CmdResult(success: false, "The current event is not an ancient event.");
        }

        if (!OriginalOptions.ContainsKey(ancient))
        {
            return new CmdResult(success: false, "You are not currently on the What If relic selection page.");
        }

        var candidates = GetWhatIfCandidates(ancient);
        int choiceCount = WhatIfReplacementContext.GetWhatIfRelicChoiceCount();
        if (choiceCount == 0 || candidates.Count <= choiceCount)
        {
            return new CmdResult(success: false, "There are not enough candidate What If relics to generate a different option set.");
        }

        var currentIds = GetDisplayedRelicIds(ancient);
        var refreshCount = RefreshCounts.TryGetValue(ancient, out var value) ? value : 0;
        var maxAttempts = Math.Max(candidates.Count * 2, 8);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            RefreshCounts[ancient] = refreshCount + attempt;
            var nextIds = GetSelectedRelicIds(ancient);
            if (currentIds.SetEquals(nextIds))
            {
                continue;
            }

            PageIndices[ancient] = 0;
            ShowWhatIfOptions(ancient);
            return new CmdResult(success: true, $"Refreshed What If relic options. Refresh round: {RefreshCounts[ancient]}.");
        }

        RefreshCounts[ancient] = refreshCount;
        return new CmdResult(success: false, "Failed to find a different set of What If relic options.");
    }

    private static void ShowWhatIfOptions(AncientEventModel ancient)
    {
        var replacementDescription = new LocString("relics", WhatIfEntryDescriptionKey);
        var replacementOptions = CreateWhatIfOptions(ancient);

        SuppressInjection.Add(ancient);
        try
        {
            SetEventState(ancient, replacementDescription, replacementOptions);
        }
        finally
        {
            SuppressInjection.Remove(ancient);
        }
    }

    private static HashSet<string> GetDisplayedRelicIds(AncientEventModel ancient)
    {
        return ancient.CurrentOptions
            .Where(static option => option.Relic != null)
            .Select(static option => option.Relic!.Id.Entry)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> GetSelectedRelicIds(AncientEventModel ancient)
    {
        return SelectDeterministicWhatIfRelics(ancient)
            .Select(static relic => relic.Id.Entry)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static void RestoreNormalOptions(AncientEventModel ancient)
    {
        if (!OriginalOptions.TryGetValue(ancient, out var originalOptions) || originalOptions.Count == 0)
        {
            ClearStoredState(ancient);
            return;
        }

        var description = OriginalDescriptions.TryGetValue(ancient, out var originalDescription)
            ? originalDescription
            : ancient.InitialDescription;

        bool restored = SetEventState(ancient, description, originalOptions);
        Entry.Logger.Info($"[StartingAncientOptionsPatch] Restored original options: success={restored}, count={ancient.CurrentOptions.Count}");
        ClearStoredState(ancient);
    }

    private static bool SetEventState(EventModel eventModel, LocString description, IEnumerable<EventOption> options)
    {
        return WhatIfReflectionHelper.CallPrivateMethod(eventModel, "SetEventState", description, options);
    }

    private static void ClearStoredState(AncientEventModel ancient)
    {
        OriginalOptions.Remove(ancient);
        OriginalDescriptions.Remove(ancient);
        RefreshCounts.Remove(ancient);
        PageIndices.Remove(ancient);
    }
}
