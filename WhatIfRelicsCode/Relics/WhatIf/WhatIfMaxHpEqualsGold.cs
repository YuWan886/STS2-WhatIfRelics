using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfMaxHpEqualsGold")]
public class WhatIfMaxHpEqualsGold : WhatIfRelicModel
{
    public WhatIfMaxHpEqualsGold() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await SyncPlayerMaxHpAsync(Owner);
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        return SyncPlayerMaxHpAsync(Owner);
    }

    internal static Task SyncPlayerMaxHpAsync(Player? player)
    {
        if (player == null || player.GetRelic<WhatIfMaxHpEqualsGold>() == null)
        {
            return Task.CompletedTask;
        }

        return CreatureCmd.SetMaxHp(player.Creature, Math.Max(0, player.Gold));
    }
}
