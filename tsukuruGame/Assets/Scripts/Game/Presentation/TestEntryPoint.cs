using System.Collections.Generic;
using Game.Domain.Battle;
using Game.Infrastructure.Battle;
using Game.Infrastructure.MasterData.Assets;
using Game.Presentation.Game.Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Presentation
{
    public class TestEntryPoint : MonoBehaviour
    {
        [SerializeField] private KeyInputManager keyInputManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerParamsAsset playerParamsAsset;
        [SerializeField] private Transform itemParent;
        [SerializeField] private GameObject healthPotionPrefab;
        [SerializeField, FormerlySerializedAs("itemPrefab")] private GameObject energyPotionPrefab;

        private BattleContext battleContext;
        private ItemPresenter _itemPresenter;

        void Start()
        {
            string stageDir = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Scripts/Game/Domain/Battle");
            var repository = new StageMapRepository();
            var stageId = new StageId(1);
            var factory = new BattleEntityFactory(repository, stageDir);

            var playerStaticParams = playerParamsAsset != null
                ? new PlayerStaticParams(
                    playerParamsAsset.MaxHp,
                    playerParamsAsset.MaxEnergy,
                    playerParamsAsset.WalkSpeed,
                    playerParamsAsset.DashSpeed,
                    playerParamsAsset.DashDuration,
                    playerParamsAsset.DashCooldown,
                    playerParamsAsset.DashDeceleration)
                : new PlayerStaticParams(100, 100, 5f, 10f, 0.5f, 2f, 1f);

            battleContext = new BattleContext(factory, playerStaticParams);
            battleContext.Setup(stageId);

            keyInputManager.Initialize(battleContext.Player, battleContext.Robot);
            playerController.Initialize(battleContext.Player);

            SpawnBattleItems();
        }

        private void SpawnBattleItems()
        {
            if (itemParent == null)
            {
                Debug.LogWarning("itemParent is not assigned. Skipping item spawn.");
                return;
            }

            var prefabMap = new Dictionary<ItemType, GameObject>
            {
                { ItemType.HealthPotion, healthPotionPrefab },
                { ItemType.EnergyPotion, energyPotionPrefab },
            };
            var energyPickupService = new EnergyPickupService();
            _itemPresenter = new ItemPresenter(itemParent, prefabMap, energyPickupService, battleContext);
            _itemPresenter.SpawnItems(battleContext.Items);
        }

        private void OnDestroy()
        {
            if (_itemPresenter != null)
            {
                _itemPresenter.Dispose();
                _itemPresenter = null;
            }
        }
    }
}
