namespace Game.Contracts.Settings.Models
{
    /// <summary>
    /// Serializable game settings contract.
    /// </summary>
    public sealed class GameSettingsContract
    {
        public VolumeSettingsContract Volume { get; set; } = new VolumeSettingsContract();

        public GraphicsSettingsContract Graphics { get; set; } = new GraphicsSettingsContract();
    }

    public sealed class VolumeSettingsContract
    {
        public float BgmVolume { get; set; } = 1.0f;

        public bool BgmEnabled { get; set; } = true;

        public float SeVolume { get; set; } = 1.0f;

        public bool SeEnabled { get; set; } = true;
    }

    public sealed class GraphicsSettingsContract
    {
        public int Width { get; set; } = 1920;

        public int Height { get; set; } = 1080;

        public bool Fullscreen { get; set; } = true;
    }
}
