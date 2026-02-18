using System;
using System.Collections.Generic;

namespace Game.Contracts.MasterData.Models
{
    /// <summary>
    /// Static boss parameters needed by battle logic.
    /// </summary>
    public sealed class BossParamsContract
    {
        public string Id { get; set; } = string.Empty;

        public IReadOnlyList<int> GaugeMaxHps { get; set; } = Array.Empty<int>();

        public int BaseDropEnergyAmount { get; set; }

        public float MinDropIntervalSeconds { get; set; }
    }
}
