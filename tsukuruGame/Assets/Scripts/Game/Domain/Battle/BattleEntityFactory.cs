using System;
using System.Collections.Generic;
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

            var robot = new Robot(stageMap);

            if (dto.ItemPlacements != null)
            {
                var itemInstances = new List<ItemInstance>(dto.ItemPlacements.Count);
                foreach (var placement in dto.ItemPlacements)
                {
                    if (!Enum.IsDefined(typeof(ItemType), placement.ItemType))
                        throw new ArgumentException($"Unknown ItemType value: {placement.ItemType}", nameof(placement.ItemType));
                    var itemType = (ItemType)placement.ItemType;
                    var definition = new ItemDefinition(itemType, placement.BaseEnergyAmount);
                    itemInstances.Add(new ItemInstance(placement.CellX, placement.CellY, definition, placement.MergedCount));
                }
                robot.PlaceItems(itemInstances);
            }

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

        public EnemyBullet CreateEnemyBullet()
        {
            return new EnemyBullet();
        }
    }
}
