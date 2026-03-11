using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Game
{
    /// <summary>
    /// InGame HUD（HP/エネルギー/ボスHPゲージ）の表示を担当するView。
    /// </summary>
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text playerHpLabel;
        [SerializeField] private TMP_Text playerEnergyLabel;
        [SerializeField] private GameObject bossHpRoot;
        [SerializeField] private Image bossHpFillImage;

        public bool IsVisible => root != null && root.activeSelf;

        public void Bind()
        {
            ValidateBindings();
            SetBossHpGauge(1f);
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
            root.SetActive(false);
        }

        public void SetPlayerHp(int current, int max)
        {
            ValidateBindings();
            playerHpLabel.text = $"HP {current}/{max}";
        }

        public void SetPlayerEnergy(int current, int max)
        {
            ValidateBindings();
            playerEnergyLabel.text = $"EN {current}/{max}";
        }

        public void SetBossHpGauge(float normalized01)
        {
            ValidateBindings();
            bossHpFillImage.fillAmount = Mathf.Clamp01(normalized01);
        }

        public void SetBossHpGaugeVisible(bool visible)
        {
            ValidateBindings();

            if (bossHpRoot != null)
                bossHpRoot.SetActive(visible);
        }

        private void ValidateBindings()
        {
            if (root == null)
                throw new InvalidOperationException("GameHudView.root is not assigned.");
            if (playerHpLabel == null)
                throw new InvalidOperationException("GameHudView.playerHpLabel is not assigned.");
            if (playerEnergyLabel == null)
                throw new InvalidOperationException("GameHudView.playerEnergyLabel is not assigned.");
            if (bossHpFillImage == null)
                throw new InvalidOperationException("GameHudView.bossHpFillImage is not assigned.");
        }
    }
}
