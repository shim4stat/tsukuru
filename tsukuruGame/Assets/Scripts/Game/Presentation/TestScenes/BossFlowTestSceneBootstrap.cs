using System;
using System.Collections.Generic;
using Game.Application.Flow;
using Game.Application.Option;
using Game.Contracts.MasterData.Models;
using Game.Domain.GameSession;
using Game.Infrastructure.MasterData;
using Game.Infrastructure.MasterData.Assets;
using Game.Presentation.Common;
using Game.Presentation.TestScenes.Bootstrap;
using UnityEngine;

namespace Game.Presentation.TestScenes
{
    /// <summary>
    /// ボス演出確認用テストシーンで、GameServices を最小構成で初期化する。
    /// </summary>
    public sealed class BossFlowTestSceneBootstrap : MonoBehaviour
    {
        [Header("MasterData")]
        [SerializeField] private List<StageDefinitionAsset> stageDefinitions = new List<StageDefinitionAsset>();
        [SerializeField] private PlayerParamsAsset playerParams;
        [SerializeField] private List<BossParamsAsset> bossParams = new List<BossParamsAsset>();
        [SerializeField] private List<AttackSequenceAsset> attackSequences = new List<AttackSequenceAsset>();
        [SerializeField] private List<StorySequenceAsset> storySequences = new List<StorySequenceAsset>();

        [Header("Runtime")]
        [SerializeField] private string stageId = "stage_01";

        public NoOpSceneLoader SceneLoader { get; private set; }

        public InMemorySaveRepository SaveRepository { get; private set; }

        public NoOpSettingsApplier SettingsApplier { get; private set; }

        private void Awake()
        {
            ValidateSerializedFields();

            ScriptableObjectMasterDataRepository masterDataRepository = new ScriptableObjectMasterDataRepository(
                stageDefinitions,
                playerParams,
                bossParams,
                attackSequences,
                storySequences);

            StageDefinitionContract stage = masterDataRepository.GetStage(stageId);
            GameSession session = new GameSession();
            session.EnterInGame(stageId, stage.HasIntroStory);

            SceneLoader = new NoOpSceneLoader();
            SaveRepository = new InMemorySaveRepository();
            SettingsApplier = new NoOpSettingsApplier();

            GameFlowUseCase flowUseCase = new GameFlowUseCase(session, masterDataRepository, SceneLoader);
            OptionUseCase optionUseCase = new OptionUseCase(SaveRepository, SettingsApplier);
            GameServices services = new GameServices(
                flowUseCase,
                optionUseCase,
                session,
                SaveRepository,
                SettingsApplier,
                masterDataRepository);

            GameServicesLocator.Set(services);
            Debug.Log($"BossFlowTestSceneBootstrap initialized. stageId={stageId}");
        }

        private void ValidateSerializedFields()
        {
            ValidateAssetList(stageDefinitions, nameof(stageDefinitions), allowEmpty: false);
            ValidateAsset(playerParams, nameof(playerParams));
            ValidateAssetList(bossParams, nameof(bossParams), allowEmpty: false);
            ValidateAssetList(attackSequences, nameof(attackSequences), allowEmpty: true);
            ValidateAssetList(storySequences, nameof(storySequences), allowEmpty: true);

            if (string.IsNullOrWhiteSpace(stageId))
                throw new InvalidOperationException("BossFlowTestSceneBootstrap.stageId is null or empty.");

            EnsureStageExists(stageId);
        }

        private void EnsureStageExists(string targetStageId)
        {
            for (int i = 0; i < stageDefinitions.Count; i++)
            {
                StageDefinitionAsset stage = stageDefinitions[i];
                if (stage != null && string.Equals(stage.Id, targetStageId, StringComparison.Ordinal))
                    return;
            }

            throw new InvalidOperationException($"Stage id is not found in stageDefinitions: {targetStageId}");
        }

        private static void ValidateAsset(UnityEngine.Object asset, string name)
        {
            if (asset == null)
                throw new InvalidOperationException($"BossFlowTestSceneBootstrap required asset is null: {name}");
        }

        private static void ValidateAssetList<T>(IReadOnlyList<T> assets, string name, bool allowEmpty) where T : UnityEngine.Object
        {
            if (assets == null)
                throw new InvalidOperationException($"BossFlowTestSceneBootstrap required list is null: {name}");

            if (!allowEmpty && assets.Count == 0)
                throw new InvalidOperationException($"BossFlowTestSceneBootstrap required list is empty: {name}");

            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] == null)
                    throw new InvalidOperationException($"BossFlowTestSceneBootstrap list contains null asset: {name}[{i}]");
            }
        }
    }
}
