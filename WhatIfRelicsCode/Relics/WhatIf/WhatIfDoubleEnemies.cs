using System;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[WhatIfRegisterRelic(typeof(WhatIfRelicPool), StableEntryStem = "WhatIfDoubleEnemies")]
public sealed class WhatIfDoubleEnemies : WhatIfRelicModel
{
    private const float VerticalGap = 20f;
    private const float HorizontalGap = 40f;
    private const float ScreenPadding = 24f;
    private const float DiagonalLift = 120f;

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
            if (!CanDuplicateEnemy(original.Monster))
            {
                continue;
            }

            MonsterModel duplicateMonster = original.Monster!.CanonicalInstance.ToMutable();
            Creature copy = await AddInitialCopyAsync(
                combatRoom.CombatState,
                duplicateMonster,
                original.SlotName);

            CopyInitialCombatState(original, copy);
            pairs.Add((original, copy));
        }

        if (pairs.Count == 0)
        {
            return;
        }

        RepositionCopies(pairs);
        Flash();
    }

    private static bool CanDuplicateEnemy(MonsterModel? monster)
    {
        // These creatures belong to encounter-specific composite rigs rather than ordinary standalone enemies.
        // Duplicating them spawns extra logic owners without the matching scene contract / visual wiring.
        return monster is not Crusher
            and not Rocket
            and not DecimillipedeSegment;
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

    private static Task<Creature> AddInitialCopyAsync(
        CombatState combatState,
        MonsterModel monster,
        string? slotName)
    {
        Creature creature = combatState.CreateCreature(monster, CombatSide.Enemy, slotName);
        combatState.AddCreature(creature);
        CombatManager.Instance.AddCreature(creature);
        NCombatRoom.Instance?.AddCreature(creature);

        if (combatState.RunState.CurrentMapPointHistoryEntry?.Rooms.LastOrDefault() is { } roomHistory
            && creature.Monster != null
            && !roomHistory.MonsterIds.Contains(creature.Monster.Id))
        {
            roomHistory.MonsterIds.Add(creature.Monster.Id);
        }

        return Task.FromResult(creature);
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

            PositionCopyNearOriginal(originalNode, copyNode);
        }
    }

    private static void PositionCopyNearOriginal(NCreature originalNode, NCreature copyNode)
    {
        ICombatState? combatState = originalNode.Entity.CombatState;
        if (combatState == null)
        {
            return;
        }

        Rect2 originalRect = originalNode.Hitbox.GetGlobalRect();
        Rect2 copyRect = copyNode.Hitbox.GetGlobalRect();
        Vector2 viewportSize = copyNode.GetViewport().GetVisibleRect().Size;

        if (TryGetQueenCopyPlacement(
                combatState,
                originalNode.Entity,
                copyNode.GlobalPosition,
                copyRect,
                viewportSize,
                out Vector2 queenPlacement))
        {
            MoveCopyToTopLeft(copyNode, copyRect, queenPlacement);
            return;
        }

        if (TryGetRubyRaidersCopyPlacement(
                combatState,
                originalNode.Entity,
                originalRect,
                copyRect.Size,
                viewportSize,
                out Vector2 rubyRaidersPlacement))
        {
            MoveCopyToTopLeft(copyNode, copyRect, rubyRaidersPlacement);
            return;
        }

        List<Rect2> occupiedRects = combatState.Enemies
            .Where(enemy => enemy != copyNode.Entity)
            .Select(enemy => NCombatRoom.Instance?.GetCreatureNode(enemy))
            .Where(static node => node != null)
            .Select(static node => node!.Hitbox.GetGlobalRect())
            .ToList();

        MoveCopyToTopLeft(
            copyNode,
            copyRect,
            FindBestPlacement(originalRect, copyRect.Size, viewportSize, occupiedRects));
    }

    private static bool TryGetQueenCopyPlacement(
        ICombatState combatState,
        Creature originalCreature,
        Vector2 copyNodePosition,
        Rect2 copyRect,
        Vector2 viewportSize,
        out Vector2 placement)
    {
        placement = default;
        if (originalCreature.Monster is not Queen)
        {
            return false;
        }

        Creature? originalAmalgam = combatState.Enemies
            .Where(enemy => enemy.Monster is TorchHeadAmalgam)
            .OrderBy(enemy => enemy.CombatId ?? uint.MaxValue)
            .FirstOrDefault();
        if (originalAmalgam == null || NCombatRoom.Instance?.GetCreatureNode(originalAmalgam) is not { } amalgamNode)
        {
            return false;
        }

        Rect2 amalgamRect = amalgamNode.Hitbox.GetGlobalRect();
        Vector2 hitboxOffset = copyRect.Position - copyNodePosition;
        float desiredNodeX = amalgamRect.Position.X - HorizontalGap - copyRect.Size.X - hitboxOffset.X;
        float desiredNodeY = amalgamNode.GlobalPosition.Y;
        placement = ClampToViewport(
            new Vector2(desiredNodeX, desiredNodeY) + hitboxOffset,
            copyRect.Size,
            viewportSize);
        return true;
    }

    private static bool TryGetRubyRaidersCopyPlacement(
        ICombatState combatState,
        Creature originalCreature,
        Rect2 originalRect,
        Vector2 copySize,
        Vector2 viewportSize,
        out Vector2 placement)
    {
        placement = default;
        if (combatState.Encounter is not RubyRaidersNormal || originalCreature.Monster is null)
        {
            return false;
        }

        float centeredAboveX = originalRect.Position.X + (originalRect.Size.X - copySize.X) * 0.5f;
        float aboveY = originalRect.Position.Y - VerticalGap - copySize.Y;
        placement = ClampToViewport(new Vector2(centeredAboveX, aboveY), copySize, viewportSize);
        return true;
    }

    private static Vector2 FindBestPlacement(
        Rect2 originalRect,
        Vector2 copySize,
        Vector2 viewportSize,
        IReadOnlyList<Rect2> occupiedRects)
    {
        float leftSpace = originalRect.Position.X - ScreenPadding;
        float rightSpace = viewportSize.X - ScreenPadding - (originalRect.Position.X + originalRect.Size.X);
        bool preferRight = rightSpace >= leftSpace;

        List<Vector2> candidates = BuildCandidates(originalRect, copySize, preferRight);
        Vector2 best = ClampToViewport(candidates[0], copySize, viewportSize);
        float bestOverlap = float.MaxValue;

        foreach (Vector2 candidate in candidates)
        {
            Vector2 clamped = ClampToViewport(candidate, copySize, viewportSize);
            float overlap = CalculateTotalOverlap(new Rect2(clamped, copySize), occupiedRects);
            if (overlap < bestOverlap)
            {
                best = clamped;
                bestOverlap = overlap;
                if (overlap <= 0f)
                {
                    break;
                }
            }
        }

        return best;
    }

    private static List<Vector2> BuildCandidates(Rect2 originalRect, Vector2 copySize, bool preferRight)
    {
        float centeredAboveX = originalRect.Position.X + (originalRect.Size.X - copySize.X) * 0.5f;
        float aboveY = originalRect.Position.Y - VerticalGap - copySize.Y;
        float sideY = originalRect.Position.Y + (originalRect.Size.Y - copySize.Y) * 0.5f;
        float belowY = originalRect.Position.Y + originalRect.Size.Y + VerticalGap;
        float rightX = originalRect.Position.X + originalRect.Size.X + HorizontalGap;
        float leftX = originalRect.Position.X - HorizontalGap - copySize.X;
        float diagonalY = originalRect.Position.Y - DiagonalLift;

        Vector2 primaryDiagonal = new(preferRight ? rightX : leftX, diagonalY);
        Vector2 secondaryDiagonal = new(preferRight ? leftX : rightX, diagonalY);
        Vector2 primarySide = new(preferRight ? rightX : leftX, sideY);
        Vector2 secondarySide = new(preferRight ? leftX : rightX, sideY);
        Vector2 primaryBelow = new(preferRight ? rightX : leftX, belowY);
        Vector2 secondaryBelow = new(preferRight ? leftX : rightX, belowY);

        return
        [
            new Vector2(centeredAboveX, aboveY),
            primaryDiagonal,
            secondaryDiagonal,
            primarySide,
            secondarySide,
            primaryBelow,
            secondaryBelow
        ];
    }

    private static Vector2 ClampToViewport(Vector2 topLeft, Vector2 size, Vector2 viewportSize)
    {
        return new Vector2(
            ClampX(topLeft.X, size.X, viewportSize.X),
            ClampY(topLeft.Y, size.Y, viewportSize.Y));
    }

    private static float CalculateTotalOverlap(Rect2 candidateRect, IReadOnlyList<Rect2> occupiedRects)
    {
        float total = 0f;
        foreach (Rect2 occupiedRect in occupiedRects)
        {
            total += CalculateOverlapArea(candidateRect, occupiedRect);
        }

        return total;
    }

    private static float CalculateOverlapArea(Rect2 a, Rect2 b)
    {
        float overlapX = MathF.Max(0f, MathF.Min(a.End.X, b.End.X) - MathF.Max(a.Position.X, b.Position.X));
        float overlapY = MathF.Max(0f, MathF.Min(a.End.Y, b.End.Y) - MathF.Max(a.Position.Y, b.Position.Y));
        return overlapX * overlapY;
    }

    private static void MoveCopyToTopLeft(NCreature copyNode, Rect2 copyRect, Vector2 targetTopLeft)
    {
        copyNode.GlobalPosition += targetTopLeft - copyRect.Position;
    }

    private static float ClampX(float left, float width, float viewportWidth)
    {
        return Mathf.Clamp(left, ScreenPadding, viewportWidth - ScreenPadding - width);
    }

    private static float ClampY(float top, float height, float viewportHeight)
    {
        return Mathf.Clamp(top, ScreenPadding, viewportHeight - ScreenPadding - height);
    }
}
