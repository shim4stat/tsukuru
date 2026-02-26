using System;
using Game.Contracts.MasterData.Models;
using Game.Domain.Battle;
using Game.Domain.GameSession;
using Game.Presentation.Common;
using UnityEngine;

namespace Game.Presentation.Game
{
    /// <summary>
    /// GameSceneの正式な進行入口。Story -> Battle -> Exit の骨格を管理する。
    /// </summary>
    public sealed class GameSceneEntryPoint : MonoBehaviour
    {
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
        private BattleContext _battleContext;
        private BattleFlowService _battleFlowService;
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
            _battleContext = null;
            _battleFlowService = null;
            _stage = null;
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

            _battleContext = new BattleContext();
            _battleContext.Setup(battleStageId, new BattleEntityFactory());
            _battleFlowService = new BattleFlowService();
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

            _battleFlowService.Update(_battleContext, _session, Time.deltaTime);

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

            _isExiting = true;
            _services.GameFlowUseCase.ReturnToTitleWithStageSelect();
            _flowState = FlowState.Completed;
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
    }
}
