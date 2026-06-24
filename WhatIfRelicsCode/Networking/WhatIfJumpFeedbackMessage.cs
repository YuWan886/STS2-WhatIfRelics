using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace WhatIfRelics.WhatIfRelicsCode.Networking;

internal struct WhatIfJumpFeedbackMessage : INetMessage, IPacketSerializable
{
    public ulong PlayerNetId;
    public int HoldDurationMsec;

    public bool ShouldBroadcast => true;

    public bool ShouldBuffer => false;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Debug;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(PlayerNetId);
        writer.WriteInt(HoldDurationMsec);
    }

    public void Deserialize(PacketReader reader)
    {
        PlayerNetId = reader.ReadULong();
        HoldDurationMsec = reader.ReadInt();
    }
}
