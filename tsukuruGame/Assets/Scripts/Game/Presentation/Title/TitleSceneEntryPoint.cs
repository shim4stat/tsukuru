using System;
using Game.Domain.GameSession;
using Game.Presentation.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation.Title
{
    public sealed class TitleSceneEntryPoint : MonoBehaviour
    {
        [SerializeField] private TitleRootView titleRootView;
        [SerializeField] private StageSelectWindowView stageSelectWindowView;
        [SerializeField] private OptionWindowView optionWindowView;

        private TitleRootPresenter _presenter;
        private StageSelectWindowPresenter _stageSelectPresenter;
        private OptionWindowPresenter _optionPresenter;
        private ScreenStackRouter _screenStackRouter;
        private GameServices _services;

        private void Start()
        {
            try
            {
                if (titleRootView == null)
                    throw new InvalidOperationException("TitleSceneEntryPoint.titleRootView is not assigned.");
                if (stageSelectWindowView == null)
                    throw new InvalidOperationException("TitleSceneEntryPoint.stageSelectWindowView is not assigned.");
                if (optionWindowView == null)
                    throw new InvalidOperationException("TitleSceneEntryPoint.optionWindowView is not assigned.");

                _services = GameServicesLocator.Require();
                _presenter = new TitleRootPresenter(titleRootView, _services.GameFlowUseCase);
                _stageSelectPresenter = new StageSelectWindowPresenter(
                    stageSelectWindowView,
                    _services.GameFlowUseCase,
                    _services.MasterDataRepository,
                    _services.SaveRepository);
                _optionPresenter = new OptionWindowPresenter(
                    optionWindowView,
                    _services.OptionUseCase,
                    _services.GameFlowUseCase);
                _screenStackRouter = new ScreenStackRouter();
                _screenStackRouter.Register(
                    "stage_select",
                    100,
                    IsStageSelectActive,
                    _services.GameFlowUseCase.CloseStageSelect);
                _screenStackRouter.Register(
                    "option",
                    200,
                    IsOptionActive,
                    _optionPresenter.CloseAndSave);

                titleRootView.Bind();
                stageSelectWindowView.Bind();
                optionWindowView.Bind();
                stageSelectWindowView.Hide();
                optionWindowView.Hide();
                titleRootView.FocusInitial();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                throw;
            }
        }

        private void Update()
        {
            if (_services == null)
                return;

            bool shouldShowStageSelect = IsStageSelectActive();
            bool shouldShowOption = IsOptionActive();

            if (shouldShowStageSelect)
            {
                _stageSelectPresenter.Open();
                _optionPresenter.HideIfOpen();
            }
            else if (shouldShowOption)
            {
                _optionPresenter.Open();
                _stageSelectPresenter.Close();
            }
            else
            {
                _stageSelectPresenter.Close();
                _optionPresenter.HideIfOpen();
            }

            if (IsCancelPressed())
                _screenStackRouter.TryHandleBack();
        }

        private void OnDestroy()
        {
            if (titleRootView != null)
                titleRootView.Unbind();
            if (stageSelectWindowView != null)
                stageSelectWindowView.Unbind();
            if (optionWindowView != null)
                optionWindowView.Unbind();

            _presenter?.Dispose();
            _stageSelectPresenter?.Dispose();
            _optionPresenter?.Dispose();
            _screenStackRouter?.Clear();
            _presenter = null;
            _stageSelectPresenter = null;
            _optionPresenter = null;
            _screenStackRouter = null;
            _services = null;
        }

        private bool IsStageSelectActive()
        {
            return _services != null && _services.GameSession.GameMode == GameMode.StageSelect;
        }

        private bool IsOptionActive()
        {
            return _services != null && _services.GameSession.GameMode == GameMode.Option;
        }

        private static bool IsCancelPressed()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                return true;

            return false;
        }
    }
}
