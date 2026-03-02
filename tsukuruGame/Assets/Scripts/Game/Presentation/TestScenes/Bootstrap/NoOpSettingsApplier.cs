using Game.Contracts.Settings;
using Game.Contracts.Settings.Models;

namespace Game.Presentation.TestScenes.Bootstrap
{
    /// <summary>
    /// 設定を実適用せず、最後に渡された値のみ保持するテスト用アプライヤー。
    /// </summary>
    public sealed class NoOpSettingsApplier : ISettingsApplier
    {
        public int ApplyCallCount { get; private set; }

        public GameSettingsContract LastAppliedSettings { get; private set; } = new GameSettingsContract();

        public void ApplySettings(GameSettingsContract settings)
        {
            ApplyCallCount++;
            LastAppliedSettings = CloneSettings(settings);
        }

        private static GameSettingsContract CloneSettings(GameSettingsContract source)
        {
            if (source == null)
                return new GameSettingsContract();

            VolumeSettingsContract volume = source.Volume ?? new VolumeSettingsContract();
            GraphicsSettingsContract graphics = source.Graphics ?? new GraphicsSettingsContract();

            return new GameSettingsContract
            {
                Volume = new VolumeSettingsContract
                {
                    BgmVolume = volume.BgmVolume,
                    BgmEnabled = volume.BgmEnabled,
                    SeVolume = volume.SeVolume,
                    SeEnabled = volume.SeEnabled,
                },
                Graphics = new GraphicsSettingsContract
                {
                    Width = graphics.Width,
                    Height = graphics.Height,
                    Fullscreen = graphics.Fullscreen,
                },
            };
        }
    }
}
