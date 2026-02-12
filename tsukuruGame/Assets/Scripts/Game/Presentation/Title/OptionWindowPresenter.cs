using System;
using Game.Application.Flow;
using Game.Application.Option;
using Game.Contracts.Settings.Models;

namespace Game.Presentation.Title
{
    public sealed class OptionWindowPresenter : IDisposable
    {
        private readonly OptionWindowView _view;
        private readonly OptionUseCase _optionUseCase;
        private readonly GameFlowUseCase _flowUseCase;

        private bool _isOpen;
        private bool _disposed;

        public OptionWindowPresenter(
            OptionWindowView view,
            OptionUseCase optionUseCase,
            GameFlowUseCase flowUseCase)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _optionUseCase = optionUseCase ?? throw new ArgumentNullException(nameof(optionUseCase));
            _flowUseCase = flowUseCase ?? throw new ArgumentNullException(nameof(flowUseCase));

            _view.SettingsChanged += OnSettingsChanged;
            _view.CloseRequested += OnCloseRequested;
        }

        public void Open()
        {
            if (_isOpen)
                return;

            GameSettingsContract settings = _optionUseCase.OpenAndGetCurrentSettings();
            _view.SetSettings(settings);
            _view.Show();
            _view.FocusInitial();
            _isOpen = true;
        }

        public void CloseAndSave()
        {
            if (!_isOpen)
                return;

            _optionUseCase.CloseAndSave();
            _flowUseCase.CloseOption();
            _isOpen = false;
        }

        public void HideIfOpen()
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

            _view.SettingsChanged -= OnSettingsChanged;
            _view.CloseRequested -= OnCloseRequested;
            _disposed = true;
        }

        private void OnSettingsChanged(GameSettingsContract settings)
        {
            _optionUseCase.ApplyChange(settings);
        }

        private void OnCloseRequested()
        {
            CloseAndSave();
        }
    }
}
