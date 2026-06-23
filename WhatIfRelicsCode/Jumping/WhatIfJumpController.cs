using System.Diagnostics.CodeAnalysis;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Utils;
using WhatIfRelics.WhatIfRelicsCode.Relics;

namespace WhatIfRelics.WhatIfRelicsCode.Jumping;

internal static partial class WhatIfJumpController
{
    private const float BaseJumpHeight = 84f;
    private const float MaxJumpHeight = 288f;
    private const double BaseJumpDuration = 0.28;
    private const double MaxJumpDuration = 0.72;
    private const ulong MediumChargeMsec = 1500;
    private const ulong HighChargeMsec = 3000;
    private const ulong MaxChargeMsec = HighChargeMsec;
    private const ulong JumpWindowMsec = 3000;
    private const int RequiredJumpCount = 30;
    private static readonly AttachedState<CombatRoom, bool> SkipCurrentCombatWithoutRewards = new(() => false);
    private static JumpRuntimeNode? _runtimeNode;

    public static void Register()
    {
        RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
        {
            EnsureRuntimeNode(evt.Game);
        });
        RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => _runtimeNode?.ResetForRoomChange());
    }

    private static void EnsureRuntimeNode(NGame game)
    {
        if (_runtimeNode != null && GodotObject.IsInstanceValid(_runtimeNode))
        {
            return;
        }

        _runtimeNode = new JumpRuntimeNode
        {
            Name = "WhatIfJumpRuntime"
        };
        game.AddChild(_runtimeNode);
    }

    private static bool TryGetLocalJumpRelic(IRunState? runState, out Player? owner, out WhatIfJump? relic)
    {
        owner = null;
        relic = null;
        if (runState == null || !WhatIfJump.HasJumpRelic(runState))
        {
            return false;
        }

        try
        {
            owner = LocalContext.GetMe(runState);
        }
        catch (Exception)
        {
            return false;
        }

        if (owner == null)
        {
            return false;
        }

        relic = owner.GetRelic<WhatIfJump>();
        return relic != null;
    }

    private static bool TryGetActiveCombatContext(
        out IRunState? runState,
        out Player? owner,
        out WhatIfJump? relic,
        out CombatRoom? room)
    {
        runState = RunManager.Instance?.DebugOnlyGetState();
        room = runState?.CurrentRoom as CombatRoom;

        if (room == null ||
            !CombatManager.Instance.IsInProgress ||
            CombatManager.Instance.IsEnding ||
            CombatManager.Instance.PlayerActionsDisabled ||
            !TryGetLocalJumpRelic(runState, out owner, out relic))
        {
            owner = null;
            relic = null;
            room = null;
            return false;
        }

        return true;
    }

    private static void PlayJumpFeedback(Player owner, double holdDurationMsec)
    {
        if (NCombatRoom.Instance?.GetCreatureNode(owner.Creature) is { } creatureNode)
        {
            PlayNodeJump(creatureNode.Visuals, holdDurationMsec);
        }
    }

    private static void PlayNodeJump(Node node, double holdDurationMsec)
    {
        if (node is not Node2D and not Control)
        {
            return;
        }

        float chargeRatio = Mathf.Clamp((float)(holdDurationMsec / MaxChargeMsec), 0f, 1f);
        float jumpHeight = Mathf.Lerp(BaseJumpHeight, MaxJumpHeight, chargeRatio);
        double totalDuration = Mathf.Lerp((float)BaseJumpDuration, (float)MaxJumpDuration, chargeRatio);
        double riseDuration = totalDuration * 0.42;
        double fallDuration = totalDuration - riseDuration;
        float rotationAmount = holdDurationMsec switch
        {
            >= HighChargeMsec => -Mathf.Tau,
            >= MediumChargeMsec => Mathf.Tau,
            _ => 0f
        };

        Tween tween = node.CreateTween();
        switch (node)
        {
            case Node2D node2D:
            {
                Vector2 start = node2D.Position;
                float startRotation = node2D.Rotation;
                tween.TweenProperty(node2D, "position", start + Vector2.Up * jumpHeight, riseDuration)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Sine);
                tween.TweenProperty(node2D, "position", start, fallDuration)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Sine);
                if (!Mathf.IsZeroApprox(rotationAmount))
                {
                    tween.Parallel()
                        .TweenProperty(node2D, "rotation", startRotation + rotationAmount, totalDuration)
                        .SetEase(Tween.EaseType.InOut)
                        .SetTrans(Tween.TransitionType.Sine);
                    tween.TweenCallback(Callable.From(() => node2D.Rotation = startRotation));
                }
                break;
            }
            case Control control:
            {
                Vector2 start = control.Position;
                float startRotation = control.Rotation;
                tween.TweenProperty(control, "position", start + Vector2.Up * jumpHeight, riseDuration)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Sine);
                tween.TweenProperty(control, "position", start, fallDuration)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Sine);
                if (!Mathf.IsZeroApprox(rotationAmount))
                {
                    tween.Parallel()
                        .TweenProperty(control, "rotation", startRotation + rotationAmount, totalDuration)
                        .SetEase(Tween.EaseType.InOut)
                        .SetTrans(Tween.TransitionType.Sine);
                    tween.TweenCallback(Callable.From(() => control.Rotation = startRotation));
                }
                break;
            }
        }
    }

    private static async Task SkipCurrentCombatAsync(CombatRoom room)
    {
        if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding)
        {
            return;
        }

        SkipCurrentCombatWithoutRewards[room] = true;
        List<Creature> enemies = CombatManager.Instance.DebugOnlyGetState()?.Enemies.ToList() ?? [];

        foreach (Creature enemy in enemies)
        {
            enemy.RemoveAllPowersInternalExcept();
            await CreatureCmd.Kill(enemy);
        }

        await CombatManager.Instance.CheckWinCondition();
    }

    private static bool TryGetSpaceKeyEvent(InputEvent? @event, [NotNullWhen(true)] out InputEventKey? keyEvent)
    {
        keyEvent = @event as InputEventKey;
        return keyEvent != null && !keyEvent.IsEcho() && keyEvent.Keycode == Key.Space;
    }

    private static void TryHandleSpaceInput(InputEvent @event)
    {
        if (!TryGetSpaceKeyEvent(@event, out InputEventKey? keyEvent))
        {
            return;
        }

        if (!TryGetActiveCombatContext(out _, out Player? owner, out WhatIfJump? relic, out CombatRoom? room))
        {
            _runtimeNode?.CancelCharge();
            return;
        }
        if (owner == null || relic == null || room == null)
        {
            return;
        }

        EnsureRuntimeNode(NGame.Instance ?? throw new InvalidOperationException("NGame.Instance was null while handling jump input."));
        if (keyEvent.Pressed)
        {
            _runtimeNode?.BeginCharge();
            return;
        }

        _runtimeNode?.ReleaseCharge(owner, relic, room, Time.GetTicksMsec());
    }

    private sealed partial class JumpRuntimeNode : Node
    {
        private readonly Queue<ulong> _jumpTimes = [];
        private bool _isCharging;
        private ulong _chargeStartedAtMsec;

        public override void _Process(double delta)
        {
            if (!TryGetActiveCombatContext(out _, out _, out WhatIfJump? relic, out _))
            {
                ClearJumpState(null);
                return;
            }
            if (relic == null)
            {
                ClearJumpState(null);
                return;
            }

            ulong now = Time.GetTicksMsec();
            PruneExpired(now);
            relic.SetCurrentJumpCount(_jumpTimes.Count);
        }

        public override void _Input(InputEvent @event)
        {
        }

        public override void _EnterTree()
        {
            SetProcess(true);
        }

        public void ResetForRoomChange()
        {
            CancelCharge();
            ClearJumpState(null);
        }

        public void BeginCharge()
        {
            if (_isCharging)
            {
                return;
            }

            _isCharging = true;
            _chargeStartedAtMsec = Time.GetTicksMsec();
        }

        public void CancelCharge()
        {
            _isCharging = false;
            _chargeStartedAtMsec = 0;
        }

        public void ReleaseCharge(Player owner, WhatIfJump relic, CombatRoom room, ulong now)
        {
            if (!_isCharging)
            {
                return;
            }

            double holdDurationMsec = Math.Min(now - _chargeStartedAtMsec, MaxChargeMsec);
            CancelCharge();
            PruneExpired(now);

            if (_jumpTimes.Count > 0 && now == _jumpTimes.ToArray()[^1])
            {
                return;
            }

            _jumpTimes.Enqueue(now);
            relic.SetCurrentJumpCount(_jumpTimes.Count);
            PlayJumpFeedback(owner, holdDurationMsec);

            if (_jumpTimes.Count < RequiredJumpCount)
            {
                return;
            }

            ClearJumpState(relic);
            TaskHelper.RunSafely(SkipCurrentCombatAsync(room));
        }

        private void PruneExpired(ulong now)
        {
            while (_jumpTimes.Count > 0 && now - _jumpTimes.Peek() > JumpWindowMsec)
            {
                _jumpTimes.Dequeue();
            }
        }

        private void ClearJumpState(WhatIfJump? relic)
        {
            _jumpTimes.Clear();
            relic?.SetCurrentJumpCount(0);
        }
    }

    [HarmonyPatch(typeof(NGame), nameof(NGame._Input))]
    private static class NGame_Input_Patch
    {
        private static void Prefix(InputEvent inputEvent)
        {
            TryHandleSpaceInput(inputEvent);
        }
    }

    [HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.OfferRoomEndRewards))]
    private static class CombatRoom_OfferRoomEndRewards_Patch
    {
        private static bool Prefix(CombatRoom __instance, ref Task __result)
        {
            if (!SkipCurrentCombatWithoutRewards[__instance])
            {
                return true;
            }

            SkipCurrentCombatWithoutRewards[__instance] = false;
            __result = NCombatRoom.Instance?.Ui?.ProceedWithoutRewards() ?? Task.CompletedTask;
            return false;
        }
    }
}
