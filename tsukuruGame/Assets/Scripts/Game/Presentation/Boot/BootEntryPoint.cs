using System;
using System.Collections.Generic;
using Game.Application.Flow;
using Game.Application.Option;
using Game.Contracts.Flow;
using Game.Contracts.MasterData;
using Game.Contracts.Save;
using Game.Contracts.Save.Models;
using Game.Contracts.Settings;
using Game.Domain.GameSession;
using Game.Infrastructure.Flow;
using Game.Infrastructure.MasterData;
using Game.Infrastructure.MasterData.Assets;
using Game.Infrastructure.Save;
using Game.Infrastructure.Settings;
using Game.Presentation.Common;
using UnityEngine;

namespace Game.Presentation.Boot
{
    public sealed class BootEntryPoint : MonoBehaviour
    {
        [Header("MasterData")]
        [SerializeField] private List<StageDefinitionAsset> stageDefinitions = new List<StageDefinitionAsset>();
        [SerializeField] private PlayerParamsAsset playerParams;
        [SerializeField] private List<BossParamsAsset> bossParams = new List<BossParamsAsset>();
        [SerializeField] private List<AttackSequenceAsset> attackSequences = new List<AttackSequenceAsset>();
        [SerializeField] private List<StorySequenceAsset> storySequences = new List<StorySequenceAsset>();

        [Header("Scene Names")]
        [SerializeField] private string titleSceneName = "TitleScene";
        [SerializeField] private string gameSceneName = "GameScene";

        private void Start()
        {
            try
            {
                // Fail fast: 起動時に必須参照が欠けている場合はここで止める。
                ValidateSerializedFields();

                // Composition Root: 各層の実装を束ねて依存関係を構成する。
                ISaveRepository saveRepository = new JsonSaveRepository();
                ISettingsApplier settingsApplier = new UnitySettingsApplier();
                IMasterDataRepository masterDataRepository = new ScriptableObjectMasterDataRepository(
                    stageDefinitions,
                    playerParams,
                    bossParams,
                    attackSequences,
                    storySequences);
                ISceneLoader sceneLoader = new UnitySceneLoader(titleSceneName, gameSceneName);

                // 保存済み設定を読み込み、タイトル表示前に反映する。
                SaveDataContract saveData = saveRepository.LoadOrCreateDefault();
                settingsApplier.ApplySettings(saveData.Settings);

                // 実行時セッションを生成し、シーン跨ぎで参照できるよう保持する。
                GameSession gameSession = new GameSession();
                GameSessionHolder holder = ResolveOrCreateHolder();
                holder.Initialize(gameSession);

                // TitleScene から利用するサービス群を登録する。
                GameFlowUseCase flowUseCase = new GameFlowUseCase(gameSession, masterDataRepository, sceneLoader);
                OptionUseCase optionUseCase = new OptionUseCase(saveRepository, settingsApplier);
                GameServicesLocator.Set(new GameServices(
                    flowUseCase,
                    optionUseCase,
                    gameSession,
                    saveRepository,
                    settingsApplier,
                    masterDataRepository));

                // Boot完了後は常にTitleからゲームフローを開始する。
                flowUseCase.StartFromTitle();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                throw;
            }
        }

        private GameSessionHolder ResolveOrCreateHolder()
        {
            if (GameSessionHolder.Instance != null)
                return GameSessionHolder.Instance;

            // BootScene内にHolderが無い場合はランタイム生成する。
            GameObject holderObject = new GameObject("GameSessionHolder");
            return holderObject.AddComponent<GameSessionHolder>();
        }

        private void ValidateSerializedFields()
        {
            ValidateAssetList(stageDefinitions, nameof(stageDefinitions));
            ValidateAsset(playerParams, nameof(playerParams));
            ValidateAssetList(bossParams, nameof(bossParams));
            ValidateAssetList(attackSequences, nameof(attackSequences));
            ValidateAssetList(storySequences, nameof(storySequences));

            if (string.IsNullOrWhiteSpace(titleSceneName))
                throw new InvalidOperationException("titleSceneName is null or empty.");

            if (string.IsNullOrWhiteSpace(gameSceneName))
                throw new InvalidOperationException("gameSceneName is null or empty.");
        }

        private static void ValidateAsset(UnityEngine.Object asset, string name)
        {
            if (asset == null)
                throw new InvalidOperationException($"BootEntryPoint required asset is null: {name}");
        }

        private static void ValidateAssetList<T>(IReadOnlyList<T> assets, string name) where T : UnityEngine.Object
        {
            if (assets == null)
                throw new InvalidOperationException($"BootEntryPoint required list is null: {name}");

            if (assets.Count == 0)
                throw new InvalidOperationException($"BootEntryPoint required list is empty: {name}");

            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] == null)
                    throw new InvalidOperationException($"BootEntryPoint list contains null asset: {name}[{i}]");
            }
        }
    }
}
