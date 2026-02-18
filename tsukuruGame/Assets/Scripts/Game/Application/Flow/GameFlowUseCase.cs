using System;
using Game.Contracts.Flow;
using Game.Contracts.MasterData;
using Game.Domain.GameSession;

namespace Game.Application.Flow
{
    public sealed class GameFlowUseCase
    {
        private readonly GameSession _gameSession;
        private readonly IMasterDataRepository _masterDataRepository;
        private readonly ISceneLoader _sceneLoader;

        public GameFlowUseCase(
            GameSession gameSession,
            IMasterDataRepository masterDataRepository,
            ISceneLoader sceneLoader)
        {
            _gameSession = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
            _masterDataRepository = masterDataRepository ?? throw new ArgumentNullException(nameof(masterDataRepository));
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
        }

        public void StartFromTitle()
        {
            _gameSession.EnterTitle();
            _sceneLoader.LoadTitleScene();
        }

        public void OpenStageSelect()
        {
            _gameSession.EnterStageSelect();
        }

        public void CloseStageSelect()
        {
            _gameSession.EnterTitle();
        }

        public void OpenOption()
        {
            _gameSession.EnterOption();
        }

        public void CloseOption()
        {
            _gameSession.EnterTitle();
        }

        public void StartDefaultStage()
        {
            StartGame(DefaultStageIds.Stage01);
        }

        public void StartGame(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("stageId is null or empty.", nameof(stageId));

            var stage = _masterDataRepository.GetStage(stageId);
            if (stage == null)
                throw new InvalidOperationException($"Stage not found: {stageId}");

            _gameSession.EnterInGame(stageId, stage.HasIntroStory);
            _sceneLoader.LoadGameScene();
        }

        public void ReturnToTitleWithStageSelect()
        {
            _gameSession.EnterTitle();
            _sceneLoader.LoadTitleScene();
            _gameSession.EnterStageSelect();
        }
    }
}
