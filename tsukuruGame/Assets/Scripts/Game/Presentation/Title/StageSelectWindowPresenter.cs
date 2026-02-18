using System;
using System.Collections.Generic;
using Game.Application.Flow;
using Game.Contracts.MasterData;
using Game.Contracts.MasterData.Models;
using Game.Contracts.Save;
using Game.Contracts.Save.Models;
using UnityEngine;

namespace Game.Presentation.Title
{
    public sealed class StageSelectWindowPresenter : IDisposable
    {
        private readonly StageSelectWindowView _view;
        private readonly GameFlowUseCase _flowUseCase;
        private readonly IMasterDataRepository _masterDataRepository;
        private readonly ISaveRepository _saveRepository;

        private bool _isOpen;
        private bool _disposed;

        public StageSelectWindowPresenter(
            StageSelectWindowView view,
            GameFlowUseCase flowUseCase,
            IMasterDataRepository masterDataRepository,
            ISaveRepository saveRepository)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _flowUseCase = flowUseCase ?? throw new ArgumentNullException(nameof(flowUseCase));
            _masterDataRepository = masterDataRepository ?? throw new ArgumentNullException(nameof(masterDataRepository));
            _saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));

            _view.StageSelected += OnStageSelected;
            _view.CloseRequested += OnCloseRequested;
        }

        public void Open()
        {
            if (_isOpen)
                return;

            IReadOnlyList<StageSelectItemViewModel> items = BuildItems();
            _view.Rebuild(items);
            _view.Show();
            _view.FocusInitial();
            _isOpen = true;
        }

        public void Close()
        {
            if (!_isOpen)
                return;

            _view.Hide();
            _isOpen = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _view.StageSelected -= OnStageSelected;
            _view.CloseRequested -= OnCloseRequested;
            _disposed = true;
        }

        private IReadOnlyList<StageSelectItemViewModel> BuildItems()
        {
            IReadOnlyList<StageDefinitionContract> stages = _masterDataRepository.GetAllStages();
            SaveDataContract save = _saveRepository.LoadOrCreateDefault();
            Dictionary<string, StageProgressContract> progressMap = BuildProgressMap(save);

            List<StageSelectItemViewModel> items = new List<StageSelectItemViewModel>(stages.Count);
            for (int i = 0; i < stages.Count; i++)
            {
                StageDefinitionContract stage = stages[i];
                bool isUnlocked = false;
                string bestRank = string.Empty;

                if (progressMap.TryGetValue(stage.Id, out StageProgressContract progress))
                {
                    isUnlocked = progress.Unlocked;
                    bestRank = progress.BestRank ?? string.Empty;
                }

                string displayName = string.IsNullOrWhiteSpace(stage.DisplayName) ? stage.Id : stage.DisplayName;
                items.Add(new StageSelectItemViewModel(stage.Id, displayName, isUnlocked, bestRank));
            }

            return items;
        }

        private static Dictionary<string, StageProgressContract> BuildProgressMap(SaveDataContract save)
        {
            Dictionary<string, StageProgressContract> map = new Dictionary<string, StageProgressContract>(StringComparer.Ordinal);
            if (save?.Stages == null)
                return map;

            for (int i = 0; i < save.Stages.Count; i++)
            {
                StageProgressContract stage = save.Stages[i];
                if (stage == null || string.IsNullOrWhiteSpace(stage.StageId))
                    continue;

                map[stage.StageId] = stage;
            }

            return map;
        }

        private void OnStageSelected(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
            {
                Debug.LogWarning("StageSelectWindowPresenter received empty stageId.");
                return;
            }

            _flowUseCase.StartGame(stageId);
        }

        private void OnCloseRequested()
        {
            _flowUseCase.CloseStageSelect();
        }
    }
}
