using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Domain.Battle
{
    internal readonly struct BossBulletPatternConfig
    {
        private readonly Vector3 _spawnOffset;
        private readonly Vector3 _fireDirection;
        private readonly float _bulletSpeed;
        private readonly int _damage;
        private readonly float _lifetimeSeconds;
        private readonly int _absorbableEnergyAmount;
        private readonly EnemyBulletBehaviorType _behaviorType;

        public BossBulletPatternConfig(
            Vector3 spawnOffset,
            Vector3 fireDirection,
            float bulletSpeed,
            int damage,
            float lifetimeSeconds,
            int absorbableEnergyAmount,
            EnemyBulletBehaviorType behaviorType)
        {
            if (fireDirection.LengthSquared() <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fireDirection), "fireDirection must be non-zero.");
            if (bulletSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(bulletSpeed), "bulletSpeed must be positive.");
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "damage must be non-negative.");
            if (lifetimeSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(lifetimeSeconds), "lifetimeSeconds must be positive.");
            if (absorbableEnergyAmount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(absorbableEnergyAmount),
                    "absorbableEnergyAmount must be non-negative.");
            }

            _spawnOffset = spawnOffset;
            _fireDirection = Vector3.Normalize(fireDirection);
            _bulletSpeed = bulletSpeed;
            _damage = damage;
            _lifetimeSeconds = lifetimeSeconds;
            _absorbableEnergyAmount = absorbableEnergyAmount;
            _behaviorType = behaviorType;
        }

        public Vector3 FireDirection => _fireDirection;

        public EnemyBulletSpawnRequest CreateSpawnRequest(BattleContext context)
        {
            return CreateSpawnRequest(context, _fireDirection);
        }

        public EnemyBulletSpawnRequest CreateSpawnRequest(BattleContext context, Vector3 fireDirection)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Boss == null)
                throw new InvalidOperationException("BattleContext.Boss is not initialized.");
            if (fireDirection.LengthSquared() <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fireDirection), "fireDirection must be non-zero.");

            Vector3 normalizedDirection = Vector3.Normalize(fireDirection);
            return new EnemyBulletSpawnRequest(
                context.Boss.Position + _spawnOffset,
                normalizedDirection * _bulletSpeed,
                _damage,
                _lifetimeSeconds,
                _absorbableEnergyAmount,
                _behaviorType);
        }
    }

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
                EmitShots(context, requests);
                _cooldownRemaining = _fireIntervalSeconds;
            }

            return requests ?? EmptyRequests;
        }

        protected abstract void EmitShots(BattleContext context, List<EnemyBulletSpawnRequest> requests);
    }

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
            _cooldownRemaining = _burstIntervalSeconds;
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
