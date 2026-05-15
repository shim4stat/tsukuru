using System;
using System.Numerics;
namespace Game.Domain.Battle

{
    public class Player
    {
        public bool IsAlive() => true;
        public Vector3 Position;

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
