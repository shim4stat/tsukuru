using System;
using Game.Application.Flow;
using Game.Application.Option;
using Game.Contracts.MasterData;
using Game.Contracts.Save;
using Game.Contracts.Settings;
using Game.Domain.GameSession;

namespace Game.Presentation.Common
{
    public sealed class GameServices
    {
        public GameFlowUseCase GameFlowUseCase { get; }

        public OptionUseCase OptionUseCase { get; }

        public GameSession GameSession { get; }

        public ISaveRepository SaveRepository { get; }

        public ISettingsApplier SettingsApplier { get; }

        public IMasterDataRepository MasterDataRepository { get; }

        public GameServices(
            GameFlowUseCase gameFlowUseCase,
            OptionUseCase optionUseCase,
            GameSession gameSession,
            ISaveRepository saveRepository,
            ISettingsApplier settingsApplier,
            IMasterDataRepository masterDataRepository)
        {
            GameFlowUseCase = gameFlowUseCase ?? throw new ArgumentNullException(nameof(gameFlowUseCase));
            OptionUseCase = optionUseCase ?? throw new ArgumentNullException(nameof(optionUseCase));
            GameSession = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
            SaveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
            SettingsApplier = settingsApplier ?? throw new ArgumentNullException(nameof(settingsApplier));
            MasterDataRepository = masterDataRepository ?? throw new ArgumentNullException(nameof(masterDataRepository));
        }
    }
}
