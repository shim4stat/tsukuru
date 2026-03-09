using System;
using System.Numerics;

namespace Game.Domain.Battle
{
    public class Player
    {
        private bool _hasInitializedStats;

        public Vector3 Position;
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public bool HasInitializedStats => _hasInitializedStats;

        public bool IsAlive()
        {
            return !_hasInitializedStats || CurrentHp > 0;
        }

        public void InitializeStats(int maxHp)
        {
            if (maxHp <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHp), "maxHp must be positive.");

            MaxHp = maxHp;
            CurrentHp = maxHp;
            _hasInitializedStats = true;
        }

        public bool ApplyDamage(int damage)
        {
            if (!_hasInitializedStats)
                throw new InvalidOperationException("Player stats are not initialized.");
            if (damage <= 0 || CurrentHp <= 0)
                return false;

            CurrentHp = Math.Max(0, CurrentHp - damage);
            return true;
        }

        // ダッシュ関連のプロパティ
        public bool IsDashing { get; set; }
        public float DashTimeRemaining { get; set; }
        public float DashCooldownRemaining { get; set; }
        public float DashSpeed { get; set; } = 10f;
        public float WalkSpeed { get; set; } = 5f;
        public float DashDuration { get; set; } = 0.5f;
        public float DashCooldown { get; set; } = 2f;
    }
}
