using UnityEngine;

namespace Game.Infrastructure.MasterData.Assets
{
    [CreateAssetMenu(menuName = "Game/MasterData/PlayerParams", fileName = "PlayerParams")]
    public sealed class PlayerParamsAsset : ScriptableObject
    {
        [SerializeField] private int maxHp;
        [SerializeField] private int maxEnergy;
        [SerializeField] private int maxSpecialEnergy;

        public int MaxHp => maxHp;
        public int MaxEnergy => maxEnergy;
        public int MaxSpecialEnergy => maxSpecialEnergy;
    }
}
