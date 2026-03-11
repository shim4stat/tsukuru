using Game.Domain.Battle;
using Game.Infrastructure.Battle;
using Game.Infrastructure.MasterData.Assets;
using UnityEngine;

namespace Game.Presentation
{
    public class TestEntryPoint : MonoBehaviour
    {
        [SerializeField] private KeyInputManager keyInputManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerParamsAsset playerParamsAsset;

        private BattleContext battleContext;

        void Start()
        {
            string stageDir = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Scripts/Game/Domain/Battle");
            var repository = new StageMapRepository();
            var stageId = new StageId(1);
            var factory = new BattleEntityFactory(repository, stageDir);

            var playerStaticParams = playerParamsAsset != null
                ? new PlayerStaticParams(
                    playerParamsAsset.MaxHp,
                    playerParamsAsset.WalkSpeed,
                    playerParamsAsset.DashSpeed,
                    playerParamsAsset.DashDuration,
                    playerParamsAsset.DashCooldown,
                    playerParamsAsset.DashDeceleration)
                : new PlayerStaticParams(100, 5f, 10f, 0.5f, 2f, 1f);

            battleContext = new BattleContext(factory, playerStaticParams);
            battleContext.Setup(stageId);

            keyInputManager.Initialize(battleContext.Player, battleContext.Robot);
            playerController.Initialize(battleContext.Player);
        }
    }
}
