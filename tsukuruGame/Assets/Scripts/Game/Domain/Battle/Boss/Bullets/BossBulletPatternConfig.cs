using System;
using System.Numerics;

namespace Game.Domain.Battle
{
    /// <summary>
    /// ボス弾生成に必要な共通パラメータをまとめた設定値。
    /// 各パターンはこの設定を使って SpawnRequest を組み立てる。
    /// </summary>
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
            // 以降のパターン実装では正規化済み方向を前提に扱う。
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
            // 発射位置は常に現在のボス位置を基準にした相対オフセットで決める。
            return new EnemyBulletSpawnRequest(
                context.Boss.Position + _spawnOffset,
                normalizedDirection * _bulletSpeed,
                _damage,
                _lifetimeSeconds,
                _absorbableEnergyAmount,
                _behaviorType);
        }
    }
}
