using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// ボス行動 1 フレーム分の更新結果。
    /// 生成した弾、発火したシグナル、その時点で有効な Window をまとめて返す。
    /// </summary>
    internal readonly struct BossActionExecutionResult
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptySpawnRequests = Array.Empty<EnemyBulletSpawnRequest>();
        private static readonly IReadOnlyList<string> EmptySignals = Array.Empty<string>();

        public static BossActionExecutionResult Empty => new BossActionExecutionResult(
            EmptySpawnRequests,
            EmptySignals,
            BossActionFrameState.Empty);

        public BossActionExecutionResult(
            IReadOnlyList<EnemyBulletSpawnRequest> spawnRequests,
            IReadOnlyList<string> emittedSignals,
            BossActionFrameState frameState)
        {
            SpawnRequests = spawnRequests ?? EmptySpawnRequests;
            EmittedSignals = emittedSignals ?? EmptySignals;
            FrameState = frameState;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> SpawnRequests { get; }

        public IReadOnlyList<string> EmittedSignals { get; }

        public BossActionFrameState FrameState { get; }

        public BossActionExecutionResult Merge(BossActionExecutionResult other)
        {
            IReadOnlyList<EnemyBulletSpawnRequest> mergedSpawnRequests = MergeLists(SpawnRequests, other.SpawnRequests);
            IReadOnlyList<string> mergedSignals = MergeLists(EmittedSignals, other.EmittedSignals);
            return new BossActionExecutionResult(mergedSpawnRequests, mergedSignals, other.FrameState);
        }

        private static IReadOnlyList<T> MergeLists<T>(IReadOnlyList<T> first, IReadOnlyList<T> second)
        {
            bool hasFirst = first != null && first.Count > 0;
            bool hasSecond = second != null && second.Count > 0;
            if (!hasFirst)
                return hasSecond ? second : Array.Empty<T>();
            if (!hasSecond)
                return first;

            List<T> merged = new List<T>(first.Count + second.Count);
            for (int i = 0; i < first.Count; i++)
                merged.Add(first[i]);
            for (int i = 0; i < second.Count; i++)
                merged.Add(second[i]);
            return merged;
        }
    }

    /// <summary>
    /// 1 回の action update 呼び出しで何 frame 消費したかと、完了有無を返す。
    /// </summary>
    internal readonly struct BossActionStepResult
    {
        public BossActionStepResult(BossActionExecutionResult executionResult, int consumedFrames, bool completed)
        {
            ExecutionResult = executionResult;
            ConsumedFrames = consumedFrames;
            Completed = completed;
        }

        public BossActionExecutionResult ExecutionResult { get; }

        public int ConsumedFrames { get; }

        public bool Completed { get; }
    }

    /// <summary>
    /// ボス状態機械が扱う状態の共通契約。
    /// 各状態は Enter / Update / Exit のライフサイクルを持つ。
    /// </summary>
    internal interface IBossState
    {
        string Id { get; }

        BossStateType StateType { get; }

        void Enter();

        BossStateUpdateResult Update(BattleContext context, float deltaTime, IReadOnlyCollection<string> pendingSignals);

        void Exit();
    }

    /// <summary>
    /// フェーズ内で実行される単発の行動定義の共通契約。
    /// 状態側はこの契約を通して行動の開始、更新、終了を制御する。
    /// </summary>
    internal interface IBossAction
    {
        string Id { get; }

        bool IsCompleted { get; }

        void Enter(BattleContext context);

        BossActionExecutionResult Snapshot();

        BossActionStepResult Update(BattleContext context, int availableFrames);

        BossActionCancelPolicy CancelPolicy { get; }

        void Exit();
    }

    /// <summary>
    /// 次に実行する行動を選ぶ責務を切り出したインターフェース。
    /// オープニング順序や履歴による重複回避を差し替えやすくする。
    /// </summary>
    internal interface IActionSelector
    {
        ActionSelectionResult SelectNext(
            BossActionPlanContract plan,
            IReadOnlyList<string> actionHistory,
            int openingIndex);
    }
}
