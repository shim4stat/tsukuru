using System;
using System.Collections.Generic;
using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// マスターデータ定義 1 件ぶんの行動を実行するランタイム表現。
    /// フレーム基準のタイムライン上で command と window を進める。
    /// </summary>
    internal sealed class ConfiguredBossAction : IBossAction
    {
        private readonly BossActionDefinitionContract _definition;
        private readonly BossActionTimelineExecutor _timelineExecutor;

        private bool _isActive;

        public ConfiguredBossAction(BossActionDefinitionContract definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(_definition.Id))
                throw new InvalidOperationException("Boss action id is null or empty.");

            _timelineExecutor = new BossActionTimelineExecutor(_definition);
        }

        public string Id => _definition.Id;

        public bool IsCompleted { get; private set; }

        public BossActionCancelPolicy CancelPolicy => _definition.CancelPolicy;

        public BossActionExecutionResult Enter(BattleContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _timelineExecutor.Reset();
            _isActive = true;
            IsCompleted = false;
            return BuildActionEnterResult();
        }

        public BossActionExecutionResult Snapshot()
        {
            return _timelineExecutor.Snapshot();
        }

        public BossActionStepResult Update(BattleContext context, int availableFrames)
        {
            if (!_isActive || IsCompleted)
                return new BossActionStepResult(_timelineExecutor.Snapshot(), 0, IsCompleted);
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (availableFrames <= 0)
                return new BossActionStepResult(_timelineExecutor.Snapshot(), 0, false);

            int framesToProcess = availableFrames;
            if (_definition.EndConditionType == BossActionEndConditionType.DurationElapsed)
            {
                int remainingFrames = _definition.TotalDurationFrames - _timelineExecutor.ElapsedFrames;
                if (remainingFrames <= 0)
                {
                    IsCompleted = true;
                    return new BossActionStepResult(_timelineExecutor.Snapshot(), 0, true);
                }

                framesToProcess = Math.Min(framesToProcess, remainingFrames);
            }

            BossActionExecutionResult result = _timelineExecutor.Update(context, framesToProcess);
            if (_definition.EndConditionType == BossActionEndConditionType.DurationElapsed &&
                _timelineExecutor.ElapsedFrames >= _definition.TotalDurationFrames)
            {
                IsCompleted = true;
            }

            return new BossActionStepResult(result, framesToProcess, IsCompleted);
        }

        public void Exit()
        {
            _isActive = false;
        }

        private BossActionExecutionResult BuildActionEnterResult()
        {
            BossActionExecutionResult snapshot = _timelineExecutor.Snapshot();
            if (string.IsNullOrWhiteSpace(_definition.AnimationStateName))
                return snapshot;

            BossActionCommandEvent commandEvent = new BossActionCommandEvent(
                _definition.Id,
                BossActionCommandType.PlayAnimation,
                0,
                _definition.AnimationStateName,
                0,
                string.Empty,
                Vector3.Zero,
                string.Empty,
                Vector3.Zero,
                string.Empty,
                1.0f);

            return new BossActionExecutionResult(
                snapshot.SpawnRequests,
                snapshot.EmittedSignals,
                new[] { commandEvent },
                snapshot.FrameState);
        }
    }

    internal sealed class BossActionTimelineExecutor
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptySpawnRequests = Array.Empty<EnemyBulletSpawnRequest>();
        private static readonly IReadOnlyList<string> EmptySignals = Array.Empty<string>();
        private static readonly IReadOnlyList<BossActionCommandEvent> EmptyCommandEvents = Array.Empty<BossActionCommandEvent>();
        private static readonly IReadOnlyList<BossActiveHitbox> EmptyHitboxes = Array.Empty<BossActiveHitbox>();
        private static readonly IReadOnlyList<BossActiveHurtbox> EmptyHurtboxes = Array.Empty<BossActiveHurtbox>();
        private static readonly IReadOnlyList<string> EmptyCancelTags = Array.Empty<string>();

        private readonly string _actionId;
        private readonly List<IndexedBossActionCommand> _commands;
        private readonly IReadOnlyList<BossActionWindowContract> _windows;
        private readonly List<BossBulletPatternEmitterRuntime> _activeEmitters = new List<BossBulletPatternEmitterRuntime>();
        private readonly bool _usesExplicitHurtboxWindows;

        private int _currentFrame;
        private int _nextCommandIndex;

        public BossActionTimelineExecutor(BossActionDefinitionContract definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _commands = BuildSortedCommands(definition.Commands);
            _windows = definition.Windows ?? Array.Empty<BossActionWindowContract>();
            _actionId = definition.Id ?? string.Empty;
            _usesExplicitHurtboxWindows = HasWindowType(_windows, BossActionWindowType.HurtboxWindow);
        }

        public int ElapsedFrames => _currentFrame;

        public void Reset()
        {
            _currentFrame = 0;
            _nextCommandIndex = 0;
            _activeEmitters.Clear();
        }

        public BossActionExecutionResult Snapshot()
        {
            return new BossActionExecutionResult(
                EmptySpawnRequests,
                EmptySignals,
                EmptyCommandEvents,
                BuildFrameStateAtFrame(_currentFrame));
        }

        public BossActionExecutionResult Update(BattleContext context, int framesToProcess)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (framesToProcess <= 0)
                return Snapshot();

            List<EnemyBulletSpawnRequest> spawnRequests = null;
            List<string> emittedSignals = null;
            List<BossActionCommandEvent> commandEvents = null;
            BossActionFrameState frameState = BuildFrameStateAtFrame(_currentFrame);

            for (int i = 0; i < framesToProcess; i++)
            {
                FireCommandsAtCurrentFrame(ref emittedSignals, ref commandEvents);
                TickEmitters(context, ref spawnRequests);
                frameState = BuildFrameStateAtFrame(_currentFrame);
                ApplyMovement(context, frameState);
                _currentFrame++;
            }

            return new BossActionExecutionResult(
                spawnRequests ?? EmptySpawnRequests,
                emittedSignals ?? EmptySignals,
                commandEvents ?? EmptyCommandEvents,
                frameState);
        }

        private void FireCommandsAtCurrentFrame(
            ref List<string> emittedSignals,
            ref List<BossActionCommandEvent> commandEvents)
        {
            while (_nextCommandIndex < _commands.Count && _commands[_nextCommandIndex].Command.TriggerFrame == _currentFrame)
            {
                BossActionCommandContract command = _commands[_nextCommandIndex].Command;
                switch (command.CommandType)
                {
                    case BossActionCommandType.SpawnBulletPattern:
                        _activeEmitters.Add(new BossBulletPatternEmitterRuntime(command.BulletPattern, command.EmitterDurationFrames));
                        break;
                    case BossActionCommandType.EmitSignal:
                        emittedSignals ??= new List<string>();
                        emittedSignals.Add(command.SignalId ?? string.Empty);
                        break;
                    case BossActionCommandType.PlayAnimation:
                    case BossActionCommandType.SpawnEnemy:
                    case BossActionCommandType.PlayEffect:
                    case BossActionCommandType.PlaySound:
                        commandEvents ??= new List<BossActionCommandEvent>();
                        commandEvents.Add(BuildCommandEvent(command));
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown boss action command type at runtime: {command.CommandType}");
                }

                _nextCommandIndex++;
            }
        }

        private BossActionCommandEvent BuildCommandEvent(BossActionCommandContract command)
        {
            return new BossActionCommandEvent(
                _actionId,
                command.CommandType,
                command.TriggerFrame,
                command.AnimationStateName,
                command.CrossFadeFrames,
                command.EnemyDefinitionId,
                command.SpawnOffset,
                command.EffectId,
                command.EffectLocalOffset,
                command.SoundId,
                command.VolumeScale);
        }

        private void TickEmitters(BattleContext context, ref List<EnemyBulletSpawnRequest> spawnRequests)
        {
            for (int i = _activeEmitters.Count - 1; i >= 0; i--)
            {
                BossBulletPatternEmitterRuntime emitter = _activeEmitters[i];
                emitter.TickFrame(context, ref spawnRequests);
                if (emitter.IsCompleted)
                    _activeEmitters.RemoveAt(i);
            }
        }

        private BossActionFrameState BuildFrameStateAtFrame(int frame)
        {
            if (_windows == null || _windows.Count == 0)
            {
                return _usesExplicitHurtboxWindows
                    ? new BossActionFrameState(EmptyHitboxes, EmptyHurtboxes, false, Vector3.Zero, EmptyCancelTags, true)
                    : BossActionFrameState.Empty;
            }

            List<BossActiveHitbox> activeHitboxes = null;
            List<BossActiveHurtbox> activeHurtboxes = null;
            List<string> activeCancelTags = null;
            bool isInvincible = false;
            Vector3 moveVelocityPerSecond = Vector3.Zero;

            for (int i = 0; i < _windows.Count; i++)
            {
                BossActionWindowContract window = _windows[i];
                if (window == null)
                    continue;
                if (frame < window.StartFrameInclusive || frame >= window.EndFrameExclusive)
                    continue;

                switch (window.WindowType)
                {
                    case BossActionWindowType.MoveWindow:
                        if (window.MoveWindow != null)
                            moveVelocityPerSecond += window.MoveWindow.VelocityPerSecond;
                        break;
                    case BossActionWindowType.HitboxWindow:
                        if (window.HitboxWindow != null && window.HitboxWindow.Radius > 0f && window.HitboxWindow.Damage > 0)
                        {
                            activeHitboxes ??= new List<BossActiveHitbox>();
                            activeHitboxes.Add(
                                new BossActiveHitbox(
                                    window.Id,
                                    window.HitboxWindow.Offset,
                                    window.HitboxWindow.Radius,
                                    window.HitboxWindow.Damage));
                        }

                        break;
                    case BossActionWindowType.HurtboxWindow:
                        if (window.HurtboxWindow != null && window.HurtboxWindow.Radius > 0f)
                        {
                            activeHurtboxes ??= new List<BossActiveHurtbox>();
                            activeHurtboxes.Add(
                                new BossActiveHurtbox(
                                    window.Id,
                                    window.HurtboxWindow.Offset,
                                    window.HurtboxWindow.Radius));
                        }

                        break;
                    case BossActionWindowType.InvincibleWindow:
                        isInvincible = true;
                        break;
                    case BossActionWindowType.CancelWindow:
                        if (window.CancelWindow != null && !string.IsNullOrWhiteSpace(window.CancelWindow.CancelTag))
                        {
                            activeCancelTags ??= new List<string>();
                            activeCancelTags.Add(window.CancelWindow.CancelTag);
                        }

                        break;
                    default:
                        break;
                }
            }

            return new BossActionFrameState(
                activeHitboxes ?? EmptyHitboxes,
                activeHurtboxes ?? EmptyHurtboxes,
                isInvincible,
                moveVelocityPerSecond,
                activeCancelTags ?? EmptyCancelTags,
                _usesExplicitHurtboxWindows);
        }

        private static void ApplyMovement(BattleContext context, BossActionFrameState frameState)
        {
            if (context?.Boss == null)
                return;
            if (frameState.MoveVelocityPerSecond.LengthSquared() <= 0f)
                return;

            float frameDeltaTime = 1.0f / BossActionTimelineConstants.FramesPerSecond;
            Vector3 nextPosition = context.Boss.Position + (frameState.MoveVelocityPerSecond * frameDeltaTime);
            context.Boss.SetPosition(nextPosition);
        }

        private static List<IndexedBossActionCommand> BuildSortedCommands(IReadOnlyList<BossActionCommandContract> source)
        {
            List<IndexedBossActionCommand> commands = new List<IndexedBossActionCommand>();
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    if (source[i] != null)
                        commands.Add(new IndexedBossActionCommand(source[i], i));
                }
            }

            commands.Sort((x, y) =>
            {
                int frameCompare = x.Command.TriggerFrame.CompareTo(y.Command.TriggerFrame);
                return frameCompare != 0 ? frameCompare : x.Order.CompareTo(y.Order);
            });
            return commands;
        }

        private static bool HasWindowType(IReadOnlyList<BossActionWindowContract> windows, BossActionWindowType windowType)
        {
            if (windows == null)
                return false;

            for (int i = 0; i < windows.Count; i++)
            {
                BossActionWindowContract window = windows[i];
                if (window != null && window.WindowType == windowType)
                    return true;
            }

            return false;
        }

        private readonly struct IndexedBossActionCommand
        {
            public IndexedBossActionCommand(BossActionCommandContract command, int order)
            {
                Command = command;
                Order = order;
            }

            public BossActionCommandContract Command { get; }

            public int Order { get; }
        }
    }

    internal sealed class BossBulletPatternEmitterRuntime
    {
        private readonly IBossAttackPattern _pattern;
        private int? _remainingFrames;

        public BossBulletPatternEmitterRuntime(BossBulletPatternDefinitionContract definition, int? durationFrames)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _pattern = BossBulletPatternFactory.BuildPattern(definition);
            float initialDelaySeconds = definition.InitialDelayFrames >= 0
                ? definition.InitialDelayFrames / (float)BossActionTimelineConstants.FramesPerSecond
                : -1f;
            _pattern.Reset(initialDelaySeconds);
            _remainingFrames = durationFrames;
        }

        public bool IsCompleted { get; private set; }

        public void TickFrame(BattleContext context, ref List<EnemyBulletSpawnRequest> spawnRequests)
        {
            if (IsCompleted)
                return;
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (_remainingFrames.HasValue && _remainingFrames.Value <= 0)
            {
                IsCompleted = true;
                return;
            }

            IReadOnlyList<EnemyBulletSpawnRequest> frameRequests =
                _pattern.Update(context, 1.0f / BossActionTimelineConstants.FramesPerSecond);
            if (frameRequests.Count > 0)
            {
                spawnRequests ??= new List<EnemyBulletSpawnRequest>();
                for (int i = 0; i < frameRequests.Count; i++)
                    spawnRequests.Add(frameRequests[i]);
            }

            if (_remainingFrames.HasValue)
            {
                _remainingFrames--;
                if (_remainingFrames.Value <= 0)
                    IsCompleted = true;
            }
        }
    }
}
