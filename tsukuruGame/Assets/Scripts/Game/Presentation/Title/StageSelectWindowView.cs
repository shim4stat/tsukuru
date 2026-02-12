using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Presentation.Title
{
    public sealed class StageSelectWindowView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform listRoot;
        [SerializeField] private Button stageButtonPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Selectable initialFocus;
        [SerializeField] private TextMeshProUGUI stageNameLabel;
        [SerializeField] private TextMeshProUGUI rankLabel;

        private readonly List<Button> _spawnedButtons = new List<Button>();

        public event Action<string> StageSelected;
        public event Action CloseRequested;

        public bool IsVisible => root != null && root.activeSelf;

        public void Bind()
        {
            ValidateBindings();
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        public void Unbind()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        public void Show()
        {
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        public void Rebuild(IReadOnlyList<StageSelectItemViewModel> items)
        {
            ValidateBindings();
            ClearItems();

            if (items == null)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                StageSelectItemViewModel item = items[i];
                Button button = Instantiate(stageButtonPrefab, listRoot);
                button.interactable = item.IsUnlocked;
                button.name = $"Stage_{item.StageId}";
                SetButtonLabel(button, FormatListLabel(item));

                string stageId = item.StageId;
                string displayName = item.DisplayName;
                string bestRank = item.BestRank;
                button.onClick.AddListener(() => OnStageClicked(stageId, displayName, bestRank));
                _spawnedButtons.Add(button);
            }

            if (items.Count > 0)
            {
                SetSelectedStageDetails(items[0].DisplayName, items[0].BestRank);
            }
            else
            {
                SetSelectedStageDetails(string.Empty, string.Empty);
            }
        }

        public void FocusInitial()
        {
            if (EventSystem.current == null)
                return;

            Selectable focus = ResolveInitialFocus();
            if (focus == null)
                return;

            EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }

        private void OnCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void OnStageClicked(string stageId, string displayName, string bestRank)
        {
            SetSelectedStageDetails(displayName, bestRank);
            StageSelected?.Invoke(stageId);
        }

        private Selectable ResolveInitialFocus()
        {
            if (initialFocus != null && initialFocus.IsInteractable())
                return initialFocus;

            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                Button button = _spawnedButtons[i];
                if (button != null && button.interactable)
                    return button;
            }

            if (closeButton != null && closeButton.IsInteractable())
                return closeButton;

            return null;
        }

        private void SetSelectedStageDetails(string displayName, string bestRank)
        {
            if (stageNameLabel != null)
                stageNameLabel.text = displayName ?? string.Empty;

            if (rankLabel != null)
                rankLabel.text = string.IsNullOrWhiteSpace(bestRank) ? "-" : bestRank;
        }

        private static string FormatListLabel(StageSelectItemViewModel item)
        {
            string rank = string.IsNullOrWhiteSpace(item.BestRank) ? "-" : item.BestRank;
            string suffix = item.IsUnlocked ? $"[Rank:{rank}]" : "[LOCKED]";
            return $"{item.DisplayName} {suffix}";
        }

        private static void SetButtonLabel(Button button, string text)
        {
            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = text;
                return;
            }

            Text legacy = button.GetComponentInChildren<Text>();
            if (legacy != null)
                legacy.text = text;
        }

        private void ClearItems()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                Button button = _spawnedButtons[i];
                if (button != null)
                    Destroy(button.gameObject);
            }

            _spawnedButtons.Clear();
        }

        private void ValidateBindings()
        {
            if (root == null)
                throw new InvalidOperationException("StageSelectWindowView.root is not assigned.");

            if (listRoot == null)
                throw new InvalidOperationException("StageSelectWindowView.listRoot is not assigned.");

            if (stageButtonPrefab == null)
                throw new InvalidOperationException("StageSelectWindowView.stageButtonPrefab is not assigned.");

            if (closeButton == null)
                throw new InvalidOperationException("StageSelectWindowView.closeButton is not assigned.");
        }
    }
}
