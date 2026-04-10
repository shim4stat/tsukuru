using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Domain.Battle
{
    /// <summary>
    /// 基準方向を中心に扇状へ複数弾をばら撒くパターン。
    /// </summary>
    internal sealed class NWayShotPattern : IntervalBossAttackPatternBase
    {
        private readonly int _shotCount;
        private readonly float _totalSpreadDegrees;
        private readonly BossBulletPatternConfig _bulletConfig;

        public NWayShotPattern(
            float fireIntervalSeconds,
            int shotCount,
            float totalSpreadDegrees,
            BossBulletPatternConfig bulletConfig)
            : base(fireIntervalSeconds)
        {
            if (shotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(shotCount), "shotCount must be positive.");

            _shotCount = shotCount;
            _totalSpreadDegrees = totalSpreadDegrees;
            _bulletConfig = bulletConfig;
        }

        protected override void EmitShots(BattleContext context, List<EnemyBulletSpawnRequest> requests)
        {
            Vector3 baseDirection = _bulletConfig.FireDirection;
            if (_shotCount == 1)
            {
                requests.Add(_bulletConfig.CreateSpawnRequest(context));
                return;
            }

            // 全弾を基準方向に対して左右対称に配置する。
            float angleStep = _totalSpreadDegrees / (_shotCount - 1);
            float startAngle = -_totalSpreadDegrees * 0.5f;
            for (int i = 0; i < _shotCount; i++)
            {
                float angle = startAngle + (angleStep * i);
                Vector3 direction = RotateAroundZ(baseDirection, angle);
                requests.Add(_bulletConfig.CreateSpawnRequest(context, direction));
            }
        }

        private static Vector3 RotateAroundZ(Vector3 vector, float degrees)
        {
            float radians = degrees * (float)Math.PI / 180f;
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            return new Vector3(
                (vector.X * cos) - (vector.Y * sin),
                (vector.X * sin) + (vector.Y * cos),
                vector.Z);
        }
    }
}
