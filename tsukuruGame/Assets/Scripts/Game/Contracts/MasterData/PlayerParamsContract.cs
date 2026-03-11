namespace Game.Contracts.MasterData.Models
{
    /// <summary>
    /// Static player parameters needed by domain logic.
    /// </summary>
    public sealed class PlayerParamsContract
    {
        public int MaxHp { get; set; }

        public int MaxEnergy { get; set; }

        public int MaxSpecialEnergy { get; set; }

        public float WalkSpeed { get; set; }

        public float DashSpeed { get; set; }

        public float DashDuration { get; set; }

        public float DashCooldown { get; set; }

        public float DashDeceleration { get; set; }
    }
}
