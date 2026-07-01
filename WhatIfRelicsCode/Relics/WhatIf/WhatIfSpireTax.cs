using STS2RitsuLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfSpireTax")]
public class WhatIfSpireTax : WhatIfRelicModel
{
    private static readonly AttachedState<WhatIfSpireTax, int[]> LastChargedLocation =
        new(() => [-1, -1, -1, -1]);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("MonsterTax", MonsterTax),
        new IntVar("EliteTax", EliteTax),
        new IntVar("BossTax", BossTax),
        new IntVar("TreasureTax", TreasureTax),
        new IntVar("ShopTax", ShopTax),
        new IntVar("EventTax", EventTax),
        new IntVar("AncientTax", AncientTax),
        new IntVar("RestSiteTax", RestSiteTax)
    ];

    public const int MonsterTax = 10;
    public const int EliteTax = 20;
    public const int BossTax = 40;
    public const int TreasureTax = 15;
    public const int ShopTax = 25;
    public const int EventTax = 12;
    public const int AncientTax = 30;
    public const int RestSiteTax = 18;

    public WhatIfSpireTax() : base(true)
    {
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner?.RunState == null || Owner.RunState.CurrentRoomCount != 1)
        {
            return Task.CompletedTask;
        }

        MapLocation location = Owner.RunState.MapLocation;
        if (!location.coord.HasValue)
        {
            return Task.CompletedTask;
        }

        int roomSequence = Owner.RunState.CurrentMapPointHistoryEntry?.Rooms.Count ?? 0;
        if (WasAlreadyChargedForLocation(location, roomSequence))
        {
            return Task.CompletedTask;
        }

        int tax = GetTax(room);
        if (tax <= 0)
        {
            return Task.CompletedTask;
        }

        ChargeOwner(tax);
        RememberChargedLocation(location, roomSequence);
        return Task.CompletedTask;
    }

    private bool WasAlreadyChargedForLocation(MapLocation location, int roomSequence)
    {
        int[] snapshot = LastChargedLocation[this];
        if (snapshot.Length < 4)
        {
            return false;
        }

        return snapshot[0] == location.actIndex
            && snapshot[1] == location.coord?.col
            && snapshot[2] == location.coord?.row
            && snapshot[3] == roomSequence;
    }

    private void RememberChargedLocation(MapLocation location, int roomSequence)
    {
        LastChargedLocation[this] =
        [
            location.actIndex,
            location.coord?.col ?? -1,
            location.coord?.row ?? -1,
            roomSequence
        ];
    }

    private void ChargeOwner(int tax)
    {
        if (Owner == null)
        {
            return;
        }

        if (LocalContext.IsMe(Owner))
        {
            SfxCmd.Play("event:/sfx/ui/gold/gold_1");
        }

        Owner.Gold -= tax;

        PlayerMapPointHistoryEntry? historyEntry = Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(Owner.NetId);
        if (historyEntry != null)
        {
            historyEntry.GoldSpent += tax;
            historyEntry.CurrentGold = Owner.Gold;
        }
    }

    private static int GetTax(AbstractRoom room)
    {
        return room switch
        {
            EventRoom { CanonicalEvent: AncientEventModel } => AncientTax,
            { RoomType: RoomType.Monster } => MonsterTax,
            { RoomType: RoomType.Elite } => EliteTax,
            { RoomType: RoomType.Boss } => BossTax,
            { RoomType: RoomType.Treasure } => TreasureTax,
            { RoomType: RoomType.Shop } => ShopTax,
            { RoomType: RoomType.Event } => EventTax,
            { RoomType: RoomType.RestSite } => RestSiteTax,
            _ => 0
        };
    }
}
