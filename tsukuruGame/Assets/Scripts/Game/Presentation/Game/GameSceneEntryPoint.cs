using System;
using System.Collections.Generic;
using NumericsVector3 = System.Numerics.Vector3;
using Game.Contracts.MasterData.Models;
using Game.Domain.Battle;
using Game.Domain.GameSession;
using Game.Presentation.Common;
using Game.Presentation.TestBoss;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation.Game
{
    /// <summary>
    /// GameSceneの正式な進行入口。Story -> Battle -> Exit の骨格を管理する。
    /// </summary>
    public sealed class GameSceneEntryPoint : MonoBehaviour
    {
        private const string DefaultClearRank = "C";

        [SerializeField] private GameHudView gameHudView;
        [SerializeField] private BossTitleOverlayView bossTitleOverlayView;
        [SerializeField] private Vector3 bossSpawnPosition = new Vector3(0f, 4f, 0f);
        [SerializeField] private TestBossBossView testBossBossPrefab;
        [SerializeField] private TestBossBulletView testBossBulletPrefab;

        private enum FlowState
        {
            Initializing = 0,
            StoryBeforeBattle = 1,
            Battle = 2,
            StoryAfterBattle = 3,
            Exit = 4,
            Completed = 5,
        }

        private GameServices _services;
        private GameSession _session;
        private StageDefinitionContract _stage;
        private PlayerParamsContract _playerParams;
        private BossParamsContract _bossParams;
        private BattleContext _battleContext;
        private BattleEntityFactory _battleEntityFactory;
        private BattleFlowService _battleFlowService;
        private BossActionService _bossActionService;
        private BossDamageService _bossDamageService;
        private EnemyBulletService _enemyBulletService;
        private TestBossBattleRuntime _testBossBattleRuntime;
        private GameHudPresenter _gameHudPresenter;
        private BossTitleOverlayPresenter _bossTitleOverlayPresenter;
        private FlowState _flowState = FlowState.Initializing;
        private bool _isInitialized;
        private bool _isExiting;

        private void Start()
        {
            try
            {
                InitializeFlow();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                throw;
            }
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            switch (_flowState)
            {
                case FlowState.StoryBeforeBattle:
                    HandleStoryBeforeBattle();
                    break;
                case FlowState.Battle:
                    HandleBattle();
                    break;
                case FlowState.StoryAfterBattle:
                    HandleStoryAfterBattle();
                    break;
                case FlowState.Exit:
                    HandleExit();
                    break;
                case FlowState.Completed:
                    break;
                case FlowState.Initializing:
                default:
                    throw new InvalidOperationException($"Unexpected GameScene flow state: {_flowState}");
            }
        }

        private void OnDestroy()
        {
            if (_gameHudPresenter != null)
            {
                _gameHudPresenter.Dispose();
            }

            if (_gameHudPresenter != null && gameHudView != null)
            {
                gameHudView.Unbind();
            }

            if (_bossTitleOverlayPresenter != null)
            {
                _bossTitleOverlayPresenter.Finished -= OnBossTitleOverlayFinished;
                _bossTitleOverlayPresenter.Dispose();
            }

            if (_bossTitleOverlayPresenter != null && bossTitleOverlayView != null)
            {
                bossTitleOverlayView.Unbind();
            }

            _battleContext = null;
            _battleEntityFactory = null;
            _battleFlowService = null;
            _bossActionService = null;
            _bossDamageService = null;
            _enemyBulletService = null;
            _testBossBattleRuntime?.Dispose();
            _testBossBattleRuntime = null;
            _gameHudPresenter = null;
            _bossTitleOverlayPresenter = null;
            _stage = null;
            _playerParams = null;
            _bossParams = null;
            _session = null;
            _services = null;
            _isInitialized = false;
            _isExiting = false;
            _flowState = FlowState.Initializing;
        }

        private void InitializeFlow()
        {
            _services = GameServicesLocator.Require();
            _session = _services.GameSession ?? throw new InvalidOperationException("GameServices.GameSession is null.");

            if (!_session.IsInGame)
                throw new InvalidOperationException("GameSceneEntryPoint requires GameSession to be InGame.");

            if (string.IsNullOrWhiteSpace(_session.CurrentStageId))
                throw new InvalidOperationException("GameSession.CurrentStageId is null or empty.");

            _stage = _services.MasterDataRepository.GetStage(_session.CurrentStageId);
            if (_stage == null)
                throw new InvalidOperationException($"Stage master data is null: {_session.CurrentStageId}");

            _playerParams = _services.MasterDataRepository.GetPlayerParams();
            if (_playerParams == null)
                throw new InvalidOperationException("Player master data is null.");

            _bossParams = ResolveBossParamsForCurrentStage();
            if (_bossParams == null)
                throw new InvalidOperationException("Boss master data is null.");

            _bossDamageService = new BossDamageService();
            InitializeGameHud();
            InitializeBossTitleOverlay();
            CreateBattleRuntime();

            if (_stage.HasIntroStory)
            {
                if (_session.InGameMode != InGameMode.StoryBeforeBattle)
                    _session.SetInGameMode(InGameMode.StoryBeforeBattle);

                _flowState = FlowState.StoryBeforeBattle;
                return;
            }

            if (_session.InGameMode != InGameMode.Battle)
                _session.SetInGameMode(InGameMode.Battle);

            StartBattleIfNeeded();
            _flowState = FlowState.Battle;
        }

        private void CreateBattleRuntime()
        {
            StageId battleStageId = ParseBattleStageId(_session.CurrentStageId);

            _battleEntityFactory = new BattleEntityFactory();
            _battleContext = new BattleContext();
            _battleContext.Setup(battleStageId, _battleEntityFactory);
            InitializeBossRuntime();
            _battleFlowService = new BattleFlowService();
            _bossActionService = new BossActionService();
            _bossActionService.Initialize(_battleContext.Boss, _bossParams);
            _enemyBulletService = new EnemyBulletService();
            InitializeTestBossBattleRuntimeIfNeeded();
        }

        private void StartBattleIfNeeded()
        {
            if (_battleContext == null)
                throw new InvalidOperationException("BattleContext is not initialized.");
            if (_battleFlowService == null)
                throw new InvalidOperationException("BattleFlowService is not initialized.");

            if (_battleContext.Phase != BattlePhase.BattleStart)
                return;

            _battleFlowService.StartBattle(_battleContext, _session);
        }

        private void HandleStoryBeforeBattle()
        {
            HideGameHud();

            // STORY-01/UI-05 未実装の間は暫定スキップしてBattleへ入る。
            if (_session.InGameMode != InGameMode.Battle)
                _session.SetInGameMode(InGameMode.Battle);

            StartBattleIfNeeded();
            _flowState = FlowState.Battle;
        }

        private void HandleBattle()
        {
            if (_battleContext == null || _battleFlowService == null)
                throw new InvalidOperationException("Battle runtime is not initialized.");

            _testBossBattleRuntime?.TickBeforeBattleSimulation(Time.deltaTime);
            _battleFlowService.Update(_battleContext, _session, Time.deltaTime);

            if (_battleContext.Phase == BattlePhase.BossBoot)
            {
                _testBossBattleRuntime?.TickAfterBattleSimulation();
                RenderGameHud();
                HandleBossBoot();
                return;
            }

            _bossTitleOverlayPresenter?.ForceHide();
            HandleDebugBossDamageInput();

            if (_battleContext.Phase == BattlePhase.Combat)
            {
                UpdateCombatEnemyBullets(Time.deltaTime);
                ApplyBossActionRequests(Time.deltaTime);
            }

            _testBossBattleRuntime?.TickAfterBattleSimulation();
            RenderGameHud();

            if (_battleContext.Phase == BattlePhase.BossDefeated)
            {
                if (_stage != null && _stage.HasOutroStory)
                {
                    if (_session.InGameMode != InGameMode.StoryAfterBattle)
                        _session.SetInGameMode(InGameMode.StoryAfterBattle);

                    _flowState = FlowState.StoryAfterBattle;
                    return;
                }

                _battleFlowService.OnBossDefeatedSequenceFinished(_battleContext, _session);
            }

            if (_battleContext.Phase == BattlePhase.BattleEnd)
                _flowState = FlowState.Exit;
        }

        private void HandleStoryAfterBattle()
        {
            if (_battleContext == null || _battleFlowService == null)
                throw new InvalidOperationException("Battle runtime is not initialized.");

            HideGameHud();

            if (_session.InGameMode != InGameMode.StoryAfterBattle)
                _session.SetInGameMode(InGameMode.StoryAfterBattle);

            // STORY-01/UI-05 未実装の間は暫定スキップしてBattleEndへ進める。
            if (_battleContext.Phase == BattlePhase.BossDefeated)
                _battleFlowService.OnBossDefeatedSequenceFinished(_battleContext, _session);

            if (_battleContext.Phase != BattlePhase.BattleEnd)
                throw new InvalidOperationException($"Expected BattleEnd after outro skip, but was {_battleContext.Phase}.");

            _flowState = FlowState.Exit;
        }

        private void HandleExit()
        {
            if (_isExiting)
                return;

            HideGameHud();
            _isExiting = true;
            string clearedStageId = _session != null ? _session.CurrentStageId : string.Empty;
            if (string.IsNullOrWhiteSpace(clearedStageId))
                throw new InvalidOperationException("Current stage id is empty at exit.");

            _services.StageClearUseCase.SaveStageClear(clearedStageId, DefaultClearRank);
            _services.GameFlowUseCase.ReturnToTitleWithStageSelect();
            _flowState = FlowState.Completed;
        }

        private void HandleBossBoot()
        {
            if (_bossTitleOverlayPresenter == null)
                throw new InvalidOperationException("BossTitleOverlayPresenter is not initialized.");

            if (!_bossTitleOverlayPresenter.IsPlaying && (bossTitleOverlayView == null || !bossTitleOverlayView.IsVisible))
                _bossTitleOverlayPresenter.Open(ResolveBossTitleText());

            _bossTitleOverlayPresenter.Update(Time.deltaTime);
        }

        private void InitializeBossTitleOverlay()
        {
            if (bossTitleOverlayView == null)
                throw new InvalidOperationException("GameSceneEntryPoint.bossTitleOverlayView is not assigned.");

            bossTitleOverlayView.Bind();
            bossTitleOverlayView.Hide();

            _bossTitleOverlayPresenter = new BossTitleOverlayPresenter(bossTitleOverlayView);
            _bossTitleOverlayPresenter.Finished += OnBossTitleOverlayFinished;
        }

        private BossParamsContract ResolveBossParamsForCurrentStage()
        {
            if (_services == null)
                throw new InvalidOperationException("GameServices are not initialized.");
            if (_stage == null)
                throw new InvalidOperationException("Stage master data is not initialized.");
            if (string.IsNullOrWhiteSpace(_stage.Id))
                throw new InvalidOperationException("StageDefinition.Id is null or empty.");

            if (TestBossParamsResolver.TryResolve(_stage.Id, out BossParamsContract testBossParams))
                return testBossParams;

            if (string.IsNullOrWhiteSpace(_stage.BossId))
                throw new InvalidOperationException($"StageDefinition.BossId is null or empty. stageId={_stage.Id}");

            return _services.MasterDataRepository.GetBossParams(_stage.BossId);
        }

        private void InitializeBossRuntime()
        {
            if (_battleContext == null)
                throw new InvalidOperationException("BattleContext is not initialized.");
            if (_battleContext.Boss == null)
                throw new InvalidOperationException("BattleContext.Boss is not initialized.");
            if (_bossParams == null)
                throw new InvalidOperationException("Boss master data is not initialized.");

            _battleContext.Boss.Initialize(_bossParams);
            _battleContext.Boss.SetPosition(ToNumericsVector3(bossSpawnPosition));
        }

        private void InitializeTestBossBattleRuntimeIfNeeded()
        {
            if (_stage == null || !TestBossSelector.ShouldUseForStage(_stage.Id))
                return;
            if (_battleContext == null)
                throw new InvalidOperationException("BattleContext is not initialized.");
            if (_playerParams == null)
                throw new InvalidOperationException("Player master data is not initialized.");
            if (_enemyBulletService == null)
                throw new InvalidOperationException("EnemyBulletService is not initialized.");

            bool hasBossPrefab = testBossBossPrefab != null;
            bool hasBulletPrefab = testBossBulletPrefab != null;
            if (hasBossPrefab != hasBulletPrefab)
            {
                Debug.LogWarning(
                    "Test boss prefab setup is incomplete. Falling back to runtime debug visuals. " +
                    $"bossPrefabAssigned={hasBossPrefab}, bulletPrefabAssigned={hasBulletPrefab}",
                    this);
            }

            _testBossBattleRuntime = new TestBossBattleRuntime(
                transform,
                _battleContext,
                _playerParams,
                _enemyBulletService,
                testBossBossPrefab,
                testBossBulletPrefab);
            _testBossBattleRuntime.Initialize();
        }

        private void InitializeGameHud()
        {
            if (gameHudView == null)
                throw new InvalidOperationException("GameSceneEntryPoint.gameHudView is not assigned.");

            gameHudView.Bind();
            gameHudView.Hide();

            _gameHudPresenter = new GameHudPresenter(gameHudView);
        }

        private void RenderGameHud()
        {
            if (_gameHudPresenter == null)
                throw new InvalidOperationException("GameHudPresenter is not initialized.");

            _gameHudPresenter.Render(BuildHudModel());
        }

        private void HideGameHud()
        {
            _gameHudPresenter?.Hide();
        }

        private void UpdateCombatEnemyBullets(float deltaTime)
        {
            if (_battleContext == null)
                throw new InvalidOperationException("BattleContext is not initialized.");
            if (_enemyBulletService == null)
                throw new InvalidOperationException("EnemyBulletService is not initialized.");

            _enemyBulletService.Update(_battleContext, deltaTime);
        }

        private void ApplyBossActionRequests(float deltaTime)
        {
            if (_battleContext == null)
                throw new InvalidOperationException("BattleContext is not initialized.");
            if (_battleEntityFactory == null)
                throw new InvalidOperationException("BattleEntityFactory is not initialized.");
            if (_bossActionService == null)
                throw new InvalidOperationException("BossActionService is not initialized.");
            if (_enemyBulletService == null)
                throw new InvalidOperationException("EnemyBulletService is not initialized.");

            IReadOnlyList<EnemyBulletSpawnRequest> requests = _bossActionService.Update(_battleContext, deltaTime);
            _enemyBulletService.Spawn(_battleContext, _battleEntityFactory, requests);
        }

        private void HandleDebugBossDamageInput()
        {
#if UNITY_EDITOR
            if (!IsDebugBossDamagePressed())
                return;

            if (_bossDamageService == null)
                throw new InvalidOperationException("BossDamageService is not initialized.");
            if (_battleContext == null || _battleFlowService == null || _session == null)
                throw new InvalidOperationException("Battle runtime is not initialized.");

            _bossDamageService.ApplyBossDamage(_battleContext, _session, _battleFlowService, 1);
#endif
        }

        private static bool IsDebugBossDamagePressed()
        {
#if UNITY_EDITOR
            return Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        private void OnBossTitleOverlayFinished()
        {
            if (_battleContext == null || _battleFlowService == null || _session == null)
                throw new InvalidOperationException("Battle runtime is not initialized.");

            if (_battleContext.Phase != BattlePhase.BossBoot)
                return;

            _battleFlowService.OnBossBootFinished(_battleContext, _session);
        }

        private string ResolveBossTitleText()
        {
            if (_stage != null && !string.IsNullOrWhiteSpace(_stage.DisplayName))
                return _stage.DisplayName;

            if (_session != null && !string.IsNullOrWhiteSpace(_session.CurrentStageId))
                return _session.CurrentStageId;

            return string.Empty;
        }

        private GameHudViewModel BuildHudModel()
        {
            if (_session == null)
                return GameHudViewModel.Hidden;

            bool visible = _session.IsInGame && _session.InGameMode == InGameMode.Battle;
            if (!visible)
                return GameHudViewModel.Hidden;

            int playerHpMax = Mathf.Max(1, _playerParams != null ? _playerParams.MaxHp : 1);
            int playerEnergyMax = Mathf.Max(1, _playerParams != null ? _playerParams.MaxEnergy : 1);

            if (_battleContext != null && _battleContext.Player != null && _battleContext.Player.HasInitializedStats)
            {
                playerHpMax = Mathf.Max(1, _battleContext.Player.MaxHp);
            }

            int playerHpCurrent = _battleContext != null && _battleContext.Player != null && _battleContext.Player.HasInitializedStats
                ? Mathf.Clamp(_battleContext.Player.CurrentHp, 0, playerHpMax)
                : playerHpMax;
            int playerEnergyCurrent = 0;
            bool showBossGauge = _battleContext != null && _battleContext.Boss != null;
            float bossHpNormalized = showBossGauge ? _battleContext.Boss.GetCurrentGaugeHpNormalized() : 0f;

            return new GameHudViewModel(
                visible: true,
                playerHpCurrent: playerHpCurrent,
                playerHpMax: playerHpMax,
                playerEnergyCurrent: playerEnergyCurrent,
                playerEnergyMax: playerEnergyMax,
                showBossGauge: showBossGauge,
                bossHpNormalized: bossHpNormalized);
        }

        private static StageId ParseBattleStageId(string stageIdText)
        {
            if (string.IsNullOrWhiteSpace(stageIdText))
                throw new ArgumentException("stageIdText is null or empty.", nameof(stageIdText));

            int end = stageIdText.Length - 1;
            int start = end;

            while (start >= 0 && char.IsDigit(stageIdText[start]))
                start--;

            int digitStart = start + 1;
            if (digitStart > end)
                throw new InvalidOperationException($"Failed to parse numeric StageId from '{stageIdText}'.");

            string numericPart = stageIdText.Substring(digitStart, end - digitStart + 1);
            if (!int.TryParse(numericPart, out int numericId))
                throw new InvalidOperationException($"Failed to parse numeric StageId from '{stageIdText}'.");

            if (numericId <= 0)
                throw new InvalidOperationException($"Parsed StageId must be positive. input='{stageIdText}', parsed={numericId}");

            // 暫定対応: stage_01 の末尾数値を Domain.StageId(int) に変換する。
            return new StageId(numericId);
        }

        private static NumericsVector3 ToNumericsVector3(Vector3 source)
        {
            return new NumericsVector3(source.x, source.y, source.z);
        }
    }
}
