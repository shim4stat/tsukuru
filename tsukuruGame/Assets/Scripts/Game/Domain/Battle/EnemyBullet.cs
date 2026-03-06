using System;

namespace Game.Domain.Battle
{
    public class EnemyBullet
    {
        private float _lifetimeRemaining;
        private bool _isInitialized;

        public int Damage { get; private set; }

        public int AbsorbableEnergyAmount { get; private set; }

        public float LifetimeRemaining
        {
            get
            {
                EnsureInitialized();
                return _lifetimeRemaining;
            }
        }

        public bool IsVanished
        {
            get
            {
                EnsureInitialized();
                return _lifetimeRemaining <= 0f;
            }
        }

        public void Initialize(int damage, float lifetimeSeconds, int absorbableEnergyAmount)
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

            Damage = damage;
            AbsorbableEnergyAmount = absorbableEnergyAmount;
            _lifetimeRemaining = lifetimeSeconds;
            _isInitialized = true;
        }

        public void Tick(float deltaTime)
        {
            EnsureInitialized();

            if (deltaTime <= 0f || IsVanished)
                return;

            _lifetimeRemaining -= deltaTime;
            if (_lifetimeRemaining <= 0f)
                _lifetimeRemaining = 0f;
        }

        public void Vanish()
        {
            EnsureInitialized();

            if (IsVanished)
                return;

            _lifetimeRemaining = 0f;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("EnemyBullet is not initialized.");
        }
    }
}
