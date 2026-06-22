using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[RegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfAllRelics")]
public class WhatIfAllRelics : WhatIfRelicModel
{
    private static readonly Assembly LocalAssembly = typeof(WhatIfAllRelics).Assembly;
    private static readonly Lazy<HashSet<string>> LegacyWhatIfRelicTypeNames = new(
        static () => [.. Interop.YuWanInterop.GetRegisteredWhatIfRelicTypeNames().Where(static name => !string.IsNullOrWhiteSpace(name))],
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<HashSet<string>> SupplementalRelicTypeNames = new(
        static () => [.. Interop.YuWanInterop.GetSupplementalWhatIfRelicTypeNames().Where(static name => !string.IsNullOrWhiteSpace(name))],
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly string[] RewardEffectHookNames =
    [
        nameof(TryModifyCardRewardAlternatives),
        nameof(ShouldAllowSelectingMoreCardRewards),
        nameof(ModifyCardRewardCreationOptions),
        nameof(TryModifyCardRewardOptions),
        nameof(TryModifyRewards)
    ];

    public WhatIfAllRelics() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
        {
            return;
        }

        var allCandidateRelics = ModelDb.AllRelics
            .Where(relic => relic.Id != Id
                && Owner.GetRelicById(relic.Id) == null
                && relic.IsAllowed(Owner.RunState)
                && IsWhitelistedRelicSource(relic))
            .ToList();

        var skippedPickupEffectRelics = allCandidateRelics
            .Where(HasPickupEffect)
            .ToList();

        var skippedRewardEffectRelics = allCandidateRelics
            .Where(HasRewardEffect)
            .ToList();

        var relicsToAdd = allCandidateRelics
            .Where(relic => !HasPickupEffect(relic) && !HasRewardEffect(relic))
            .ToList();

        int added = 0;
        int failed = 0;
        int skippedPickupEffect = skippedPickupEffectRelics.Count;
        int skippedRewardEffect = skippedRewardEffectRelics.Count;

        Entry.Logger.Info($"[WhatIfAllRelics] Preparing relic grant. Candidates={allCandidateRelics.Count}, ToAdd={relicsToAdd.Count}, SkippedPickupEffect={skippedPickupEffect}, SkippedRewardEffect={skippedRewardEffect}");

        if (skippedPickupEffect > 0)
        {
            var previewIds = skippedPickupEffectRelics
                .Take(12)
                .Select(relic => relic.Id.Entry)
                .ToArray();
            Entry.Logger.Info($"[WhatIfAllRelics] Skipped pickup-effect relics: {string.Join(", ", previewIds)}{(skippedPickupEffectRelics.Count > previewIds.Length ? ", ..." : string.Empty)}");
        }

        if (skippedRewardEffect > 0)
        {
            var previewIds = skippedRewardEffectRelics
                .Except(skippedPickupEffectRelics)
                .Take(12)
                .Select(relic => relic.Id.Entry)
                .ToArray();
            if (previewIds.Length > 0)
            {
                Entry.Logger.Info($"[WhatIfAllRelics] Skipped reward-effect relics: {string.Join(", ", previewIds)}{(skippedRewardEffectRelics.Count > previewIds.Length ? ", ..." : string.Empty)}");
            }
        }

        foreach (var relicModel in relicsToAdd)
        {
            try
            {
                var relic = relicModel.ToMutable();
                relic.FloorAddedToDeck = 1;
                Owner.AddRelicInternal(relic);
                added++;
            }
            catch (Exception ex)
            {
                failed++;
                Entry.Logger.Error($"[WhatIfAllRelics] Failed to add {relicModel.Id.Entry}: {ex.Message}");
            }
        }

        Entry.Logger.Info($"[WhatIfAllRelics] Finished granting relics. Added={added}, Failed={failed}, SkippedPickupEffect={skippedPickupEffect}, SkippedRewardEffect={skippedRewardEffect}");
    }

    private static bool HasPickupEffect(RelicModel relicModel)
    {
        var method = relicModel.GetType().GetMethod(
            nameof(AfterObtained),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return method != null && method.DeclaringType != null && method.DeclaringType != typeof(RelicModel);
    }

    private static bool HasRewardEffect(RelicModel relicModel)
    {
        var relicType = relicModel.GetType();
        return RewardEffectHookNames.Any(hookName =>
        {
            var method = relicType.GetMethod(hookName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method != null && method.DeclaringType == relicType;
        });
    }

    private static bool IsWhitelistedRelicSource(RelicModel relicModel)
    {
        var relicType = relicModel.GetType();
        var typeName = relicType.FullName;
        if (typeName != null && LegacyWhatIfRelicTypeNames.Value.Contains(typeName))
        {
            return false;
        }

        if (relicType.Assembly == LocalAssembly)
        {
            return true;
        }

        var assemblyName = relicType.Assembly.GetName().Name;
        if (!string.IsNullOrEmpty(assemblyName) &&
            assemblyName.StartsWith("MegaCrit.Sts2.", StringComparison.Ordinal))
        {
            return true;
        }

        if (relicType.Namespace?.StartsWith("MegaCrit.Sts2.", StringComparison.Ordinal) == true)
        {
            return true;
        }

        return typeName != null && SupplementalRelicTypeNames.Value.Contains(typeName);
    }
}




