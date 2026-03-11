using System.IO;
using Game.Contracts.Battle;

namespace Game.Domain.Battle
{
    public class BattleEntityFactory : IBattleEntityFactory
    {
        private readonly IStageMapRepository _stageMapRepository;
        private readonly string _stageDir;

        public BattleEntityFactory(IStageMapRepository stageMapRepository, string stageDir)
        {
            _stageMapRepository = stageMapRepository;
            _stageDir = stageDir;
        }

        public Player CreatePlayer(PlayerStaticParams staticParams, BattleContext battleContext)
        {
            return new Player(staticParams, battleContext);
        }

        public Robot CreateRobot(StageId stageId)
        {
            string filepath = Path.Combine(_stageDir, $"stage_{stageId.Value}.json");
            var dto = _stageMapRepository.LoadStageMap(filepath);
            var robot = new Robot
            {
                StageMap = StageMap.CreateFromDto(dto)
            };
            return robot;
        }

        public Boss CreateBoss()
        {
            return new Boss();
        }

        public Enemy CreateEnemy()
        {
            return new Enemy();
        }
    }
}
