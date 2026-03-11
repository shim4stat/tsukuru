using System;
using System.Numerics;
namespace Game.Domain.Battle

{
    public class Player
    {
        public bool IsAlive() => true;
        public Vector3 Position;

        public PlayerStaticParams StaticParams;
        public PlayerMoveManager MoveManager;

        public bool IsDashing { get; set; }
        public float DashTimeRemaining { get; set; }
        public float DashCooldownRemaining { get; set; }

        public Player(PlayerStaticParams staticParams)
        {
            StaticParams = staticParams;
            MoveManager = new PlayerMoveManager(this);
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
