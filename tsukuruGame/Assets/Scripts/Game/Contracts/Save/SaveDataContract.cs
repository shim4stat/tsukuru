using System.Collections.Generic;
using Game.Contracts.Settings.Models;

namespace Game.Contracts.Save.Models
{
    /// <summary>
    /// Serializable save contract shared across layers.
    /// </summary>
    public sealed class SaveDataContract
    {
        public int Version { get; set; }

        public List<StageProgressContract> Stages { get; set; } = new List<StageProgressContract>();

        public GameSettingsContract Settings { get; set; } = new GameSettingsContract();
    }

    public sealed class StageProgressContract
    {
        public string StageId { get; set; } = string.Empty;

        public bool Unlocked { get; set; }

        public bool Cleared { get; set; }

        public string BestRank { get; set; } = string.Empty;
    }
}
