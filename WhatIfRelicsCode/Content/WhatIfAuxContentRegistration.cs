using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Content;
using WhatIfRelics.WhatIfRelicsCode.Cards;
using WhatIfRelics.WhatIfRelicsCode.Powers;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace WhatIfRelics.WhatIfRelicsCode.Content;

internal static class WhatIfAuxContentRegistration
{
    private static readonly Lock Sync = new();
    private static bool _initialized;
    private static bool _blasphemyCardRegistered;
    private static readonly HashSet<Type> RegisteredPowerTypes = [];

    public static void Initialize(string modId, Logger logger)
    {
        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
        }

        var registry = ModContentRegistry.For(modId);
        int registeredPowerCount = 0;

        lock (Sync)
        {
            if (!_blasphemyCardRegistered)
            {
                registry.RegisterCard(
                    typeof(EventCardPool),
                    typeof(WhatIfBlasphemyCard),
                    ModelPublicEntryOptions.FromFullPublicEntry("WHAT_IF_RELICS_CARD_WHAT_IF_BLASPHEMY_CARD"));
                _blasphemyCardRegistered = true;
            }

            registeredPowerCount += TryRegisterPower(registry, typeof(WhatIfBlasphemyPower));
            registeredPowerCount += TryRegisterPower(registry, typeof(WhatIfJumpSlashCriticalPower));
        }

        logger.Info($"[Content] Registered WhatIf aux content: card=1, addedPowers={registeredPowerCount}");
    }

    private static int TryRegisterPower(ModContentRegistry registry, Type powerType)
    {
        if (!RegisteredPowerTypes.Add(powerType))
        {
            return 0;
        }

        registry.RegisterPower(powerType);
        return 1;
    }
}
