using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Networking.ManagedActions;
using STS2RitsuLib.Utils;
using WhatIfRelics.WhatIfRelicsCode.Networking;
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
    private static readonly AttachedState<CombatRoom, bool> SkipCurrentCombatWithoutRewards = new(() => false);
    private static readonly RitsuLibManagedNetActionDescriptor<JumpSlashCriticalActionPayload> JumpSlashCriticalDescriptor =
        new(
            Entry.ModId,
            "WhatIfJumpSlashGrantCritical",
            static _ => [],
            static _ => default,
            static context => ExecuteJumpSlashCriticalAsync(context),
            GameActionType.CombatPlayPhaseOnly);
    private static readonly ConditionalWeakTable<Node, JumpAnimationState> JumpAnimationStates = [];
    private static JumpRuntimeNode? _runtimeNode;
    private static INetGameService? _registeredNetService;

    public static void Register()
    {
        RitsuLibManagedNetActions.Register(JumpSlashCriticalDescriptor);
        EnsureNetRegistered();
        RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
        {
            EnsureRuntimeNode(evt.Game);
        });
        RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(_ =>
        {
            EnsureNetRegistered();
        }, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(_ =>
        {
            EnsureNetRegistered();
        }, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => _runtimeNode?.ResetForRoomChange());
    }

    private static void EnsureNetRegistered()
    {
        INetGameService? netService = RunManager.Instance?.NetService;
        if (_registeredNetService == netService)
        {
            return;
        }

        if (_registeredNetService != null)
        {
            _registeredNetService.UnregisterMessageHandler<WhatIfJumpFeedbackMessage>(HandleJumpFeedbackMessage);
        }

        if (netService != null)
        {
            netService.RegisterMessageHandler<WhatIfJumpFeedbackMessage>(HandleJumpFeedbackMessage);
        }

        _registeredNetService = netService;
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

    private static bool TryGetLocalJumpContext(
        IRunState? runState,
        out Player? owner,
        out WhatIfJump? jumpRelic,
        out WhatIfJumpSlash? jumpSlashRelic)
    {
        owner = null;
        jumpRelic = null;
        jumpSlashRelic = null;
        if (runState == null || (!WhatIfJump.HasJumpRelic(runState) && !WhatIfJumpSlash.HasJumpSlashRelic(runState)))
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

        jumpRelic = owner.GetRelic<WhatIfJump>();
        jumpSlashRelic = owner.GetRelic<WhatIfJumpSlash>();
        return jumpRelic != null || jumpSlashRelic != null;
    }

    private static bool TryGetActiveCombatContext(
        out IRunState? runState,
        out Player? owner,
        out WhatIfJump? jumpRelic,
        out WhatIfJumpSlash? jumpSlashRelic,
        out CombatRoom? room)
    {
        runState = RunManager.Instance?.DebugOnlyGetState();
        room = runState?.CurrentRoom as CombatRoom;

        if (room == null ||
            !CombatManager.Instance.IsInProgress ||
            CombatManager.Instance.IsEnding ||
            CombatManager.Instance.PlayerActionsDisabled ||
            !TryGetLocalJumpContext(runState, out owner, out jumpRelic, out jumpSlashRelic))
        {
            owner = null;
            jumpRelic = null;
            jumpSlashRelic = null;
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

    private static void BroadcastJumpFeedback(Player owner, double holdDurationMsec)
    {
        EnsureNetRegistered();

        if (_registeredNetService is not { IsConnected: true })
        {
            return;
        }

        _registeredNetService.SendMessage(new WhatIfJumpFeedbackMessage
        {
            PlayerNetId = owner.NetId,
            HoldDurationMsec = (int)Math.Round(holdDurationMsec)
        });
    }

    private static void HandleJumpFeedbackMessage(WhatIfJumpFeedbackMessage message, ulong senderId)
    {
        IRunState? runState = RunManager.Instance?.DebugOnlyGetState();
        Player? localPlayer = runState == null ? null : LocalContext.GetMe(runState);
        if (localPlayer?.NetId == message.PlayerNetId)
        {
            return;
        }

        CombatState? combatState = CombatManager.Instance?.DebugOnlyGetState();
        Player? player = combatState?.GetPlayer(message.PlayerNetId) ?? runState?.Players.FirstOrDefault(p => p.NetId == message.PlayerNetId);
        if (player?.Creature == null)
        {
            Entry.Logger.VeryDebug($"WhatIfJump ignored synced jump feedback from {senderId}: player {message.PlayerNetId} not found.");
            return;
        }

        PlayJumpFeedback(player, Math.Max(0, message.HoldDurationMsec));
    }

    private static void PlayNodeJump(Node node, double holdDurationMsec)
    {
        if (node is not Node2D and not Control)
        {
            return;
        }

        JumpAnimationState animationState = JumpAnimationStates.GetOrCreateValue(node);
        CaptureBaseTransformIfNeeded(node, animationState);
        Vector2 currentPosition = GetNodePosition(node);
        float currentRotation = GetNodeRotation(node);
        StopActiveJumpAnimation(animationState);

        float chargeRatio = Mathf.Clamp((float)(holdDurationMsec / MaxChargeMsec), 0f, 1f);
        float jumpHeight = Mathf.Lerp(BaseJumpHeight, MaxJumpHeight, chargeRatio);
        double totalDuration = Mathf.Lerp((float)BaseJumpDuration, (float)MaxJumpDuration, chargeRatio);
        double riseDuration = totalDuration * 0.42;
        double fallDuration = totalDuration - riseDuration;
        float currentLift = Mathf.Max(0f, animationState.BasePosition.Y - currentPosition.Y);
        float peakLift = currentLift + jumpHeight;
        Vector2 peakPosition = animationState.BasePosition + Vector2.Up * peakLift;
        float rotationAmount = holdDurationMsec switch
        {
            >= HighChargeMsec => -Mathf.Tau,
            >= MediumChargeMsec => Mathf.Tau,
            _ => 0f
        };

        Tween tween = node.CreateTween();
        animationState.ActiveTween = tween;
        switch (node)
        {
            case Node2D node2D:
            {
                tween.TweenProperty(node2D, "position", peakPosition, riseDuration)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Sine);
                tween.TweenProperty(node2D, "position", animationState.BasePosition, fallDuration)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Sine);
                if (!Mathf.IsZeroApprox(rotationAmount))
                {
                    tween.Parallel()
                        .TweenProperty(node2D, "rotation", currentRotation + rotationAmount, totalDuration)
                        .SetEase(Tween.EaseType.InOut)
                        .SetTrans(Tween.TransitionType.Sine);
                }
                break;
            }
            case Control control:
            {
                tween.TweenProperty(control, "position", peakPosition, riseDuration)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Sine);
                tween.TweenProperty(control, "position", animationState.BasePosition, fallDuration)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Sine);
                if (!Mathf.IsZeroApprox(rotationAmount))
                {
                    tween.Parallel()
                        .TweenProperty(control, "rotation", currentRotation + rotationAmount, totalDuration)
                        .SetEase(Tween.EaseType.InOut)
                        .SetTrans(Tween.TransitionType.Sine);
                }
                break;
            }
        }

        tween.TweenCallback(Callable.From(() => CompleteJumpAnimation(node, animationState)));
    }

    private static void StopActiveJumpAnimation(JumpAnimationState animationState)
    {
        if (animationState.ActiveTween != null && GodotObject.IsInstanceValid(animationState.ActiveTween))
        {
            animationState.ActiveTween.Kill();
        }

        animationState.ActiveTween = null;
    }

    private static void CaptureBaseTransformIfNeeded(Node node, JumpAnimationState animationState)
    {
        if (animationState.HasBaseTransform)
        {
            return;
        }

        switch (node)
        {
            case Node2D node2D:
                animationState.BasePosition = node2D.Position;
                animationState.BaseRotation = node2D.Rotation;
                break;
            case Control control:
                animationState.BasePosition = control.Position;
                animationState.BaseRotation = control.Rotation;
                break;
        }

        animationState.HasBaseTransform = true;
    }

    private static Vector2 GetNodePosition(Node node)
    {
        return node switch
        {
            Node2D node2D => node2D.Position,
            Control control => control.Position,
            _ => Vector2.Zero
        };
    }

    private static float GetNodeRotation(Node node)
    {
        return node switch
        {
            Node2D node2D => node2D.Rotation,
            Control control => control.Rotation,
            _ => 0f
        };
    }

    private static void RestoreBaseTransform(Node node, JumpAnimationState animationState)
    {
        switch (node)
        {
            case Node2D node2D:
                node2D.Position = animationState.BasePosition;
                node2D.Rotation = animationState.BaseRotation;
                break;
            case Control control:
                control.Position = animationState.BasePosition;
                control.Rotation = animationState.BaseRotation;
                break;
        }
    }

    private static void CompleteJumpAnimation(Node node, JumpAnimationState animationState)
    {
        animationState.ActiveTween = null;
        RestoreBaseTransform(node, animationState);
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

        if (!TryGetActiveCombatContext(out _, out Player? owner, out WhatIfJump? jumpRelic, out WhatIfJumpSlash? jumpSlashRelic, out CombatRoom? room))
        {
            _runtimeNode?.CancelCharge();
            return;
        }
        if (owner == null || room == null)
        {
            return;
        }

        EnsureRuntimeNode(NGame.Instance ?? throw new InvalidOperationException("NGame.Instance was null while handling jump input."));
        if (keyEvent.Pressed)
        {
            _runtimeNode?.BeginCharge();
            return;
        }

        _runtimeNode?.ReleaseCharge(owner, jumpRelic, jumpSlashRelic, room, Time.GetTicksMsec());
    }

    private sealed partial class JumpRuntimeNode : Node
    {
        private readonly Queue<ulong> _jumpTimes = [];
        private bool _isCharging;
        private ulong _chargeStartedAtMsec;
        private ulong _lastProcessedJumpAtMsec;

        public override void _Process(double delta)
        {
            if (!TryGetActiveCombatContext(out _, out _, out WhatIfJump? jumpRelic, out _, out _))
            {
                ClearJumpState(null);
                return;
            }
            if (jumpRelic == null)
            {
                ClearJumpState(null);
                return;
            }

            ulong now = Time.GetTicksMsec();
            PruneExpired(now);
            jumpRelic.SetCurrentJumpCount(_jumpTimes.Count);
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

        public void ReleaseCharge(
            Player owner,
            WhatIfJump? jumpRelic,
            WhatIfJumpSlash? jumpSlashRelic,
            CombatRoom room,
            ulong now)
        {
            if (!_isCharging)
            {
                return;
            }

            double holdDurationMsec = Math.Min(now - _chargeStartedAtMsec, MaxChargeMsec);
            CancelCharge();
            if (_lastProcessedJumpAtMsec == now)
            {
                return;
            }

            _lastProcessedJumpAtMsec = now;
            PlayJumpFeedback(owner, holdDurationMsec);
            BroadcastJumpFeedback(owner, holdDurationMsec);

            if (jumpSlashRelic != null)
            {
                TryQueueJumpSlashCritical(owner, jumpSlashRelic);
            }

            if (jumpRelic == null)
            {
                return;
            }

            PruneExpired(now);
            _jumpTimes.Enqueue(now);
            jumpRelic.SetCurrentJumpCount(_jumpTimes.Count);

            if (_jumpTimes.Count < WhatIfJump.RequiredJumpCount)
            {
                return;
            }

            ClearJumpState(jumpRelic);
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
            _lastProcessedJumpAtMsec = 0;
            relic?.SetCurrentJumpCount(0);
        }
    }

    private static void TryQueueJumpSlashCritical(Player owner, WhatIfJumpSlash relic)
    {
        if (!relic.CanGainCriticalThisTurn())
        {
            return;
        }

        bool requested = RitsuLibManagedNetActions.Request(
            RunManager.Instance,
            JumpSlashCriticalDescriptor,
            default,
            owner.NetId);
        if (!requested)
        {
            Entry.Logger.VeryDebug($"WhatIfJumpSlash could not enqueue managed critical action for player {owner.NetId}.");
        }
    }

    private static Task ExecuteJumpSlashCriticalAsync(RitsuLibManagedNetActionContext<JumpSlashCriticalActionPayload> context)
    {
        WhatIfJumpSlash? relic = context.Player.GetRelic<WhatIfJumpSlash>();
        return relic?.TryGrantCriticalFromJump(context.PlayerChoiceContext, context.Player) ?? Task.CompletedTask;
    }

    private sealed class JumpAnimationState
    {
        public bool HasBaseTransform { get; set; }

        public Vector2 BasePosition { get; set; }

        public float BaseRotation { get; set; }

        public Tween? ActiveTween { get; set; }
    }

    private readonly record struct JumpSlashCriticalActionPayload;

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
