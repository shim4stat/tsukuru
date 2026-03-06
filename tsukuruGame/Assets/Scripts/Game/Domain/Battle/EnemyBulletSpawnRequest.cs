using System;
using System.Numerics;

namespace Game.Domain.Battle
{
    public readonly struct EnemyBulletSpawnRequest
    {
        public EnemyBulletSpawnRequest(
            Vector3 position,
            Vector3 velocity,
            int damage,
            float lifetimeSeconds,
            int absorbableEnergyAmount,
            EnemyBulletBehaviorType behaviorType)
        {
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

            Position = position;
            Velocity = velocity;
            Damage = damage;
            LifetimeSeconds = lifetimeSeconds;
            AbsorbableEnergyAmount = absorbableEnergyAmount;
            BehaviorType = behaviorType;
        }

        public Vector3 Position { get; }

        public Vector3 Velocity { get; }

        public int Damage { get; }

        public float LifetimeSeconds { get; }

        public int AbsorbableEnergyAmount { get; }

        public EnemyBulletBehaviorType BehaviorType { get; }
    }
}
