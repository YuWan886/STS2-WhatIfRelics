using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Patches;

[HarmonyPatch(typeof(RunManager), "CreateRoom", typeof(RoomType), typeof(MapPointType), typeof(AbstractModel))]
internal static class WhatIfRandomEncountersPatch
{
    [HarmonyPostfix]
    private static void Postfix(RoomType roomType, MapPointType mapPointType, AbstractModel? model, ref AbstractRoom __result)
    {
        if (model != null || !roomType.IsCombatRoom() || __result is not CombatRoom)
        {
            return;
        }

        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (!WhatIfRandomEncounters.HasRandomEncounters(runState))
        {
            return;
        }

        EncounterModel? encounter = PickRandomEncounter(runState, roomType, mapPointType);
        if (encounter == null)
        {
            return;
        }

        Entry.Logger.Info($"[WhatIfRandomEncounters] {roomType} -> {encounter.Id.Entry} ({encounter.RoomType})");
        __result = new WhatIfRandomEncounterCombatRoom(encounter.ToMutable(), runState, roomType);
    }

    private static EncounterModel? PickRandomEncounter(RunState? runState, RoomType roomType, MapPointType mapPointType)
    {
        if (runState == null)
        {
            return null;
        }

        List<EncounterModel> candidates = ModelDb.AllEncounters
            .Where(static encounter =>
                encounter is not DeprecatedEncounter
                && !encounter.IsDebugEncounter
                && encounter.ShouldGiveRewards
                && !encounter.GetType().Name.Contains("EventEncounter", StringComparison.Ordinal)
                && encounter.RoomType.IsCombatRoom())
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        uint mixin = BuildLocationMixin(runState, roomType, mapPointType);
        Rng rng = new(runState.Rng.Seed + mixin, "what_if_random_encounters");
        return rng.NextItem(candidates);
    }

    private static uint BuildLocationMixin(RunState runState, RoomType roomType, MapPointType mapPointType)
    {
        int actIndex = runState.CurrentActIndex;
        int col = runState.CurrentMapCoord?.col ?? -1;
        int row = runState.CurrentMapCoord?.row ?? -1;
        string key = $"{actIndex}|{col}|{row}|{(int)roomType}|{(int)mapPointType}|WHAT_IF_RANDOM_ENCOUNTERS";
        return unchecked((uint)StringHelper.GetDeterministicHashCode(key));
    }

    internal sealed class WhatIfRandomEncounterCombatRoom : CombatRoom
    {
        private readonly RoomType _originalRoomType;

        public override RoomType RoomType => _originalRoomType;

        public WhatIfRandomEncounterCombatRoom(EncounterModel encounter, IRunState? runState, RoomType originalRoomType)
            : base(encounter, runState)
        {
            _originalRoomType = originalRoomType;
        }
    }
}

[HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.FromSerializable), typeof(SerializableRoom), typeof(IRunState))]
internal static class WhatIfRandomEncountersRestorePatch
{
    [HarmonyPostfix]
    private static void CombatRoom_FromSerializable_Postfix(SerializableRoom serializableRoom, IRunState? runState, ref CombatRoom __result)
    {
        if (!serializableRoom.RoomType.IsCombatRoom() || serializableRoom.EncounterId == null)
        {
            return;
        }

        if (serializableRoom.RoomType == __result.RoomType)
        {
            return;
        }

        __result = RecreateRandomEncounterCombatRoom(serializableRoom, runState);
    }

    private static CombatRoom RecreateRandomEncounterCombatRoom(SerializableRoom serializableRoom, IRunState? runState)
    {
        if (serializableRoom.ExtraRewards.Count > 0 && runState == null)
        {
            throw new InvalidOperationException("Cannot load extra rewards without a run state.");
        }

        EncounterModel encounterModel = SaveUtil.EncounterOrDeprecated(serializableRoom.EncounterId!).ToMutable();
        encounterModel.LoadCustomState(serializableRoom.EncounterState);
        var combatRoom = new WhatIfRandomEncountersPatch.WhatIfRandomEncounterCombatRoom(encounterModel, runState, serializableRoom.RoomType)
        {
            ShouldResumeParentEventAfterCombat = serializableRoom.ShouldResumeParentEvent,
            ParentEventId = serializableRoom.ParentEventId
        };

        Traverse.Create(combatRoom).Property(nameof(CombatRoom.GoldProportion)).SetValue(serializableRoom.GoldProportion);

        foreach ((ulong netId, List<SerializableReward> rewards) in serializableRoom.ExtraRewards)
        {
            Player player = runState!.GetPlayer(netId)
                ?? throw new InvalidOperationException($"Cannot restore extra rewards for missing player {netId}.");
            foreach (SerializableReward reward in rewards)
            {
                combatRoom.AddExtraReward(player, Reward.FromSerializable(reward, player));
            }
        }

        if (serializableRoom.IsPreFinished)
        {
            combatRoom.MarkPreFinished();
        }

        return combatRoom;
    }
}
