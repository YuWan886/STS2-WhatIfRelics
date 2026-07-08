using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfDoubleEnemies")]
public sealed class WhatIfDoubleEnemies : WhatIfRelicModel
{
    private const float VerticalGap = 20f;

    public WhatIfDoubleEnemies() : base(true)
    {
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom combatRoom)
        {
            return;
        }

        List<Creature> originals = combatRoom.CombatState.Enemies
            .Where(static enemy => enemy.IsAlive && enemy.Monster != null)
            .ToList();
        if (originals.Count == 0)
        {
            return;
        }

        List<(Creature Original, Creature Copy)> pairs = [];
        foreach (Creature original in originals)
        {
            MonsterModel duplicateMonster = original.Monster!.CanonicalInstance.ToMutable();
            Creature copy = await AddInitialCopyAsync(
                combatRoom.CombatState,
                duplicateMonster,
                original.SlotName);

            CopyInitialCombatState(original, copy);
            pairs.Add((original, copy));
        }

        RepositionCopies(pairs);
        Flash();
    }

    private static void CopyInitialCombatState(Creature original, Creature copy)
    {
        copy.SetMaxHpInternal(original.MaxHp);
        copy.SetCurrentHpInternal(original.CurrentHp);

        if (TryResolveMatchingMove(original, copy, out MoveState move))
        {
            copy.Monster!.SetMoveImmediate(move, forceTransition: true);
        }
    }

    private static bool TryResolveMatchingMove(Creature original, Creature copy, out MoveState move)
    {
        move = null!;

        string? moveId = original.Monster?.NextMove?.Id;
        if (string.IsNullOrWhiteSpace(moveId))
        {
            return false;
        }

        if (copy.Monster?.MoveStateMachine?.States.GetValueOrDefault(moveId) is not MoveState resolved)
        {
            return false;
        }

        move = resolved;
        return true;
    }

    private static async Task<Creature> AddInitialCopyAsync(
        CombatState combatState,
        MonsterModel monster,
        string? slotName)
    {
        Creature creature = combatState.CreateCreature(monster, CombatSide.Enemy, slotName);
        combatState.AddCreature(creature);
        CombatManager.Instance.AddCreature(creature);
        NCombatRoom.Instance?.AddCreature(creature);
        await CombatManager.Instance.AfterCreatureAdded(creature);

        if (combatState.RunState.CurrentMapPointHistoryEntry?.Rooms.LastOrDefault() is { } roomHistory
            && creature.Monster != null
            && !roomHistory.MonsterIds.Contains(creature.Monster.Id))
        {
            roomHistory.MonsterIds.Add(creature.Monster.Id);
        }

        await Hook.AfterCreatureAddedToCombat(combatState, creature);
        return creature;
    }

    private static void RepositionCopies(IEnumerable<(Creature Original, Creature Copy)> pairs)
    {
        NCombatRoom? combatRoom = NCombatRoom.Instance;
        if (combatRoom == null)
        {
            return;
        }

        foreach ((Creature original, Creature copy) in pairs)
        {
            if (combatRoom.GetCreatureNode(original) is not { } originalNode
                || combatRoom.GetCreatureNode(copy) is not { } copyNode)
            {
                continue;
            }

            PositionCopyAboveOriginal(originalNode, copyNode);
        }
    }

    private static void PositionCopyAboveOriginal(NCreature originalNode, NCreature copyNode)
    {
        Vector2 desiredBottom = originalNode.GetTopOfHitbox() + Vector2.Up * VerticalGap;
        Vector2 translation = desiredBottom - copyNode.GetBottomOfHitbox();
        copyNode.GlobalPosition += translation;
    }
}
