using System;
using System.Collections.Generic;

namespace Game.Domain.Battle
{
    /// <summary>
    /// バースト間隔ごとに連射を開始し、バースト中は短い間隔で弾を撃ち続けるパターン。
    /// </summary>
    internal sealed class BurstShotPattern : IBossAttackPattern
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptyRequests = Array.Empty<EnemyBulletSpawnRequest>();

        private readonly float _burstIntervalSeconds;
        private readonly int _shotsPerBurst;
        private readonly float _shotIntervalSeconds;
        private readonly BossBulletPatternConfig _bulletConfig;

        private float _cooldownRemaining;
        private int _remainingShotsInBurst;

        public BurstShotPattern(
            float burstIntervalSeconds,
            int shotsPerBurst,
            float shotIntervalSeconds,
            BossBulletPatternConfig bulletConfig)
        {
            if (burstIntervalSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(burstIntervalSeconds), "burstIntervalSeconds must be positive.");
            if (shotsPerBurst <= 0)
                throw new ArgumentOutOfRangeException(nameof(shotsPerBurst), "shotsPerBurst must be positive.");
            if (shotIntervalSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(shotIntervalSeconds), "shotIntervalSeconds must be positive.");

            _burstIntervalSeconds = burstIntervalSeconds;
            _shotsPerBurst = shotsPerBurst;
            _shotIntervalSeconds = shotIntervalSeconds;
            _bulletConfig = bulletConfig;

            Reset();
        }

        public void Reset()
        {
            // 最初の 1 発も burstIntervalSeconds 経過後に発射される。
            _cooldownRemaining = _burstIntervalSeconds;
            _remainingShotsInBurst = 0;
        }

        public void Reset(float initialDelaySeconds)
        {
            _cooldownRemaining = initialDelaySeconds >= 0f ? initialDelaySeconds : _burstIntervalSeconds;
            _remainingShotsInBurst = 0;
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
                requests.Add(_bulletConfig.CreateSpawnRequest(context));

                if (_remainingShotsInBurst == 0)
                {
                    // この弾がバースト開始弾なので、残り連射数をセットする。
                    _remainingShotsInBurst = _shotsPerBurst - 1;
                }
                else
                {
                    _remainingShotsInBurst--;
                }

                _cooldownRemaining = _remainingShotsInBurst > 0 ? _shotIntervalSeconds : _burstIntervalSeconds;
            }

            return requests ?? EmptyRequests;
        }
    }
}
