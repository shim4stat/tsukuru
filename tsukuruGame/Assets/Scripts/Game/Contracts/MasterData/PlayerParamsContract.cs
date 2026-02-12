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
    }
}
