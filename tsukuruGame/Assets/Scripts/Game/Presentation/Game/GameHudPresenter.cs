using System;
using UnityEngine;

namespace Game.Presentation.Game
{
    /// <summary>
    /// GameHUDの表示値正規化とView描画呼び出しを担当する。
    /// </summary>
    public sealed class GameHudPresenter : IDisposable
    {
        private readonly GameHudView _view;
        private bool _disposed;
        private bool _isVisible;

        public GameHudPresenter(GameHudView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Show()
        {
            if (_disposed || _isVisible)
                return;

            _view.Show();
            _isVisible = true;
        }

        public void Hide()
        {
            if (_disposed || !_isVisible)
                return;

            _view.Hide();
            _isVisible = false;
        }

        public void Render(GameHudViewModel model)
        {
            if (_disposed)
                return;

            if (!model.Visible)
            {
                Hide();
                return;
            }

            Show();

            int hpMax = Mathf.Max(1, model.PlayerHpMax);
            int hpCurrent = Mathf.Clamp(model.PlayerHpCurrent, 0, hpMax);
            int energyMax = Mathf.Max(1, model.PlayerEnergyMax);
            int energyCurrent = Mathf.Clamp(model.PlayerEnergyCurrent, 0, energyMax);
            float bossGauge = Mathf.Clamp01(model.BossHpNormalized);

            _view.SetPlayerHp(hpCurrent, hpMax);
            _view.SetPlayerEnergy(energyCurrent, energyMax);
            _view.SetBossHpGaugeVisible(model.ShowBossGauge);

            if (model.ShowBossGauge)
                _view.SetBossHpGauge(bossGauge);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
        }
    }
}
