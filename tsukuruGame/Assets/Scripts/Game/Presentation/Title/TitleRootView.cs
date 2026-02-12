using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Presentation.Title
{
    public sealed class TitleRootView : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button stageSelectButton;
        [SerializeField] private Button optionButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Selectable initialFocus;

        public event Action StartRequested;
        public event Action OpenStageSelectRequested;
        public event Action OpenOptionRequested;
        public event Action QuitRequested;

        public void Bind()
        {
            ValidateBindings();

            startButton.onClick.AddListener(OnStartClicked);
            stageSelectButton.onClick.AddListener(OnStageSelectClicked);
            optionButton.onClick.AddListener(OnOptionClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        public void Unbind()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);

            if (stageSelectButton != null)
                stageSelectButton.onClick.RemoveListener(OnStageSelectClicked);

            if (optionButton != null)
                optionButton.onClick.RemoveListener(OnOptionClicked);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        public void FocusInitial()
        {
            Selectable focus = initialFocus != null ? initialFocus : startButton;
            if (focus == null || EventSystem.current == null)
                return;

            EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }

        private void ValidateBindings()
        {
            if (startButton == null)
                throw new InvalidOperationException("TitleRootView.startButton is not assigned.");

            if (stageSelectButton == null)
                throw new InvalidOperationException("TitleRootView.stageSelectButton is not assigned.");

            if (optionButton == null)
                throw new InvalidOperationException("TitleRootView.optionButton is not assigned.");

            if (quitButton == null)
                throw new InvalidOperationException("TitleRootView.quitButton is not assigned.");
        }

        private void OnStartClicked()
        {
            StartRequested?.Invoke();
        }

        private void OnStageSelectClicked()
        {
            OpenStageSelectRequested?.Invoke();
        }

        private void OnOptionClicked()
        {
            OpenOptionRequested?.Invoke();
        }

        private void OnQuitClicked()
        {
            QuitRequested?.Invoke();
        }
    }
}
