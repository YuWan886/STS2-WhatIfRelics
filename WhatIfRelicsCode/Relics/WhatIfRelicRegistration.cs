using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using WhatIfRelics.WhatIfRelicsCode.Relics.YuWan;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

public static class WhatIfRelicRegistration
{
    private sealed record RegistrationCandidate(Type RelicType, RegisterRelicAttribute Attribute, bool RequiresYuWan);

    private static readonly Lock Sync = new();
    private static readonly HashSet<Type> RegisteredRelicTypes = [];
    private static readonly HashSet<Type> PendingYuWanRelicTypes = [];
    private static bool _sharedPoolRegistered;
    private static bool _yuWanAssemblyHookInstalled;
    private static string? _modId;
    private static Logger? _logger;
    private static Assembly? _assembly;

    public static void Initialize(string modId, Logger logger, Assembly assembly)
    {
        lock (Sync)
        {
            _modId = modId;
            _logger = logger;
            _assembly = assembly;
        }

        RegisterAvailableRelics();
        InstallYuWanAssemblyLoadHook();
    }

    private static void RegisterAvailableRelics()
    {
        string modId;
        Logger logger;
        Assembly assembly;

        lock (Sync)
        {
            if (_modId == null || _logger == null || _assembly == null)
            {
                return;
            }

            modId = _modId;
            logger = _logger;
            assembly = _assembly;
        }

        var registry = ModContentRegistry.For(modId);
        var candidates = DiscoverCandidates(assembly);
        var readyToRegister = new List<RegistrationCandidate>();
        int newlyPendingYuWan = 0;

        lock (Sync)
        {
            foreach (var candidate in candidates)
            {
                if (RegisteredRelicTypes.Contains(candidate.RelicType))
                {
                    continue;
                }

                if (!candidate.RequiresYuWan)
                {
                    readyToRegister.Add(candidate);
                    continue;
                }

                if (YuWanWhatIfRelicAvailability.CanRegister(candidate.RelicType))
                {
                    readyToRegister.Add(candidate);
                    continue;
                }

                if (PendingYuWanRelicTypes.Add(candidate.RelicType))
                {
                    newlyPendingYuWan++;
                }
            }
        }

        if (readyToRegister.Count == 0)
        {
            if (newlyPendingYuWan > 0 && ModContentRegistry.State == ContentRegistrationState.Open)
            {
                logger.Info($"[Content] Deferred YuWan WhatIf relics: pending={GetPendingYuWanCount()}");
            }

            return;
        }

        int registeredCount = 0;
        int registeredYuWanCount = 0;
        LogLevel? originalGenericLevel = Logger.logLevelTypeMap.TryGetValue(LogType.Generic, out var level)
            ? level
            : null;

        try
        {
            Logger.SetLogLevelForType(LogType.Generic, LogLevel.Warn);

            lock (Sync)
            {
                if (!_sharedPoolRegistered)
                {
                    registry.RegisterSharedRelicPool<WhatIfRelicPool>();
                    _sharedPoolRegistered = true;
                }
            }

            foreach (var candidate in readyToRegister)
            {
                lock (Sync)
                {
                    if (RegisteredRelicTypes.Contains(candidate.RelicType))
                    {
                        continue;
                    }
                }

                registry.RegisterRelic(
                    candidate.Attribute.PoolType,
                    candidate.RelicType,
                    ResolvePublicEntryOptions(candidate.Attribute));

                lock (Sync)
                {
                    RegisteredRelicTypes.Add(candidate.RelicType);
                    PendingYuWanRelicTypes.Remove(candidate.RelicType);
                }

                registeredCount++;
                if (candidate.RequiresYuWan)
                {
                    registeredYuWanCount++;
                }
            }
        }
        finally
        {
            Logger.SetLogLevelForType(LogType.Generic, originalGenericLevel);
        }

        logger.Info(
            $"[Content] Registered WhatIf relics: added={registeredCount}, yuwanAdded={registeredYuWanCount}, total={GetRegisteredCount()}, yuwanPending={GetPendingYuWanCount()}");
    }

    private static void InstallYuWanAssemblyLoadHook()
    {
        lock (Sync)
        {
            if (_yuWanAssemblyHookInstalled)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            _yuWanAssemblyHookInstalled = true;
        }
    }

    private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        string? assemblyName = args.LoadedAssembly.GetName().Name;
        if (!string.Equals(assemblyName, "YuWanCard", StringComparison.Ordinal))
        {
            return;
        }

        if (ModContentRegistry.State != ContentRegistrationState.Open)
        {
            lock (Sync)
            {
                _logger?.Warn(
                    $"[Content] YuWanCard loaded after WhatIf content registration froze. PendingYuWan={PendingYuWanRelicTypes.Count}");
            }

            return;
        }

        RegisterAvailableRelics();
    }

    private static int GetRegisteredCount()
    {
        lock (Sync)
        {
            return RegisteredRelicTypes.Count;
        }
    }

    private static int GetPendingYuWanCount()
    {
        lock (Sync)
        {
            return PendingYuWanRelicTypes.Count;
        }
    }

    private static List<RegistrationCandidate> DiscoverCandidates(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false })
            .Where(static type => typeof(WhatIfRelicModel).IsAssignableFrom(type))
            .Select(static type => new
            {
                Type = type,
                Attribute = type.GetCustomAttribute<RegisterRelicAttribute>()
            })
            .Where(static x => x.Attribute?.PoolType == typeof(WhatIfRelicPool))
            .Select(static x => new RegistrationCandidate(
                x.Type,
                x.Attribute!,
                typeof(IYuWanWhatIfRelic).IsAssignableFrom(x.Type)))
            .OrderBy(static x => x.Attribute.Order)
            .ThenBy(static x => x.RelicType.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static ModelPublicEntryOptions ResolvePublicEntryOptions(RegisterRelicAttribute attribute)
    {
        if (!string.IsNullOrWhiteSpace(attribute.FullPublicEntry))
        {
            return ModelPublicEntryOptions.FromFullPublicEntry(attribute.FullPublicEntry);
        }

        if (!string.IsNullOrWhiteSpace(attribute.StableEntryStem))
        {
            return ModelPublicEntryOptions.FromStem(attribute.StableEntryStem);
        }

        return ModelPublicEntryOptions.FromTypeName;
    }
}
