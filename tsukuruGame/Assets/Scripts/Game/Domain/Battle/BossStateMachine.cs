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
            bool hasTransition,
            string nextStateId,
            bool isTerminalCompletion)
        {
            SpawnRequests = spawnRequests ?? Array.Empty<EnemyBulletSpawnRequest>();
            HasTransition = hasTransition;
            NextStateId = nextStateId ?? string.Empty;
            IsTerminalCompletion = isTerminalCompletion;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> SpawnRequests { get; }

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
        // 外部通知は次回 Update まで蓄積し、そのフレームで一度だけ消費する。
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
            if (bossParams.Attacks == null || bossParams.Attacks.Count == 0)
                throw new InvalidOperationException("Boss attacks are not configured.");
            if (string.IsNullOrWhiteSpace(bossParams.InitialStateId))
                throw new InvalidOperationException("Boss initial state id is null or empty.");

            Dictionary<string, BossStateDefinitionContract> stateDefinitions = BuildStateDefinitions(bossParams);
            Dictionary<string, BossAttackDefinitionContract> attackDefinitions = BuildAttackDefinitions(bossParams);
            ValidateStateDefinitions(stateDefinitions, attackDefinitions, bossParams.InitialStateId);

            _runtimeContext = new BossStateRuntimeContext(boss, stateDefinitions, attackDefinitions);
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

            // シグナルは現在状態の更新時にだけ渡し、その後は消去する。
            BossStateUpdateResult stateResult = _currentState.Update(context, deltaTime, _pendingSignals);
            _pendingSignals.Clear();

            BossBehaviorSignal signal = BossBehaviorSignal.None;
            if (stateResult.HasTransition)
            {
                BossStateType previousType = _currentState.StateType;
                _currentState.Exit();

                if (stateResult.IsTerminalCompletion)
                {
                    _currentState = null;
                    if (previousType == BossStateType.Dead)
                        // 撃破状態の終端は、進行側でリザルトや演出へつなぐ契機になる。
                        signal = BossBehaviorSignal.DeadCompleted;
                }
                else
                {
                    TransitionTo(stateResult.NextStateId);
                    if (previousType == BossStateType.Intro)
                        // Intro を抜けた瞬間に Combat 開始などへ同期できるよう通知する。
                        signal = BossBehaviorSignal.IntroCompleted;
                }
            }

            return new BossBehaviorUpdateResult(stateResult.SpawnRequests, signal);
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

        private static Dictionary<string, BossAttackDefinitionContract> BuildAttackDefinitions(BossParamsContract bossParams)
        {
            Dictionary<string, BossAttackDefinitionContract> attacksById = new Dictionary<string, BossAttackDefinitionContract>(StringComparer.Ordinal);
            IReadOnlyList<BossAttackDefinitionContract> attacks = bossParams.Attacks;
            for (int i = 0; i < attacks.Count; i++)
            {
                BossAttackDefinitionContract attack = attacks[i];
                if (attack == null)
                    throw new InvalidOperationException($"Boss attack definition is null at index {i}.");
                if (string.IsNullOrWhiteSpace(attack.Id))
                    throw new InvalidOperationException($"Boss attack id is null or empty at index {i}.");
                if (attacksById.ContainsKey(attack.Id))
                    throw new InvalidOperationException($"Boss attack id is duplicated. attackId={attack.Id}");

                attacksById.Add(attack.Id, attack);
            }

            return attacksById;
        }

        private static void ValidateStateDefinitions(
            IReadOnlyDictionary<string, BossStateDefinitionContract> statesById,
            IReadOnlyDictionary<string, BossAttackDefinitionContract> attacksById,
            string initialStateId)
        {
            if (!statesById.ContainsKey(initialStateId))
                throw new InvalidOperationException($"Boss initial state is not configured. stateId={initialStateId}");

            foreach (KeyValuePair<string, BossStateDefinitionContract> pair in statesById)
            {
                BossStateDefinitionContract state = pair.Value;
                if (state.StateType == BossStateType.Phase)
                {
                    BossAttackPlanContract plan = state.AttackPlan ?? throw new InvalidOperationException(
                        $"Boss attack plan is null. stateId={state.Id}");

                    ValidateAttackIds(plan.OpeningSequenceAttackIds, attacksById, state.Id);
                    ValidateAttackIds(plan.RandomAttackIds, attacksById, state.Id);

                    bool hasOpening = plan.OpeningSequenceAttackIds != null && plan.OpeningSequenceAttackIds.Count > 0;
                    bool hasRandom = plan.RandomAttackIds != null && plan.RandomAttackIds.Count > 0;
                    if (!hasOpening && !hasRandom)
                    {
                        throw new InvalidOperationException(
                            $"Phase state requires at least one attack reference. stateId={state.Id}");
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
                        // 状態機械の終端は撃破後だけに限定し、途中状態で停止しないようにする。
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

        private static void ValidateAttackIds(
            IReadOnlyList<string> attackIds,
            IReadOnlyDictionary<string, BossAttackDefinitionContract> attacksById,
            string stateId)
        {
            if (attackIds == null)
                return;

            for (int i = 0; i < attackIds.Count; i++)
            {
                string attackId = attackIds[i];
                if (string.IsNullOrWhiteSpace(attackId))
                    throw new InvalidOperationException($"Boss attack id is null or empty. stateId={stateId}, index={i}");
                if (!attacksById.ContainsKey(attackId))
                {
                    throw new InvalidOperationException(
                        $"Boss attack reference is not configured. stateId={stateId}, attackId={attackId}");
                }
            }
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
                    return new PhaseBossState(definition, CreateAttackController(runtimeContext, definition.AttackPlan));
                case BossStateType.Dead:
                    return new DeadBossState(definition);
                default:
                    throw new InvalidOperationException($"Unsupported boss state type: {definition.StateType}");
            }
        }

        private static BossAttackController CreateAttackController(
            BossStateRuntimeContext runtimeContext,
            BossAttackPlanContract attackPlan)
        {
            Dictionary<string, IBossAttack> attacksById = new Dictionary<string, IBossAttack>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, BossAttackDefinitionContract> pair in runtimeContext.AttackDefinitions)
                attacksById.Add(pair.Key, new ConfiguredBossAttack(pair.Value));

            return new BossAttackController(
                attackPlan ?? new BossAttackPlanContract(),
                attacksById,
                new OpeningSequenceThenRandomSelector());
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
            IReadOnlyList<EnemyBulletSpawnRequest> spawnRequests = OnUpdate(context, deltaTime);
            if (deltaTime > 0f)
                _elapsedSeconds += deltaTime;

            // 遷移判定は状態更新後に行い、このフレームで出した弾を残したまま次状態へ移れるようにする。
            BossStateTransitionContract transition = FindTransition(context, pendingSignals);
            if (transition == null)
                return new BossStateUpdateResult(spawnRequests, false, string.Empty, false);

            bool isTerminalCompletion = string.IsNullOrWhiteSpace(transition.NextStateId);
            return new BossStateUpdateResult(spawnRequests, true, transition.NextStateId, isTerminalCompletion);
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

        protected abstract IReadOnlyList<EnemyBulletSpawnRequest> OnUpdate(BattleContext context, float deltaTime);

        protected virtual bool IsCurrentAttackCompleted => false;

        private BossStateTransitionContract FindTransition(BattleContext context, IReadOnlyCollection<string> pendingSignals)
        {
            IReadOnlyList<BossStateTransitionContract> transitions = _definition.Transitions;
            if (transitions == null || transitions.Count == 0)
                return null;

            // 遷移は定義順で評価し、最初に成立したものを採用する。
            for (int i = 0; i < transitions.Count; i++)
            {
                BossStateTransitionContract transition = transitions[i];
                if (transition == null)
                    continue;
                if (IsTransitionSatisfied(transition, context, pendingSignals))
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
                case BossTransitionConditionType.CurrentAttackCompleted:
                    return IsCurrentAttackCompleted;
                case BossTransitionConditionType.CurrentGaugeIndexAtOrAbove:
                    return context != null &&
                        context.Boss != null &&
                        context.Boss.GetCurrentGaugeIndex() >= (int)transition.Threshold;
                default:
                    throw new InvalidOperationException($"Unsupported boss transition condition type: {transition.ConditionType}");
            }
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
    /// 弾は撃たず、主に時間経過や外部シグナルで次状態へ進む。
    /// </summary>
    internal sealed class IntroBossState : BossStateBase
    {
        public IntroBossState(BossStateDefinitionContract definition)
            : base(definition)
        {
        }

        protected override IReadOnlyList<EnemyBulletSpawnRequest> OnUpdate(BattleContext context, float deltaTime)
        {
            return Array.Empty<EnemyBulletSpawnRequest>();
        }
    }

    /// <summary>
    /// 実際の攻撃を行うフェーズ状態。
    /// 攻撃計画の進行は BossAttackController に委譲する。
    /// </summary>
    internal sealed class PhaseBossState : BossStateBase
    {
        private readonly BossAttackController _attackController;

        public PhaseBossState(BossStateDefinitionContract definition, BossAttackController attackController)
            : base(definition)
        {
            _attackController = attackController ?? throw new ArgumentNullException(nameof(attackController));
        }

        // 「現在の攻撃が完了した」を遷移条件として使えるように公開する。
        protected override bool IsCurrentAttackCompleted => _attackController.LastAttackCompletedThisUpdate;

        protected override void OnEnter()
        {
            _attackController.Enter();
        }

        protected override IReadOnlyList<EnemyBulletSpawnRequest> OnUpdate(BattleContext context, float deltaTime)
        {
            return _attackController.Update(context, deltaTime);
        }

        protected override void OnExit()
        {
            _attackController.Exit();
        }
    }

    /// <summary>
    /// 撃破後の状態。
    /// 新しい攻撃は行わず、終端遷移の成立だけを待つ。
    /// </summary>
    internal sealed class DeadBossState : BossStateBase
    {
        public DeadBossState(BossStateDefinitionContract definition)
            : base(definition)
        {
        }

        protected override IReadOnlyList<EnemyBulletSpawnRequest> OnUpdate(BattleContext context, float deltaTime)
        {
            return Array.Empty<EnemyBulletSpawnRequest>();
        }
    }
}
