using System.Numerics;

namespace Game.Domain.Battle
{
    public class PlayerStaticParams
    {
        // Default values are for debugging
        private readonly int _maxHp;
        private readonly float _walkSpeed;
        private readonly float _dashSpeed;
        private readonly float _dashDuration;
        private readonly float _dashCooldown;
        private readonly float _dashDeceleration;
        private readonly Vector2 _initialPosition;

        public int MaxHp => _maxHp;
        public float WalkSpeed => _walkSpeed;
        public float DashSpeed => _dashSpeed;
        public float DashDuration => _dashDuration;
        public float DashCooldown => _dashCooldown;
        public float DashDeceleration => _dashDeceleration;
        public Vector2 InitialPosition => _initialPosition;

        // Parameterless constructor keeps the original debug defaults.
        public PlayerStaticParams()
            : this(100, 5f, 10f, 0.5f, 2f, 0f, new Vector2(1, 1))
        {
        }

        // Constructor matching the original signature; keeps default initial position.
        public PlayerStaticParams(int maxHp, float walkSpeed, float dashSpeed, float dashDuration, float dashCooldown, float dashDeceleration)
            : this(maxHp, walkSpeed, dashSpeed, dashDuration, dashCooldown, dashDeceleration, new Vector2(1, 1))
        {
        }

        // Primary constructor that allows specifying all values explicitly.
        public PlayerStaticParams(int maxHp, float walkSpeed, float dashSpeed, float dashDuration, float dashCooldown, float dashDeceleration, Vector2 initialPosition)
        {
            _maxHp = maxHp;
            _walkSpeed = walkSpeed;
            _dashSpeed = dashSpeed;
            _dashDuration = dashDuration;
            _dashCooldown = dashCooldown;
            _dashDeceleration = dashDeceleration;
            _initialPosition = initialPosition;
        }
    }
}
