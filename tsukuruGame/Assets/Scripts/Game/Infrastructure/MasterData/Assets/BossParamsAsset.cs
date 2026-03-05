using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.MasterData.Assets
{
    [CreateAssetMenu(menuName = "Game/MasterData/BossParams", fileName = "BossParams")]
    public sealed class BossParamsAsset : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private List<int> gaugeMaxHps = new List<int>();
        [SerializeField] private int baseDropEnergyAmount;
        [SerializeField] private float minDropIntervalSeconds;
        [SerializeField] private float actionIntervalSeconds = 1.0f;

        public string Id => id;
        public IReadOnlyList<int> GaugeMaxHps => gaugeMaxHps;
        public int BaseDropEnergyAmount => baseDropEnergyAmount;
        public float MinDropIntervalSeconds => minDropIntervalSeconds;
        public float ActionIntervalSeconds => actionIntervalSeconds;
    }
}
