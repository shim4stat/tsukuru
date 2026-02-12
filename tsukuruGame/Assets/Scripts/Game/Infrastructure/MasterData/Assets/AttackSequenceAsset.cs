using UnityEngine;

namespace Game.Infrastructure.MasterData.Assets
{
    [CreateAssetMenu(menuName = "Game/MasterData/AttackSequence", fileName = "AttackSequence")]
    public sealed class AttackSequenceAsset : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private bool isSpecial;
        [SerializeField] private int energyCost;
        [SerializeField] private int specialEnergyCost;
        [SerializeField] private float phaseStartSeconds;
        [SerializeField] private float phaseAttackSeconds;
        [SerializeField] private float phaseEndSeconds;
        [SerializeField] private string robotBulletId = string.Empty;
        [SerializeField] private float dropMultiplier = 1.0f;

        public string Id => id;
        public bool IsSpecial => isSpecial;
        public int EnergyCost => energyCost;
        public int SpecialEnergyCost => specialEnergyCost;
        public float PhaseStartSeconds => phaseStartSeconds;
        public float PhaseAttackSeconds => phaseAttackSeconds;
        public float PhaseEndSeconds => phaseEndSeconds;
        public string RobotBulletId => robotBulletId;
        public float DropMultiplier => dropMultiplier;
    }
}
