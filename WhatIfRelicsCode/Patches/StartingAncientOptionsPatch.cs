using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using WhatIfRelics.WhatIfRelicsCode.Localization;
using WhatIfRelics.WhatIfRelicsCode.Relics;
using WhatIfRelics.WhatIfRelicsCode.Utils;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch]
public static class StartingAncientOptionsPatch
{
    private const string WhatIfEntryDescriptionKey = "WHAT_IF_RELICS_WHAT_IF_ENTRY.description";
    private const string WhatIfSkipKey = "WHAT_IF_RELICS_WHAT_IF_SKIP";

    private static readonly Dictionary<AncientEventModel, List<EventOption>> OriginalOptions = [];
    private static readonly Dictionary<AncientEventModel, LocString> OriginalDescriptions = [];
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
        if (__instance is not AncientEventModel ancient)
        {
            return;
        }

        if (!ShouldInjectWhatIfOptions(ancient))
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
        InjectedAncients.Add(ancient);

        var replacementDescription = new LocString("relics", WhatIfEntryDescriptionKey);
        var replacementOptions = CreateWhatIfOptions(ancient);
        Entry.Logger.Info($"[StartingAncientOptionsPatch] Injecting WhatIf start options for {ancient.Id.Entry}");

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

    private static bool ShouldInjectWhatIfOptions(AncientEventModel ancient)
    {
        if (SuppressInjection.Contains(ancient) || OriginalOptions.ContainsKey(ancient))
        {
            return false;
        }

        if (InjectedAncients.Contains(ancient))
        {
            return false;
        }

        if (!WhatIfReplacementContext.IsWhatIfSelectionEnabled())
        {
            return false;
        }

        if (ancient.Owner?.RunState == null)
        {
            return false;
        }

        var runState = ancient.Owner.RunState;
        return runState.CurrentActIndex == 0 && runState.TotalFloor <= 1;
    }

    private static IReadOnlyList<EventOption> CreateWhatIfOptions(AncientEventModel ancient)
    {
        var selectedRelics = SelectDeterministicWhatIfRelics(ancient);
        var options = new List<EventOption>(selectedRelics.Count + 1);

        foreach (var relic in selectedRelics)
        {
            var mutable = relic.ToMutable();
            var description = BuildRelicOptionDescription(mutable);
            options.Add(new EventOption(
                ancient,
                async () =>
                {
                    await RelicCmd.Obtain(mutable, ancient.Owner!);
                    RestoreNormalOptions(ancient);
                },
                mutable.Title,
                description,
                mutable.Id.Entry + ".NEOW",
                mutable.HoverTipsExcludingRelic).WithRelic(mutable));
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

    private static IReadOnlyList<RelicModel> SelectDeterministicWhatIfRelics(AncientEventModel ancient)
    {
        var candidates = GetWhatIfCandidates(ancient);
        var seed = ancient.Owner?.RunState?.Rng.StringSeed ?? string.Empty;

        return candidates
            .OrderBy(relic => ComputeDeterministicSelectionKey(seed, relic.Id.Entry))
            .ThenBy(static relic => relic.Id.Entry, StringComparer.Ordinal)
            .Take(3)
            .ToList();
    }

    private static List<RelicModel> GetWhatIfCandidates(AncientEventModel ancient)
    {
        var pool = ModelDb.RelicPool<WhatIfRelicPool>();
        var runState = ancient.Owner?.RunState;
        var isMultiplayer = runState?.Players.Count > 1;

        IEnumerable<RelicModel> candidates = pool.AllRelics
            .GroupBy(static relic => relic.Id.Entry, StringComparer.Ordinal)
            .Select(static group => group.First());

        if (isMultiplayer)
        {
            candidates = candidates.Where(static relic => relic is not WhatIfAllRelics);
        }

        return candidates.ToList();
    }

    private static LocString BuildRelicOptionDescription(RelicModel relic)
    {
        if (relic is WhatIfRelicModel whatIfRelic)
        {
            return WhatIfRelicDescriptionBuilder.BuildLocString(whatIfRelic);
        }

        var description = relic.Description;
        foreach (var dynamicVar in relic.DynamicVars.Values)
        {
            description.Add(dynamicVar);
        }

        return description;
    }

    private static ulong ComputeDeterministicSelectionKey(string seed, string relicId)
    {
        var bytes = Encoding.UTF8.GetBytes($"{seed}|WHAT_IF_RELICS_NEOW|{relicId}");
        var hash = SHA256.HashData(bytes);
        return BitConverter.ToUInt64(hash, 0);
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

        SetEventState(ancient, description, originalOptions);
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
    }
}
