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

        public readonly PlayerStaticParams StaticParams;
        private readonly PlayerMoveManager MoveManager;

        public bool IsDashing { get; set; }
        public float DashTimeRemaining { get; set; }
        public float DashCooldownRemaining { get; set; }

        public int CurrentEnergy { get; private set; }
        public int MaxEnergy => StaticParams.MaxEnergy;

        public Player(PlayerStaticParams staticParams)
        {
            StaticParams = staticParams;
            MoveManager = new PlayerMoveManager(this);
            CurrentEnergy = 0;
        }

        public void AddEnergy(int amount)
        {
            if (amount <= 0) return;
            CurrentEnergy = Math.Min(CurrentEnergy + amount, MaxEnergy);
        }

        public void InputDash()
        {
            MoveManager.SetInputDash();
        }
        public void Move(Vector2 inputDir, Robot robot, float deltaTime)
        {
            MoveManager.SetInputDirection(inputDir);
            MoveManager.Update(robot, deltaTime);
        }
    }
}
