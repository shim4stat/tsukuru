using Game.Domain.Battle;
using UnityEngine;

namespace Game.Presentation
{
    public class TestEntryPoint : MonoBehaviour
    {
        [SerializeField] private KeyInputManager keyInputManager;
        [SerializeField] private PlayerController playerController;

        private Domain.Battle.PlayerMoveManager playerMoveManager;

        void Start()
        {
            // 仮の初期化コード。実際はBattleContextやStageMapを作成して渡す必要がある。
            var battleContext = new Domain.Battle.BattleContext();

            string stageJsonPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Scripts/Game/Domain/Battle/stage.json");
            var repository = new Game.Infrastructure.Battle.StageMapRepository();
            var stageMapDto = repository.LoadStageMap(stageJsonPath);
            var stageMap = Domain.Battle.StageMap.CreateFromDto(stageMapDto);

            // SetupでPlayerを生成してからManagerに渡す
            battleContext.Setup(new StageId(1), new BattleEntityFactory());

            playerMoveManager = new Domain.Battle.PlayerMoveManager(battleContext, stageMap);

            keyInputManager.Initialize(playerMoveManager);
            playerController.Initialize(battleContext.Player);
        }

        void Update()
        {
            if (playerMoveManager != null)
            {
                playerMoveManager.Update(Time.deltaTime);
            }
        }
    }
}
