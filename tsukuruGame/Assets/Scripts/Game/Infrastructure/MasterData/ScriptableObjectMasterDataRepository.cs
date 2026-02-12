using System;
using System.Collections.Generic;
using Game.Contracts.MasterData;
using Game.Contracts.MasterData.Models;
using Game.Infrastructure.MasterData.Assets;
using Game.Infrastructure.MasterData.Mapping;
using Game.Infrastructure.MasterData.Validation;

namespace Game.Infrastructure.MasterData
{
    public sealed class ScriptableObjectMasterDataRepository : IMasterDataRepository
    {
        private readonly Dictionary<string, StageDefinitionContract> _stagesById;
        private readonly List<StageDefinitionContract> _orderedStages;
        private readonly PlayerParamsContract _playerParams;
        private readonly Dictionary<string, BossParamsContract> _bossById;
        private readonly Dictionary<string, AttackSequenceContract> _attackSequenceById;
        private readonly Dictionary<string, StorySequenceContract> _storyById;

        public ScriptableObjectMasterDataRepository(
            IReadOnlyList<StageDefinitionAsset> stageAssets,
            PlayerParamsAsset playerParamsAsset,
            IReadOnlyList<BossParamsAsset> bossAssets,
            IReadOnlyList<AttackSequenceAsset> attackSequenceAssets,
            IReadOnlyList<StorySequenceAsset> storyAssets)
        {
            MasterDataValidator.ValidateUniqueIds(stageAssets, nameof(stageAssets));
            MasterDataValidator.ValidateNotNull(playerParamsAsset, nameof(playerParamsAsset));
            MasterDataValidator.ValidateUniqueIds(bossAssets, nameof(bossAssets));
            MasterDataValidator.ValidateUniqueIds(attackSequenceAssets, nameof(attackSequenceAssets));
            MasterDataValidator.ValidateUniqueIds(storyAssets, nameof(storyAssets));

            _stagesById = new Dictionary<string, StageDefinitionContract>(StringComparer.Ordinal);
            _orderedStages = new List<StageDefinitionContract>(stageAssets.Count);
            for (int i = 0; i < stageAssets.Count; i++)
            {
                StageDefinitionContract contract = MasterDataMapper.ToContract(stageAssets[i]);
                _stagesById.Add(contract.Id, contract);
                _orderedStages.Add(contract);
            }

            _orderedStages.Sort(CompareStageOrder);
            _playerParams = MasterDataMapper.ToContract(playerParamsAsset);

            _bossById = new Dictionary<string, BossParamsContract>(StringComparer.Ordinal);
            for (int i = 0; i < bossAssets.Count; i++)
            {
                BossParamsContract contract = MasterDataMapper.ToContract(bossAssets[i]);
                _bossById.Add(contract.Id, contract);
            }

            _attackSequenceById = new Dictionary<string, AttackSequenceContract>(StringComparer.Ordinal);
            for (int i = 0; i < attackSequenceAssets.Count; i++)
            {
                AttackSequenceContract contract = MasterDataMapper.ToContract(attackSequenceAssets[i]);
                _attackSequenceById.Add(contract.Id, contract);
            }

            _storyById = new Dictionary<string, StorySequenceContract>(StringComparer.Ordinal);
            for (int i = 0; i < storyAssets.Count; i++)
            {
                StorySequenceContract contract = MasterDataMapper.ToContract(storyAssets[i]);
                _storyById.Add(contract.Id, contract);
            }
        }

        public StageDefinitionContract GetStage(string stageId)
        {
            EnsureId(stageId, nameof(stageId));
            if (!_stagesById.TryGetValue(stageId, out StageDefinitionContract stage))
                throw new KeyNotFoundException($"Stage master data not found: {stageId}");

            return stage;
        }

        public IReadOnlyList<StageDefinitionContract> GetAllStages()
        {
            return _orderedStages;
        }

        public PlayerParamsContract GetPlayerParams()
        {
            return _playerParams;
        }

        public BossParamsContract GetBossParams(string bossId)
        {
            EnsureId(bossId, nameof(bossId));
            if (!_bossById.TryGetValue(bossId, out BossParamsContract boss))
                throw new KeyNotFoundException($"Boss master data not found: {bossId}");

            return boss;
        }

        public AttackSequenceContract GetAttackSequence(string attackSequenceId)
        {
            EnsureId(attackSequenceId, nameof(attackSequenceId));
            if (!_attackSequenceById.TryGetValue(attackSequenceId, out AttackSequenceContract sequence))
                throw new KeyNotFoundException($"AttackSequence master data not found: {attackSequenceId}");

            return sequence;
        }

        public StorySequenceContract GetStory(string storyId)
        {
            EnsureId(storyId, nameof(storyId));
            if (!_storyById.TryGetValue(storyId, out StorySequenceContract story))
                throw new KeyNotFoundException($"Story master data not found: {storyId}");

            return story;
        }

        private static void EnsureId(string id, string paramName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id is null or empty.", paramName);
        }

        private static int CompareStageOrder(StageDefinitionContract x, StageDefinitionContract y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            int byOrder = x.OrderIndex.CompareTo(y.OrderIndex);
            if (byOrder != 0)
                return byOrder;

            return string.CompareOrdinal(x.Id, y.Id);
        }
    }
}
