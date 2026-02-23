using System.Numerics;

namespace Game.Domain.Battle
{
    public class Player
    {
        public bool IsAlive() => true;
        public Vector3 Position;

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
