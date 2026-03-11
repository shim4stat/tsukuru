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

        public Player CreatePlayer(PlayerStaticParams staticParams)
        {
            return new Player(staticParams);
        }

        public Robot CreateRobot(StageId stageId)
        {
            string filepath = Path.Combine(_stageDir, $"stage_{stageId.Value}.json");
            var dto = _stageMapRepository.LoadStageMap(filepath);

            if (dto == null)
            {
                throw new InvalidDataException($"Failed to load stage map from '{filepath}': repository returned null.");
            }

            var stageMap = StageMap.CreateFromDto(dto);
            if (stageMap == null)
            {
                throw new InvalidDataException($"Failed to create StageMap from DTO for '{filepath}'.");
            }

            var robot = new Robot
            {
                StageMap = stageMap
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
