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

                ISaveRepository saveRepository = new JsonSaveRepository();
                ISettingsApplier settingsApplier = new UnitySettingsApplier();

                // 保存済み設定を読み込み、タイトル表示前に反映する。
                SaveDataContract saveData = saveRepository.LoadOrCreateDefault();
                settingsApplier.ApplySettings(saveData.Settings);

                GameSessionHolder holder = ResolveOrCreateHolder();
                GameServices services = ResolveOrRepairServices(holder, saveRepository, settingsApplier);

                // Boot完了後は常にTitleからゲームフローを開始する。
                services.GameFlowUseCase.StartFromTitle();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
#if UNITY_EDITOR
                throw;
#else
                enabled = false;
                Debug.LogError("Boot initialization failed in player build. component disabled.", this);
#endif
            }
        }

        private GameServices ResolveOrRepairServices(
            GameSessionHolder holder,
            ISaveRepository saveRepository,
            ISettingsApplier settingsApplier)
        {
            if (holder == null)
                throw new ArgumentNullException(nameof(holder));
            if (saveRepository == null)
                throw new ArgumentNullException(nameof(saveRepository));
            if (settingsApplier == null)
                throw new ArgumentNullException(nameof(settingsApplier));

            bool hasSession = holder.HasSession;
            bool hasServices = GameServicesLocator.TryGet(out GameServices existingServices);

            if (hasSession && hasServices && ReferenceEquals(existingServices.GameSession, holder.Session))
                return existingServices;

            if (hasSession && hasServices)
            {
                Debug.LogWarning("Boot detected inconsistent runtime state. Rebuilding services with existing holder session.");
                return RebuildServices(holder.Session, saveRepository, settingsApplier);
            }

            if (hasSession)
                return RebuildServices(holder.Session, saveRepository, settingsApplier);

            if (hasServices)
            {
                Debug.LogWarning("Boot detected services without holder session. Recreating session and rebuilding services.");
                return RebuildServices(CreateAndAssignSession(holder), saveRepository, settingsApplier);
            }

            return RebuildServices(CreateAndAssignSession(holder), saveRepository, settingsApplier);
        }

        private GameSession CreateAndAssignSession(GameSessionHolder holder)
        {
            if (holder == null)
                throw new ArgumentNullException(nameof(holder));

            if (holder.HasSession)
                return holder.Session;

            GameSession session = new GameSession();
            holder.Initialize(session);
            return session;
        }

        private GameServices RebuildServices(
            GameSession gameSession,
            ISaveRepository saveRepository,
            ISettingsApplier settingsApplier)
        {
            if (gameSession == null)
                throw new ArgumentNullException(nameof(gameSession));
            if (saveRepository == null)
                throw new ArgumentNullException(nameof(saveRepository));
            if (settingsApplier == null)
                throw new ArgumentNullException(nameof(settingsApplier));

            IMasterDataRepository masterDataRepository = new ScriptableObjectMasterDataRepository(
                stageDefinitions,
                playerParams,
                bossParams,
                attackSequences,
                storySequences);
            ISceneLoader sceneLoader = new UnitySceneLoader(titleSceneName, gameSceneName);

            GameFlowUseCase flowUseCase = new GameFlowUseCase(gameSession, masterDataRepository, sceneLoader);
            OptionUseCase optionUseCase = new OptionUseCase(saveRepository, settingsApplier);
            GameServices services = new GameServices(
                flowUseCase,
                optionUseCase,
                gameSession,
                saveRepository,
                settingsApplier,
                masterDataRepository);

            GameServicesLocator.Set(services);
            return services;
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
