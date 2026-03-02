namespace Game.Presentation.Game
{
    /// <summary>
    /// GameHUDの描画に必要な最小状態。
    /// </summary>
    public readonly struct GameHudViewModel
    {
        public bool Visible { get; }
        public int PlayerHpCurrent { get; }
        public int PlayerHpMax { get; }
        public int PlayerEnergyCurrent { get; }
        public int PlayerEnergyMax { get; }
        public bool ShowBossGauge { get; }
        public float BossHpNormalized { get; }

        public GameHudViewModel(
            bool visible,
            int playerHpCurrent,
            int playerHpMax,
            int playerEnergyCurrent,
            int playerEnergyMax,
            bool showBossGauge,
            float bossHpNormalized)
        {
            Visible = visible;
            PlayerHpCurrent = playerHpCurrent;
            PlayerHpMax = playerHpMax;
            PlayerEnergyCurrent = playerEnergyCurrent;
            PlayerEnergyMax = playerEnergyMax;
            ShowBossGauge = showBossGauge;
            BossHpNormalized = bossHpNormalized;
        }

        public static GameHudViewModel Hidden =>
            new GameHudViewModel(
                visible: false,
                playerHpCurrent: 0,
                playerHpMax: 1,
                playerEnergyCurrent: 0,
                playerEnergyMax: 1,
                showBossGauge: false,
                bossHpNormalized: 0f);
    }
}
