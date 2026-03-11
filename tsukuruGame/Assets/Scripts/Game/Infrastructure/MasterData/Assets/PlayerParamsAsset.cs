using UnityEngine;

namespace Game.Infrastructure.MasterData.Assets
{
    [CreateAssetMenu(menuName = "Game/MasterData/PlayerParams", fileName = "PlayerParams")]
    public sealed class PlayerParamsAsset : ScriptableObject
    {
        [SerializeField] private int maxHp;
        [SerializeField] private int maxEnergy;
        [SerializeField] private int maxSpecialEnergy;
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float dashSpeed = 10f;
        [SerializeField] private float dashDuration = 0.5f;
        [SerializeField] private float dashCooldown = 2f;
        [SerializeField] private float dashDeceleration = 0f;

        public int MaxHp => maxHp;
        public int MaxEnergy => maxEnergy;
        public int MaxSpecialEnergy => maxSpecialEnergy;
        public float WalkSpeed => walkSpeed;
        public float DashSpeed => dashSpeed;
        public float DashDuration => dashDuration;
        public float DashCooldown => dashCooldown;
        public float DashDeceleration => dashDeceleration;
    }
}
