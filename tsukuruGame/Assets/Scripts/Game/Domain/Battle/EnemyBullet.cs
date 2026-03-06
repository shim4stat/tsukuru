using System;
using System.Numerics;

namespace Game.Domain.Battle
{
    public class EnemyBullet
    {
        private float _lifetimeRemaining;
        private float _elapsedSeconds;
        private bool _isInitialized;
        private Vector3 _position;
        private Vector3 _velocity;
        private EnemyBulletBehaviorType _behaviorType;

        public int Damage { get; private set; }

        public int AbsorbableEnergyAmount { get; private set; }

        public Vector3 Position
        {
            get
            {
                EnsureInitialized();
                return _position;
            }
        }

        public Vector3 Velocity
        {
            get
            {
                EnsureInitialized();
                return _velocity;
            }
        }

        public EnemyBulletBehaviorType BehaviorType
        {
            get
            {
                EnsureInitialized();
                return _behaviorType;
            }
        }

        public float LifetimeRemaining
        {
            get
            {
                EnsureInitialized();
                return _lifetimeRemaining;
            }
        }

        public float ElapsedSeconds
        {
            get
            {
                EnsureInitialized();
                return _elapsedSeconds;
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

        public void Initialize(EnemyBulletSpawnRequest spawnRequest)
        {
            Damage = spawnRequest.Damage;
            AbsorbableEnergyAmount = spawnRequest.AbsorbableEnergyAmount;
            _position = spawnRequest.Position;
            _velocity = spawnRequest.Velocity;
            _behaviorType = spawnRequest.BehaviorType;
            _elapsedSeconds = 0f;
            _lifetimeRemaining = spawnRequest.LifetimeSeconds;
            _isInitialized = true;
        }

        public void Tick(float deltaTime)
        {
            EnsureInitialized();

            if (deltaTime <= 0f || IsVanished)
                return;

            _elapsedSeconds += deltaTime;
            _lifetimeRemaining -= deltaTime;
            if (_lifetimeRemaining <= 0f)
                _lifetimeRemaining = 0f;
        }

        public void Translate(Vector3 delta)
        {
            EnsureInitialized();

            if (IsVanished)
                return;

            _position += delta;
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
