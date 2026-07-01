using System.Text.Json;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfAllEnemiesHaveNemesis")]
public class WhatIfAllEnemiesHaveNemesis : WhatIfRelicModel
{
    private HashSet<string> _affectedEnemyKeys = [];

    [SavedProperty]
    public string AffectedEnemyState
    {
        get => SerializeAffectedEnemyState(_affectedEnemyKeys);
        set
        {
            AssertMutable();
            _affectedEnemyKeys = DeserializeAffectedEnemyState(value);
        }
    }

    public WhatIfAllEnemiesHaveNemesis() : base(true)
    {
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        await ApplyNemesisIfNeeded(creature);
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom combatRoom)
        {
            return;
        }

        foreach (Creature creature in combatRoom.CombatState.Creatures)
        {
            await ApplyNemesisIfNeeded(creature);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _affectedEnemyKeys.Clear();
        return Task.CompletedTask;
    }

    private async Task ApplyNemesisIfNeeded(Creature creature)
    {
        if (!ShouldApplyNemesis(creature))
        {
            return;
        }

        string enemyKey = BuildCreatureKey(creature);
        if (!_affectedEnemyKeys.Add(enemyKey))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<NemesisPower>(new ThrowingPlayerChoiceContext(), creature, 1m, creature, null);
    }

    private bool ShouldApplyNemesis(Creature creature)
    {
        return Owner?.Creature != null
            && creature.Side != CombatSide.Player
            && creature.CombatState != null
            && !creature.IsDead;
    }

    private static string BuildCreatureKey(Creature creature)
    {
        string modelEntry = creature.Monster?.Id.Entry ?? creature.ModelId.Entry;
        string slotName = creature.SlotName ?? creature.CombatId?.ToString() ?? "unknown";
        return $"{modelEntry}|{slotName}";
    }

    private static string SerializeAffectedEnemyState(HashSet<string> affectedEnemyKeys)
    {
        if (affectedEnemyKeys.Count == 0)
        {
            return string.Empty;
        }

        return JsonSerializer.Serialize(affectedEnemyKeys.OrderBy(static key => key));
    }

    private static HashSet<string> DeserializeAffectedEnemyState(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<HashSet<string>>(serialized) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
