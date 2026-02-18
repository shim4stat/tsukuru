using System;

namespace Game.Domain.Save
{
    public sealed class GameSettings
    {
        public Volume BgmVolume { get; private set; }

        public Volume SeVolume { get; private set; }

        public GraphicsSettings GraphicsSettings { get; private set; }

        public GameSettings(Volume bgmVolume, Volume seVolume, GraphicsSettings graphicsSettings)
        {
            BgmVolume = bgmVolume;
            SeVolume = seVolume;
            GraphicsSettings = graphicsSettings ?? throw new ArgumentNullException(nameof(graphicsSettings));
        }

        public static GameSettings Default()
        {
            return new GameSettings(Volume.DefaultBgm(), Volume.DefaultSe(), GraphicsSettings.Default());
        }

        public void UpdateVolume(Volume bgm, Volume se)
        {
            BgmVolume = bgm;
            SeVolume = se;
        }

        public void UpdateGraphics(GraphicsSettings graphics)
        {
            GraphicsSettings = graphics ?? throw new ArgumentNullException(nameof(graphics));
        }
    }
}
