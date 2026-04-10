using System.Collections.Generic;

namespace Game.Domain.Battle
{
    /// <summary>
    /// 発射間隔ごとに 1 発だけ撃つ最も単純なパターン。
    /// </summary>
    internal sealed class SingleShotPattern : IntervalBossAttackPatternBase
    {
        private readonly BossBulletPatternConfig _bulletConfig;

        public SingleShotPattern(float fireIntervalSeconds, BossBulletPatternConfig bulletConfig)
            : base(fireIntervalSeconds)
        {
            _bulletConfig = bulletConfig;
        }

        protected override void EmitShots(BattleContext context, List<EnemyBulletSpawnRequest> requests)
        {
            requests.Add(_bulletConfig.CreateSpawnRequest(context));
        }
    }
}
