using System;
using Game.Application.Flow;
using UnityEngine;

namespace Game.Presentation.Title
{
    public sealed class TitleRootPresenter : IDisposable
    {
        private readonly TitleRootView _view;
        private readonly GameFlowUseCase _flowUseCase;
        private bool _disposed;

        public TitleRootPresenter(TitleRootView view, GameFlowUseCase flowUseCase)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _flowUseCase = flowUseCase ?? throw new ArgumentNullException(nameof(flowUseCase));

            _view.StartRequested += OnStartRequested;
            _view.OpenStageSelectRequested += OnOpenStageSelectRequested;
            _view.OpenOptionRequested += OnOpenOptionRequested;
            _view.QuitRequested += OnQuitRequested;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _view.StartRequested -= OnStartRequested;
            _view.OpenStageSelectRequested -= OnOpenStageSelectRequested;
            _view.OpenOptionRequested -= OnOpenOptionRequested;
            _view.QuitRequested -= OnQuitRequested;
            _disposed = true;
        }

        private void OnStartRequested()
        {
            _flowUseCase.StartDefaultStage();
        }

        private void OnOpenStageSelectRequested()
        {
            _flowUseCase.OpenStageSelect();
        }

        private void OnOpenOptionRequested()
        {
            _flowUseCase.OpenOption();
        }

        private static void OnQuitRequested()
        {
            UnityEngine.Application.Quit();
        }
    }
}
