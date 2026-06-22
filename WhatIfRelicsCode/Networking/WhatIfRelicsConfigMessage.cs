using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace WhatIfRelics.WhatIfRelicsCode.Networking;

internal struct WhatIfRelicsConfigMessage : INetMessage, IPacketSerializable
{
    public required bool EnableWhatIfRelics;
    public required bool ReplaceStartingDeck;
    public required bool ReplaceCardRewards;
    public required bool ReplaceRelicRewards;
    public required bool ReplacePotionRewards;
    public required bool ReplaceTreasureRelics;
    public required bool ReplaceShopCards;
    public required bool ReplaceShopRelics;
    public required bool ReplaceShopPotions;

    public bool ShouldBroadcast => false;

    public bool ShouldBuffer => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Debug;

    public static WhatIfRelicsConfigMessage FromSettings(WhatIfRelicsSettings settings)
    {
        return new WhatIfRelicsConfigMessage
        {
            EnableWhatIfRelics = settings.EnableWhatIfRelics,
            ReplaceStartingDeck = settings.ReplaceStartingDeck,
            ReplaceCardRewards = settings.ReplaceCardRewards,
            ReplaceRelicRewards = settings.ReplaceRelicRewards,
            ReplacePotionRewards = settings.ReplacePotionRewards,
            ReplaceTreasureRelics = settings.ReplaceTreasureRelics,
            ReplaceShopCards = settings.ReplaceShopCards,
            ReplaceShopRelics = settings.ReplaceShopRelics,
            ReplaceShopPotions = settings.ReplaceShopPotions
        };
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteBool(EnableWhatIfRelics);
        writer.WriteBool(ReplaceStartingDeck);
        writer.WriteBool(ReplaceCardRewards);
        writer.WriteBool(ReplaceRelicRewards);
        writer.WriteBool(ReplacePotionRewards);
        writer.WriteBool(ReplaceTreasureRelics);
        writer.WriteBool(ReplaceShopCards);
        writer.WriteBool(ReplaceShopRelics);
        writer.WriteBool(ReplaceShopPotions);
    }

    public void Deserialize(PacketReader reader)
    {
        EnableWhatIfRelics = reader.ReadBool();
        ReplaceStartingDeck = reader.ReadBool();
        ReplaceCardRewards = reader.ReadBool();
        ReplaceRelicRewards = reader.ReadBool();
        ReplacePotionRewards = reader.ReadBool();
        ReplaceTreasureRelics = reader.ReadBool();
        ReplaceShopCards = reader.ReadBool();
        ReplaceShopRelics = reader.ReadBool();
        ReplaceShopPotions = reader.ReadBool();
    }
}
