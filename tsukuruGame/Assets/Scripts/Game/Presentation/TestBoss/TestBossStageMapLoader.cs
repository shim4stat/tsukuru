using System;
using System.IO;
using Game.Domain.Battle;
using Game.Infrastructure.Battle;
using UnityEngine;

namespace Game.Presentation.TestBoss
{
    internal static class TestBossStageMapLoader
    {
        public static StageMap LoadDefault()
        {
            string stageJsonPath = Path.Combine(UnityEngine.Application.dataPath, "Scripts/Game/Domain/Battle/stage.json");
            StageMapRepository repository = new StageMapRepository();
            StageMap stageMap = StageMap.CreateFromDto(repository.LoadStageMap(stageJsonPath));
            if (stageMap == null)
                throw new InvalidOperationException($"Failed to load test boss stage map: {stageJsonPath}");

            return stageMap;
        }
    }
}
