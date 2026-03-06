using System;

namespace Game.Domain.Battle
{
    public readonly struct EnemyBulletSpawnParams
    {
        public EnemyBulletSpawnParams(int count, int damage, float lifetimeSeconds, int absorbableEnergyAmount)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "count must be positive.");
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

            Count = count;
            Damage = damage;
            LifetimeSeconds = lifetimeSeconds;
            AbsorbableEnergyAmount = absorbableEnergyAmount;
        }

        public int Count { get; }

        public int Damage { get; }

        public float LifetimeSeconds { get; }

        public int AbsorbableEnergyAmount { get; }
    }
}
