using System;
using Game.Domain.GameSession;
using Game.Presentation.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation.Title
{
    /// <summary>
    /// TitleSceneのエントリーポイント。
    /// View/Presenterの初期化と、GameModeに応じた画面表示制御を行う。
    /// </summary>
    public sealed class TitleSceneEntryPoint : MonoBehaviour
    {
        // Inspector割り当てビュー
        [SerializeField] private TitleRootView titleRootView;
        [SerializeField] private StageSelectWindowView stageSelectWindowView;
        [SerializeField] private OptionWindowView optionWindowView;

        // ランタイム生成・解決する依存
        private TitleRootPresenter _presenter;
        private StageSelectWindowPresenter _stageSelectPresenter;
        private OptionWindowPresenter _optionPresenter;
        private ScreenStackRouter _screenStackRouter;
        private GameServices _services;

        /// <summary>
        /// サービス解決、Presenter生成、Viewバインド、初期フォーカス設定を行う。
        /// </summary>
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
                // Back操作時は優先度の高い画面から閉じる。
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

                // 起動時は全ViewをBindし、ポップアップは閉じた状態にそろえる。
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

        /// <summary>
        /// GameSessionのモードに合わせて各ウィンドウ表示を更新し、Cancel入力を処理する。
        /// </summary>
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

        /// <summary>
        /// ViewのUnbindとPresenter破棄を行い、参照を解放する。
        /// </summary>
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

        /// <summary>
        /// StageSelectモードが有効か判定する。
        /// </summary>
        private bool IsStageSelectActive()
        {
            return _services != null && _services.GameSession.GameMode == GameMode.StageSelect;
        }

        /// <summary>
        /// Optionモードが有効か判定する。
        /// </summary>
        private bool IsOptionActive()
        {
            return _services != null && _services.GameSession.GameMode == GameMode.Option;
        }

        /// <summary>
        /// Cancel入力（Esc / ゲームパッドB）を判定する。
        /// </summary>
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
