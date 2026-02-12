using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Contracts.MasterData
{
    /// <summary>
    /// Read-only master data port.
    /// </summary>
    public interface IMasterDataRepository
    {
        StageDefinitionContract GetStage(string stageId);

        IReadOnlyList<StageDefinitionContract> GetAllStages();

        PlayerParamsContract GetPlayerParams();

        BossParamsContract GetBossParams(string bossId);

        AttackSequenceContract GetAttackSequence(string attackSequenceId);

        StorySequenceContract GetStory(string storyId);
    }
}
