using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
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
    /// フェーズ内で実行される単発の攻撃定義の共通契約。
    /// 状態側はこの契約を通して攻撃の開始、更新、終了を制御する。
    /// </summary>
    internal interface IBossAttack
    {
        string Id { get; }

        bool IsCompleted { get; }

        void Enter(BattleContext context);

        IReadOnlyList<EnemyBulletSpawnRequest> Update(BattleContext context, float deltaTime);

        void Exit();
    }

    /// <summary>
    /// 次に実行する攻撃を選ぶ責務を切り出したインターフェース。
    /// オープニング順序や履歴による重複回避を差し替えやすくする。
    /// </summary>
    internal interface IAttackSelector
    {
        AttackSelectionResult SelectNext(
            BossAttackPlanContract plan,
            IReadOnlyList<string> attackHistory,
            int openingIndex);
    }
}
