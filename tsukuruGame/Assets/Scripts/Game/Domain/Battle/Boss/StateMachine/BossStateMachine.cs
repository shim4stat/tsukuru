using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// 1 状態の更新結果。
    /// 弾生成要求と、次状態への遷移有無を状態機械本体へ返す。
    /// </summary>
    internal readonly struct BossStateUpdateResult
    {
        public BossStateUpdateResult(
            IReadOnlyList<EnemyBulletSpawnRequest> spawnRequests,
            IReadOnlyList<BossActionCommandEvent> commandEvents,
            BossActionFrameState frameState,
            bool hasTransition,
            string nextStateId,
            bool isTerminalCompletion)
        {
            SpawnRequests = spawnRequests ?? Array.Empty<EnemyBulletSpawnRequest>();
            CommandEvents = commandEvents ?? Array.Empty<BossActionCommandEvent>();
            FrameState = frameState;
            HasTransition = hasTransition;
            NextStateId = nextStateId ?? string.Empty;
            IsTerminalCompletion = isTerminalCompletion;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> SpawnRequests { get; }

        public IReadOnlyList<BossActionCommandEvent> CommandEvents { get; }

        public BossActionFrameState FrameState { get; }

        public bool HasTransition { get; }

        public string NextStateId { get; }

        public bool IsTerminalCompletion { get; }
    }

    /// <summary>
    /// マスターデータ定義に基づいてボスの状態遷移を進める状態機械。
    /// Intro / Phase / Dead の各状態と、その間の遷移条件を管理する。
    /// </summary>
    public sealed class BossStateMachine
    {
        private readonly HashSet<string> _pendingSignals = new HashSet<string>(StringComparer.Ordinal);

        private BossStateRuntimeContext _runtimeContext;
        private Dictionary<string, IBossState> _statesById = new Dictionary<string, IBossState>(StringComparer.Ordinal);
        private string _initialStateId = string.Empty;
        private IBossState _currentState;
        private bool _isInitialized;

        public void Initialize(Boss boss, BossParamsContract bossParams)
        {
            if (boss == null)
                throw new ArgumentNullException(nameof(boss));
            if (bossParams == null)
                throw new ArgumentNullException(nameof(bossParams));
            if (bossParams.States == null || bossParams.States.Count == 0)
                throw new InvalidOperationException("Boss states are not configured.");
            if (bossParams.Actions == null || bossParams.Actions.Count == 0)
                throw new InvalidOperationException("Boss actions are not configured.");
            if (string.IsNullOrWhiteSpace(bossParams.InitialStateId))
                throw new InvalidOperationException("Boss initial state id is null or empty.");

            Dictionary<string, BossStateDefinitionContract> stateDefinitions = BuildStateDefinitions(bossParams);
            Dictionary<string, BossActionDefinitionContract> actionDefinitions = BuildActionDefinitions(bossParams);
            ValidateStateDefinitions(stateDefinitions, actionDefinitions, bossParams.InitialStateId);

            _runtimeContext = new BossStateRuntimeContext(boss, stateDefinitions, actionDefinitions);
            _statesById = BuildStates(_runtimeContext);
            _initialStateId = bossParams.InitialStateId;
            _pendingSignals.Clear();
            TransitionTo(_initialStateId);
            _isInitialized = true;
        }

        public BossBehaviorUpdateResult Update(BattleContext context, float deltaTime)
        {
            EnsureInitialized();

            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Boss == null)
                throw new InvalidOperationException("BattleContext.Boss is not initialized.");
            if (!ReferenceEquals(context.Boss, _runtimeContext.Boss))
                throw new InvalidOperationException("BattleContext.Boss does not match initialized boss.");
            if (_currentState == null)
                return BossBehaviorUpdateResult.Empty;

            BossStateUpdateResult stateResult = _currentState.Update(context, deltaTime, _pendingSignals);
            _pendingSignals.Clear();

            BossBehaviorSignal signal = BossBehaviorSignal.None;
            BossActionFrameState frameState = stateResult.FrameState;
            if (stateResult.HasTransition)
            {
                BossStateType previousType = _currentState.StateType;
                _currentState.Exit();
                frameState = BossActionFrameState.Empty;

                if (stateResult.IsTerminalCompletion) 
                {
                    _currentState = null;
                    if (previousType == BossStateType.Dead)
                        signal = BossBehaviorSignal.DeadCompleted;
                }
                else
                {
                    TransitionTo(stateResult.NextStateId);
                    if (previousType == BossStateType.Intro)
                        signal = BossBehaviorSignal.IntroCompleted;
                }
            }

            return new BossBehaviorUpdateResult(stateResult.SpawnRequests, stateResult.CommandEvents, signal, frameState);
        }

        public void NotifySignal(string signalId)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(signalId))
                throw new ArgumentException("signalId is null or empty.", nameof(signalId));

            _pendingSignals.Add(signalId);
        }

        public void Reset()
        {
            EnsureInitialized();

            _currentState?.Exit();
            _pendingSignals.Clear();
            TransitionTo(_initialStateId);
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("BossStateMachine is not initialized.");
        }

        private void TransitionTo(string stateId)
        {
            if (string.IsNullOrWhiteSpace(stateId))
                throw new InvalidOperationException("Next boss state id is null or empty.");
            if (!_statesById.TryGetValue(stateId, out IBossState nextState))
                throw new InvalidOperationException($"Boss state is not configured. stateId={stateId}");

            _currentState = nextState;
            _currentState.Enter();
        }

        private static Dictionary<string, BossStateDefinitionContract> BuildStateDefinitions(BossParamsContract bossParams)
        {
            Dictionary<string, BossStateDefinitionContract> statesById = new Dictionary<string, BossStateDefinitionContract>(StringComparer.Ordinal);
            IReadOnlyList<BossStateDefinitionContract> states = bossParams.States;
            for (int i = 0; i < states.Count; i++)
            {
                BossStateDefinitionContract state = states[i];
                if (state == null)
                    throw new InvalidOperationException($"Boss state definition is null at index {i}.");
                if (string.IsNullOrWhiteSpace(state.Id))
                    throw new InvalidOperationException($"Boss state id is null or empty at index {i}.");
                if (statesById.ContainsKey(state.Id))
                    throw new InvalidOperationException($"Boss state id is duplicated. stateId={state.Id}");

                statesById.Add(state.Id, state);
            }

            return statesById;
        }

        private static Dictionary<string, BossActionDefinitionContract> BuildActionDefinitions(BossParamsContract bossParams)
        {
            Dictionary<string, BossActionDefinitionContract> actionsById =
                new Dictionary<string, BossActionDefinitionContract>(StringComparer.Ordinal);
            IReadOnlyList<BossActionDefinitionContract> actions = bossParams.Actions;
            for (int i = 0; i < actions.Count; i++)
            {
                BossActionDefinitionContract action = actions[i];
                if (action == null)
                    throw new InvalidOperationException($"Boss action definition is null at index {i}.");
                if (string.IsNullOrWhiteSpace(action.Id))
                    throw new InvalidOperationException($"Boss action id is null or empty at index {i}.");
                if (actionsById.ContainsKey(action.Id))
                    throw new InvalidOperationException($"Boss action id is duplicated. actionId={action.Id}");

                actionsById.Add(action.Id, action);
            }

            return actionsById;
        }

        private static void ValidateStateDefinitions(
            IReadOnlyDictionary<string, BossStateDefinitionContract> statesById,
            IReadOnlyDictionary<string, BossActionDefinitionContract> actionsById,
            string initialStateId)
        {
            if (!statesById.ContainsKey(initialStateId))
                throw new InvalidOperationException($"Boss initial state is not configured. stateId={initialStateId}");

            foreach (KeyValuePair<string, BossActionDefinitionContract> pair in actionsById)
                ValidateActionDefinition(pair.Value);

            foreach (KeyValuePair<string, BossStateDefinitionContract> pair in statesById)
            {
                BossStateDefinitionContract state = pair.Value;
                if (state.StateType == BossStateType.Phase)
                {
                    BossActionPlanContract plan = state.ActionPlan ?? throw new InvalidOperationException(
                        $"Boss action plan is null. stateId={state.Id}");

                    ValidateActionIds(plan.OpeningSequenceActionIds, actionsById, state.Id);
                    ValidateActionIds(plan.RandomActionIds, actionsById, state.Id);

                    bool hasOpening = plan.OpeningSequenceActionIds != null && plan.OpeningSequenceActionIds.Count > 0;
                    bool hasRandom = plan.RandomActionIds != null && plan.RandomActionIds.Count > 0;
                    if (!hasOpening && !hasRandom)
                    {
                        throw new InvalidOperationException(
                            $"Phase state requires at least one action reference. stateId={state.Id}");
                    }
                }

                IReadOnlyList<BossStateTransitionContract> transitions = state.Transitions;
                if (transitions == null)
                    continue;

                for (int i = 0; i < transitions.Count; i++)
                {
                    BossStateTransitionContract transition = transitions[i];
                    if (transition == null)
                        throw new InvalidOperationException($"Boss transition is null. stateId={state.Id}, index={i}");

                    bool isTerminal = string.IsNullOrWhiteSpace(transition.NextStateId);
                    if (isTerminal)
                    {
                        if (state.StateType != BossStateType.Dead)
                        {
                            throw new InvalidOperationException(
                                $"Terminal transition is only allowed from Dead state. stateId={state.Id}, index={i}");
                        }

                        continue;
                    }

                    if (!statesById.ContainsKey(transition.NextStateId))
                    {
                        throw new InvalidOperationException(
                            $"Boss transition target state is not configured. stateId={state.Id}, nextStateId={transition.NextStateId}");
                    }
                }
            }
        }

        private static void ValidateActionIds(
            IReadOnlyList<string> actionIds,
            IReadOnlyDictionary<string, BossActionDefinitionContract> actionsById,
            string stateId)
        {
            if (actionIds == null)
                return;

            for (int i = 0; i < actionIds.Count; i++)
            {
                string actionId = actionIds[i];
                if (string.IsNullOrWhiteSpace(actionId))
                    throw new InvalidOperationException($"Boss action id is null or empty. stateId={stateId}, index={i}");
                if (!actionsById.ContainsKey(actionId))
                {
                    throw new InvalidOperationException(
                        $"Boss action reference is not configured. stateId={stateId}, actionId={actionId}");
                }
            }
        }

        private static void ValidateActionDefinition(BossActionDefinitionContract action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrWhiteSpace(action.Id))
                throw new InvalidOperationException("Boss action id is null or empty.");

            bool hasCommands = action.Commands != null && action.Commands.Count > 0;
            bool hasWindows = action.Windows != null && action.Windows.Count > 0;
            bool hasActionAnimation = !string.IsNullOrWhiteSpace(action.AnimationStateName);
            if (!hasCommands && !hasWindows && !hasActionAnimation)
                throw new InvalidOperationException($"Boss action has no commands, windows, or animation. actionId={action.Id}");
            if (action.CancelPolicy == BossActionCancelPolicy.Windowed && !HasWindowType(action.Windows, BossActionWindowType.CancelWindow))
            {
                throw new InvalidOperationException(
                    $"Windowed cancel policy requires CancelWindow. actionId={action.Id}");
            }

            int durationLimit = int.MaxValue;
            switch (action.EndConditionType)
            {
                case BossActionEndConditionType.Manual:
                    if (action.TotalDurationFrames < 0)
                    {
                        throw new InvalidOperationException(
                            $"Manual boss action duration must be non-negative. actionId={action.Id}, totalDurationFrames={action.TotalDurationFrames}");
                    }

                    break;
                case BossActionEndConditionType.DurationElapsed:
                    if (action.TotalDurationFrames <= 0)
                    {
                        throw new InvalidOperationException(
                            $"DurationElapsed boss action must have positive duration. actionId={action.Id}, totalDurationFrames={action.TotalDurationFrames}");
                    }

                    durationLimit = action.TotalDurationFrames;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported boss action end condition: {action.EndConditionType}");
            }

            ValidateCommands(action, durationLimit);
            ValidateWindows(action, durationLimit);
        }

        private static void ValidateCommands(BossActionDefinitionContract action, int durationLimit)
        {
            if (action.Commands == null)
                return;

            for (int i = 0; i < action.Commands.Count; i++)
            {
                BossActionCommandContract command = action.Commands[i];
                if (command == null)
                    throw new InvalidOperationException($"Boss action command is null. actionId={action.Id}, index={i}");
                if (command.TriggerFrame < 0)
                {
                    throw new InvalidOperationException(
                        $"Boss action command trigger frame must be non-negative. actionId={action.Id}, index={i}, triggerFrame={command.TriggerFrame}");
                }
                if (durationLimit != int.MaxValue && command.TriggerFrame >= durationLimit)
                {
                    throw new InvalidOperationException(
                        $"Boss action command trigger frame exceeds action duration. actionId={action.Id}, index={i}, triggerFrame={command.TriggerFrame}, duration={durationLimit}");
                }

                switch (command.CommandType)
                {
                    case BossActionCommandType.SpawnBulletPattern:
                        if (command.BulletPattern == null)
                        {
                            throw new InvalidOperationException(
                                $"SpawnBulletPattern command requires bullet pattern. actionId={action.Id}, index={i}");
                        }

                        BossBulletPatternFactory.ValidatePatternDefinition(command.BulletPattern);
                        if (command.EmitterDurationFrames.HasValue)
                        {
                            if (command.EmitterDurationFrames.Value <= 0)
                            {
                                throw new InvalidOperationException(
                                    $"Emitter duration must be positive when specified. actionId={action.Id}, index={i}, emitterDurationFrames={command.EmitterDurationFrames.Value}");
                            }

                            if (durationLimit != int.MaxValue &&
                                command.TriggerFrame + command.EmitterDurationFrames.Value > durationLimit)
                            {
                                throw new InvalidOperationException(
                                    $"Emitter duration exceeds action duration. actionId={action.Id}, index={i}");
                            }
                        }

                        break;
                    case BossActionCommandType.EmitSignal:
                        if (string.IsNullOrWhiteSpace(command.SignalId))
                        {
                            throw new InvalidOperationException(
                                $"EmitSignal command requires signal id. actionId={action.Id}, index={i}");
                        }

                        break;
                    case BossActionCommandType.PlayAnimation:
                        if (string.IsNullOrWhiteSpace(command.AnimationStateName))
                        {
                            throw new InvalidOperationException(
                                $"PlayAnimation command requires animation state name. actionId={action.Id}, index={i}");
                        }

                        if (command.CrossFadeFrames < 0)
                        {
                            throw new InvalidOperationException(
                                $"PlayAnimation command cross fade frames must be non-negative. actionId={action.Id}, index={i}, crossFadeFrames={command.CrossFadeFrames}");
                        }

                        break;
                    case BossActionCommandType.SpawnEnemy:
                        if (string.IsNullOrWhiteSpace(command.EnemyDefinitionId))
                        {
                            throw new InvalidOperationException(
                                $"SpawnEnemy command requires enemy definition id. actionId={action.Id}, index={i}");
                        }

                        break;
                    case BossActionCommandType.PlayEffect:
                        if (string.IsNullOrWhiteSpace(command.EffectId))
                        {
                            throw new InvalidOperationException(
                                $"PlayEffect command requires effect id. actionId={action.Id}, index={i}");
                        }

                        break;
                    case BossActionCommandType.PlaySound:
                        if (string.IsNullOrWhiteSpace(command.SoundId))
                        {
                            throw new InvalidOperationException(
                                $"PlaySound command requires sound id. actionId={action.Id}, index={i}");
                        }

                        if (command.VolumeScale < 0f)
                        {
                            throw new InvalidOperationException(
                                $"PlaySound command volume scale must be non-negative. actionId={action.Id}, index={i}, volumeScale={command.VolumeScale}");
                        }

                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unknown boss action command type. actionId={action.Id}, index={i}, commandType={command.CommandType}");
                }
            }
        }

        private static void ValidateWindows(BossActionDefinitionContract action, int durationLimit)
        {
            if (action.Windows == null)
                return;

            for (int i = 0; i < action.Windows.Count; i++)
            {
                BossActionWindowContract window = action.Windows[i];
                if (window == null)
                    throw new InvalidOperationException($"Boss action window is null. actionId={action.Id}, index={i}");
                if (window.StartFrameInclusive < 0)
                {
                    throw new InvalidOperationException(
                        $"Boss action window start frame must be non-negative. actionId={action.Id}, index={i}, startFrame={window.StartFrameInclusive}");
                }
                if (window.EndFrameExclusive <= window.StartFrameInclusive)
                {
                    throw new InvalidOperationException(
                        $"Boss action window end frame must be greater than start frame. actionId={action.Id}, index={i}");
                }
                if (durationLimit != int.MaxValue && window.EndFrameExclusive > durationLimit)
                {
                    throw new InvalidOperationException(
                        $"Boss action window exceeds action duration. actionId={action.Id}, index={i}");
                }

                switch (window.WindowType)
                {
                    case BossActionWindowType.MoveWindow:
                        if (window.MoveWindow == null)
                        {
                            throw new InvalidOperationException(
                                $"MoveWindow requires payload. actionId={action.Id}, index={i}");
                        }

                        break;
                    case BossActionWindowType.HitboxWindow:
                        if (window.HitboxWindow == null || window.HitboxWindow.Radius <= 0f || window.HitboxWindow.Damage <= 0)
                        {
                            throw new InvalidOperationException(
                                $"HitboxWindow requires positive radius and damage. actionId={action.Id}, index={i}");
                        }

                        break;
                    case BossActionWindowType.HurtboxWindow:
                        if (window.HurtboxWindow == null || window.HurtboxWindow.Radius <= 0f)
                        {
                            throw new InvalidOperationException(
                                $"HurtboxWindow requires positive radius. actionId={action.Id}, index={i}");
                        }

                        break;
                    case BossActionWindowType.InvincibleWindow:
                        break;
                    case BossActionWindowType.CancelWindow:
                        if (window.CancelWindow == null || string.IsNullOrWhiteSpace(window.CancelWindow.CancelTag))
                        {
                            throw new InvalidOperationException(
                                $"CancelWindow requires cancel tag. actionId={action.Id}, index={i}");
                        }

                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unsupported boss action window type. actionId={action.Id}, index={i}, windowType={window.WindowType}");
                }
            }
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

        private static Dictionary<string, IBossState> BuildStates(BossStateRuntimeContext runtimeContext)
        {
            Dictionary<string, IBossState> statesById = new Dictionary<string, IBossState>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, BossStateDefinitionContract> pair in runtimeContext.StateDefinitions)
                statesById.Add(pair.Key, CreateState(runtimeContext, pair.Value));

            return statesById;
        }

        private static IBossState CreateState(BossStateRuntimeContext runtimeContext, BossStateDefinitionContract definition)
        {
            switch (definition.StateType)
            {
                case BossStateType.Intro:
                    return new IntroBossState(definition);
                case BossStateType.Phase:
                    return new PhaseBossState(definition, CreateActionController(runtimeContext, definition.ActionPlan));
                case BossStateType.Dead:
                    return new DeadBossState(definition);
                default:
                    throw new InvalidOperationException($"Unsupported boss state type: {definition.StateType}");
            }
        }

        private static BossActionController CreateActionController(
            BossStateRuntimeContext runtimeContext,
            BossActionPlanContract actionPlan)
        {
            Dictionary<string, IBossAction> actionsById = new Dictionary<string, IBossAction>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, BossActionDefinitionContract> pair in runtimeContext.ActionDefinitions)
                actionsById.Add(pair.Key, new ConfiguredBossAction(pair.Value));

            return new BossActionController(
                actionPlan ?? new BossActionPlanContract(),
                actionsById,
                new OpeningSequenceThenRandomActionSelector());
        }
    }

    /// <summary>
    /// 各状態実装の共通基底。
    /// 遷移判定、経過時間計測、Enter / Exit の定型処理をまとめる。
    /// </summary>
    internal abstract class BossStateBase : IBossState
    {
        private readonly BossStateDefinitionContract _definition;
        private float _elapsedSeconds;

        protected BossStateBase(BossStateDefinitionContract definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public string Id => _definition.Id;

        public BossStateType StateType => _definition.StateType;

        public void Enter()
        {
            _elapsedSeconds = 0f;
            OnEnter();
        }

        public BossStateUpdateResult Update(BattleContext context, float deltaTime, IReadOnlyCollection<string> pendingSignals)
        {
            BossActionExecutionResult actionResult = OnUpdate(context, deltaTime);
            if (deltaTime > 0f)
                _elapsedSeconds += deltaTime;

            IReadOnlyCollection<string> transitionSignals = MergeSignals(pendingSignals, actionResult.EmittedSignals);
            BossStateTransitionContract transition = FindTransition(context, transitionSignals, actionResult);
            if (transition == null)
                return new BossStateUpdateResult(
                    actionResult.SpawnRequests,
                    actionResult.CommandEvents,
                    actionResult.FrameState,
                    false,
                    string.Empty,
                    false);

            bool isTerminalCompletion = string.IsNullOrWhiteSpace(transition.NextStateId);
            return new BossStateUpdateResult(
                actionResult.SpawnRequests,
                actionResult.CommandEvents,
                actionResult.FrameState,
                true,
                transition.NextStateId,
                isTerminalCompletion);
        }

        public void Exit()
        {
            OnExit();
        }

        protected virtual void OnEnter()
        {
        }

        protected virtual void OnExit()
        {
        }

        protected abstract BossActionExecutionResult OnUpdate(BattleContext context, float deltaTime);

        protected virtual bool IsCurrentActionCompleted => false;

        protected virtual bool CanTransition(BossStateTransitionContract transition, BossActionExecutionResult actionResult)
        {
            return true;
        }

        private BossStateTransitionContract FindTransition(
            BattleContext context,
            IReadOnlyCollection<string> pendingSignals,
            BossActionExecutionResult actionResult)
        {
            IReadOnlyList<BossStateTransitionContract> transitions = _definition.Transitions;
            if (transitions == null || transitions.Count == 0)
                return null;

            for (int i = 0; i < transitions.Count; i++)
            {
                BossStateTransitionContract transition = transitions[i];
                if (transition == null)
                    continue;
                if (IsTransitionSatisfied(transition, context, pendingSignals) && CanTransition(transition, actionResult))
                    return transition;
            }

            return null;
        }

        private bool IsTransitionSatisfied(
            BossStateTransitionContract transition,
            BattleContext context,
            IReadOnlyCollection<string> pendingSignals)
        {
            switch (transition.ConditionType)
            {
                case BossTransitionConditionType.ExternalSignal:
                    return ContainsSignal(pendingSignals, transition.SignalId);
                case BossTransitionConditionType.ElapsedTime:
                    return _elapsedSeconds >= transition.Threshold;
                case BossTransitionConditionType.CurrentHpRateAtOrBelow:
                    return context != null &&
                        context.Boss != null &&
                        context.Boss.GetCurrentGaugeHpNormalized() <= transition.Threshold;
                case BossTransitionConditionType.CurrentActionCompleted:
                    return IsCurrentActionCompleted;
                case BossTransitionConditionType.CurrentGaugeIndexAtOrAbove:
                    return context != null &&
                        context.Boss != null &&
                        context.Boss.GetCurrentGaugeIndex() >= (int)transition.Threshold;
                default:
                    throw new InvalidOperationException($"Unsupported boss transition condition type: {transition.ConditionType}");
            }
        }

        private static IReadOnlyCollection<string> MergeSignals(
            IReadOnlyCollection<string> pendingSignals,
            IReadOnlyList<string> emittedSignals)
        {
            bool hasPending = pendingSignals != null && pendingSignals.Count > 0;
            bool hasEmitted = emittedSignals != null && emittedSignals.Count > 0;
            if (!hasPending && !hasEmitted)
                return Array.Empty<string>();
            if (!hasEmitted)
                return pendingSignals;
            if (!hasPending)
                return emittedSignals;

            HashSet<string> merged = new HashSet<string>(StringComparer.Ordinal);
            foreach (string signal in pendingSignals)
            {
                if (!string.IsNullOrWhiteSpace(signal))
                    merged.Add(signal);
            }

            for (int i = 0; i < emittedSignals.Count; i++)
            {
                string signal = emittedSignals[i];
                if (!string.IsNullOrWhiteSpace(signal))
                    merged.Add(signal);
            }

            return merged;
        }

        private static bool ContainsSignal(IReadOnlyCollection<string> pendingSignals, string signalId)
        {
            if (pendingSignals == null || string.IsNullOrWhiteSpace(signalId))
                return false;

            foreach (string pendingSignal in pendingSignals)
            {
                if (string.Equals(pendingSignal, signalId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// ボス登場演出中の状態。
    /// 主に時間経過や外部シグナルで次状態へ進む。
    /// </summary>
    internal sealed class IntroBossState : BossStateBase
    {
        public IntroBossState(BossStateDefinitionContract definition)
            : base(definition)
        {
        }

        protected override BossActionExecutionResult OnUpdate(BattleContext context, float deltaTime)
        {
            return BossActionExecutionResult.Empty;
        }
    }

    /// <summary>
    /// 実際の行動を行うフェーズ状態。
    /// 行動計画の進行は BossActionController に委譲する。
    /// </summary>
    internal sealed class PhaseBossState : BossStateBase
    {
        private readonly BossActionController _actionController;

        public PhaseBossState(BossStateDefinitionContract definition, BossActionController actionController)
            : base(definition)
        {
            _actionController = actionController ?? throw new ArgumentNullException(nameof(actionController));
        }

        protected override bool IsCurrentActionCompleted => _actionController.LastActionCompletedThisUpdate;

        protected override void OnEnter()
        {
            _actionController.Enter();
        }

        protected override BossActionExecutionResult OnUpdate(BattleContext context, float deltaTime)
        {
            return _actionController.Update(context, deltaTime);
        }

        protected override bool CanTransition(BossStateTransitionContract transition, BossActionExecutionResult actionResult)
        {
            if (transition == null)
                return false;

            return _actionController.CanInterruptCurrentAction(transition.ConditionType);
        }

        protected override void OnExit()
        {
            _actionController.Exit();
        }
    }

    /// <summary>
    /// 撃破後の状態。
    /// 新しい行動は行わず、終端遷移の成立だけを待つ。
    /// </summary>
    internal sealed class DeadBossState : BossStateBase
    {
        public DeadBossState(BossStateDefinitionContract definition)
            : base(definition)
        {
        }

        protected override BossActionExecutionResult OnUpdate(BattleContext context, float deltaTime)
        {
            return BossActionExecutionResult.Empty;
        }
    }
}
