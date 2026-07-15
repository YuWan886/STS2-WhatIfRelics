using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;

namespace WhatIfRelics.WhatIfRelicsCode.Networking;

internal static class WhatIfRelicsConfigSync
{
    private static INetGameService? _registeredNetService;
    private static bool _hasHostConfig;
    private static WhatIfRelicsConfigMessage _hostConfig;

    public static bool HasHostConfig => _hasHostConfig;

    public static void Register()
    {
        EnsureRegistered();

        RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(_ =>
        {
            EnsureRegistered();
            BroadcastHostConfig();
        }, replayCurrentState: false);

        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(_ =>
        {
            EnsureRegistered();
            BroadcastHostConfig();
        }, replayCurrentState: false);
    }

    public static void EnsureRegistered()
    {
        INetGameService? netService = RunManager.Instance?.NetService;
        if (_registeredNetService == netService)
        {
            return;
        }

        if (_registeredNetService != null)
        {
            _registeredNetService.UnregisterMessageHandler<WhatIfRelicsConfigMessage>(HandleConfigMessage);
        }

        _hasHostConfig = false;

        if (netService != null)
        {
            netService.RegisterMessageHandler<WhatIfRelicsConfigMessage>(HandleConfigMessage);
        }

        _registeredNetService = netService;
    }

    public static bool IsAuthority()
    {
        return GetNetGameType() is NetGameType.Singleplayer or NetGameType.Host;
    }

    public static bool IsMultiplayerClient()
    {
        return GetNetGameType() == NetGameType.Client;
    }

    public static void BroadcastHostConfig()
    {
        EnsureRegistered();

        if (_registeredNetService is not { IsConnected: true, Type: NetGameType.Host })
        {
            return;
        }

        WhatIfRelicsSettings settings = WhatIfRelicsSettingsPage.Current;
        Entry.Logger.Info(
            "WhatIfRelics broadcasting host config. " +
            $"enable={settings.EnableWhatIfRelics}, choiceCount={settings.StartingWhatIfRelicChoiceCount}, startingDeck={settings.ReplaceStartingDeck}, " +
            $"cardRewards={settings.ReplaceCardRewards}, relicRewards={settings.ReplaceRelicRewards}, " +
            $"potionRewards={settings.ReplacePotionRewards}, treasureRelics={settings.ReplaceTreasureRelics}, " +
            $"shopCards={settings.ReplaceShopCards}, shopRelics={settings.ReplaceShopRelics}, " +
            $"shopPotions={settings.ReplaceShopPotions}, scorchingSpireFloorInterval={settings.ScorchingSpireFloorInterval}");
        _registeredNetService.SendMessage(WhatIfRelicsConfigMessage.FromSettings(settings));
    }

    public static bool EffectiveEnableWhatIfRelics() => EffectiveValue(
        static config => config.EnableWhatIfRelics,
        static settings => settings.EnableWhatIfRelics);

    public static int EffectiveStartingWhatIfRelicChoiceCount() => EffectiveValue(
        static config => config.StartingWhatIfRelicChoiceCount,
        static settings => Math.Clamp(
            settings.StartingWhatIfRelicChoiceCount,
            WhatIfRelicsSettings.MinStartingWhatIfRelicChoiceCount,
            WhatIfRelicsSettings.MaxStartingWhatIfRelicChoiceCount));

    public static bool EffectiveReplaceStartingDeck() => EffectiveValue(
        static config => config.ReplaceStartingDeck,
        static settings => settings.ReplaceStartingDeck);

    public static bool EffectiveReplaceCardRewards() => EffectiveValue(
        static config => config.ReplaceCardRewards,
        static settings => settings.ReplaceCardRewards);

    public static bool EffectiveReplaceRelicRewards() => EffectiveValue(
        static config => config.ReplaceRelicRewards,
        static settings => settings.ReplaceRelicRewards);

    public static bool EffectiveReplacePotionRewards() => EffectiveValue(
        static config => config.ReplacePotionRewards,
        static settings => settings.ReplacePotionRewards);

    public static bool EffectiveReplaceTreasureRelics() => EffectiveValue(
        static config => config.ReplaceTreasureRelics,
        static settings => settings.ReplaceTreasureRelics);

    public static bool EffectiveReplaceShopCards() => EffectiveValue(
        static config => config.ReplaceShopCards,
        static settings => settings.ReplaceShopCards);

    public static bool EffectiveReplaceShopRelics() => EffectiveValue(
        static config => config.ReplaceShopRelics,
        static settings => settings.ReplaceShopRelics);

    public static bool EffectiveReplaceShopPotions() => EffectiveValue(
        static config => config.ReplaceShopPotions,
        static settings => settings.ReplaceShopPotions);

    public static int EffectiveScorchingSpireFloorInterval() => EffectiveValue(
        static config => config.ScorchingSpireFloorInterval,
        static settings => Math.Max(1, settings.ScorchingSpireFloorInterval));

    private static bool EffectiveValue(
        Func<WhatIfRelicsConfigMessage, bool> hostSelector,
        Func<WhatIfRelicsSettings, bool> localSelector)
    {
        if (IsMultiplayerClient() && _hasHostConfig)
        {
            return hostSelector(_hostConfig);
        }

        return localSelector(WhatIfRelicsSettingsPage.Current);
    }

    private static int EffectiveValue(
        Func<WhatIfRelicsConfigMessage, int> hostSelector,
        Func<WhatIfRelicsSettings, int> localSelector)
    {
        if (IsMultiplayerClient() && _hasHostConfig)
        {
            return hostSelector(_hostConfig);
        }

        return localSelector(WhatIfRelicsSettingsPage.Current);
    }

    private static void HandleConfigMessage(WhatIfRelicsConfigMessage message, ulong senderId)
    {
        _hostConfig = message;
        _hasHostConfig = true;
        Entry.Logger.Debug(
            $"WhatIfRelics received host config from {senderId}. " +
            $"enable={message.EnableWhatIfRelics}, choiceCount={message.StartingWhatIfRelicChoiceCount}, startingDeck={message.ReplaceStartingDeck}, " +
            $"cardRewards={message.ReplaceCardRewards}, relicRewards={message.ReplaceRelicRewards}, " +
            $"potionRewards={message.ReplacePotionRewards}, treasureRelics={message.ReplaceTreasureRelics}, " +
            $"shopCards={message.ReplaceShopCards}, shopRelics={message.ReplaceShopRelics}, " +
            $"shopPotions={message.ReplaceShopPotions}, scorchingSpireFloorInterval={message.ScorchingSpireFloorInterval}");
    }

    private static NetGameType GetNetGameType()
    {
        return RunManager.Instance?.NetService?.Type ?? NetGameType.None;
    }
}
