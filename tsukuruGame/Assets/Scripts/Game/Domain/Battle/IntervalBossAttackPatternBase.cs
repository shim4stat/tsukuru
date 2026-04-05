using System;
using System.Collections.Generic;

namespace Game.Domain.Battle
{
    /// <summary>
    /// 一定間隔で弾を発射するパターンの基底クラス。
    /// deltaTime が大きいフレームでも、必要回数ぶん発射を取りこぼさない。
    /// </summary>
    internal abstract class IntervalBossAttackPatternBase : IBossAttackPattern
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptyRequests = Array.Empty<EnemyBulletSpawnRequest>();

        private readonly float _fireIntervalSeconds;
        private float _cooldownRemaining;

        protected IntervalBossAttackPatternBase(float fireIntervalSeconds)
        {
            if (fireIntervalSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fireIntervalSeconds), "fireIntervalSeconds must be positive.");

            _fireIntervalSeconds = fireIntervalSeconds;
            _cooldownRemaining = fireIntervalSeconds;
        }

        public virtual void Reset()
        {
            _cooldownRemaining = _fireIntervalSeconds;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> Update(BattleContext context, float deltaTime)
        {
            if (deltaTime <= 0f)
                return EmptyRequests;

            List<EnemyBulletSpawnRequest> requests = null;
            float remainingTime = deltaTime;
            while (remainingTime > 0f)
            {
                if (_cooldownRemaining > remainingTime)
                {
                    _cooldownRemaining -= remainingTime;
                    break;
                }

                remainingTime -= _cooldownRemaining;
                requests ??= new List<EnemyBulletSpawnRequest>();
                // 1 フレーム内に複数回の発射タイミングが含まれる場合を考慮してループで処理する。
                EmitShots(context, requests);
                _cooldownRemaining = _fireIntervalSeconds;
            }

            return requests ?? EmptyRequests;
        }

        protected abstract void EmitShots(BattleContext context, List<EnemyBulletSpawnRequest> requests);
    }
}
