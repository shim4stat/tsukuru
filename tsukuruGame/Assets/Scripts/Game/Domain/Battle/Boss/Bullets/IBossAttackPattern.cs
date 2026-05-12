using System.Collections.Generic;

namespace Game.Domain.Battle
{
    /// <summary>
    /// 弾幕パターンの最小契約。
    /// 1 回の攻撃の中で時間経過に応じた弾生成要求を返す。
    /// </summary>
    public interface IBossAttackPattern
    {
        void Reset();

        void Reset(float initialDelaySeconds);

        IReadOnlyList<EnemyBulletSpawnRequest> Update(BattleContext context, float deltaTime);
    }
}
