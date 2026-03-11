using System.Numerics;

namespace Game.Domain.Battle
{
    public class PlayerStaticParams
    {
        // Default values are for debugging
        public int maxHp = 100;
        public float walkSpeed = 5f;
        public float dashSpeed = 10f;
        public float dashDuration = 0.5f;
        public float dashCooldown = 2f;
        public float dashDeceleration = 0f;
        public Vector2 initialPosition = new Vector2(1, 1);

        public PlayerStaticParams(int maxHp, float walkSpeed, float dashSpeed, float dashDuration, float dashCooldown, float dashDeceleration)
        {
            this.maxHp = maxHp;
            this.walkSpeed = walkSpeed;
            this.dashSpeed = dashSpeed;
            this.dashDuration = dashDuration;
            this.dashCooldown = dashCooldown;
            this.dashDeceleration = dashDeceleration;
        }
    }
}
