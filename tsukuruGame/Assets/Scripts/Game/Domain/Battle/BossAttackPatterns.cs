using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Domain.Battle
{
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
        private readonly EnemyBulletSpawnRequest _spawnRequest;

        public SingleShotPattern(float fireIntervalSeconds, EnemyBulletSpawnRequest spawnRequest)
            : base(fireIntervalSeconds)
        {
            _spawnRequest = spawnRequest;
        }

        protected override void EmitShots(BattleContext context, List<EnemyBulletSpawnRequest> requests)
        {
            requests.Add(_spawnRequest);
        }
    }

    internal sealed class NWayShotPattern : IntervalBossAttackPatternBase
    {
        private readonly int _shotCount;
        private readonly float _totalSpreadDegrees;
        private readonly Vector3 _origin;
        private readonly Vector3 _baseDirection;
        private readonly float _bulletSpeed;
        private readonly int _damage;
        private readonly float _lifetimeSeconds;
        private readonly int _absorbableEnergyAmount;
        private readonly EnemyBulletBehaviorType _behaviorType;

        public NWayShotPattern(
            float fireIntervalSeconds,
            int shotCount,
            float totalSpreadDegrees,
            Vector3 origin,
            Vector3 baseDirection,
            float bulletSpeed,
            int damage,
            float lifetimeSeconds,
            int absorbableEnergyAmount,
            EnemyBulletBehaviorType behaviorType)
            : base(fireIntervalSeconds)
        {
            if (shotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(shotCount), "shotCount must be positive.");
            if (bulletSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(bulletSpeed), "bulletSpeed must be positive.");

            _shotCount = shotCount;
            _totalSpreadDegrees = totalSpreadDegrees;
            _origin = origin;
            _baseDirection = NormalizeDirection(baseDirection);
            _bulletSpeed = bulletSpeed;
            _damage = damage;
            _lifetimeSeconds = lifetimeSeconds;
            _absorbableEnergyAmount = absorbableEnergyAmount;
            _behaviorType = behaviorType;
        }

        protected override void EmitShots(BattleContext context, List<EnemyBulletSpawnRequest> requests)
        {
            if (_shotCount == 1)
            {
                requests.Add(CreateSpawnRequest(_baseDirection));
                return;
            }

            float angleStep = _totalSpreadDegrees / (_shotCount - 1);
            float startAngle = -_totalSpreadDegrees * 0.5f;
            for (int i = 0; i < _shotCount; i++)
            {
                float angle = startAngle + (angleStep * i);
                Vector3 direction = RotateAroundZ(_baseDirection, angle);
                requests.Add(CreateSpawnRequest(direction));
            }
        }

        private EnemyBulletSpawnRequest CreateSpawnRequest(Vector3 direction)
        {
            return new EnemyBulletSpawnRequest(
                _origin,
                direction * _bulletSpeed,
                _damage,
                _lifetimeSeconds,
                _absorbableEnergyAmount,
                _behaviorType);
        }

        private static Vector3 NormalizeDirection(Vector3 direction)
        {
            if (direction.LengthSquared() <= 0f)
                throw new ArgumentOutOfRangeException(nameof(direction), "direction must be non-zero.");

            return Vector3.Normalize(direction);
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
        private readonly EnemyBulletSpawnRequest _spawnRequest;

        private float _cooldownRemaining;
        private int _remainingShotsInBurst;

        public BurstShotPattern(
            float burstIntervalSeconds,
            int shotsPerBurst,
            float shotIntervalSeconds,
            EnemyBulletSpawnRequest spawnRequest)
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
            _spawnRequest = spawnRequest;

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
                requests.Add(_spawnRequest);

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
