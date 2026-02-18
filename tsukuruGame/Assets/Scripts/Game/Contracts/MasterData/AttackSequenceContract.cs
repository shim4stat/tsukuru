namespace Game.Contracts.MasterData.Models
{
    /// <summary>
    /// Robot attack sequence definition used by battle flow.
    /// </summary>
    public sealed class AttackSequenceContract
    {
        public string Id { get; set; } = string.Empty;

        public bool IsSpecial { get; set; }

        public int EnergyCost { get; set; }

        public int SpecialEnergyCost { get; set; }

        public float PhaseStartSeconds { get; set; }

        public float PhaseAttackSeconds { get; set; }

        public float PhaseEndSeconds { get; set; }

        public string RobotBulletId { get; set; } = string.Empty;

        public float DropMultiplier { get; set; }
    }
}
