using System;
using TMPro;
using UnityEngine;

namespace Game.Presentation.Game
{
    /// <summary>
    /// ボス起動時のタイトルオーバーレイ表示を担うView。
    /// </summary>
    public sealed class BossTitleOverlayView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleLabel;

        [Header("Timings (seconds)")]
        [SerializeField] private float fadeInSeconds = 0.25f;
        [SerializeField] private float holdSeconds = 1.0f;
        [SerializeField] private float fadeOutSeconds = 0.25f;

        public bool IsVisible => root != null && root.activeSelf;

        public float FadeInDurationSeconds => Mathf.Max(0f, fadeInSeconds);

        public float HoldDurationSeconds => Mathf.Max(0f, holdSeconds);

        public float FadeOutDurationSeconds => Mathf.Max(0f, fadeOutSeconds);

        public void Bind()
        {
            ValidateBindings();
            SetOpacity(0f);
            Hide();
        }

        public void Unbind()
        {
            // 現状はイベント購読を持たないため、何もしない。
        }

        public void Show()
        {
            ValidateBindings();
            root.SetActive(true);
        }

        public void Hide()
        {
            ValidateBindings();
            canvasGroup.alpha = 0f;
            root.SetActive(false);
        }

        public void SetTitle(string text)
        {
            ValidateBindings();
            titleLabel.text = text ?? string.Empty;
        }

        public void SetOpacity(float alpha)
        {
            ValidateBindings();
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        private void ValidateBindings()
        {
            if (root == null)
                throw new InvalidOperationException("BossTitleOverlayView.root is not assigned.");
            if (canvasGroup == null)
                throw new InvalidOperationException("BossTitleOverlayView.canvasGroup is not assigned.");
            if (titleLabel == null)
                throw new InvalidOperationException("BossTitleOverlayView.titleLabel is not assigned.");
        }
    }
}
