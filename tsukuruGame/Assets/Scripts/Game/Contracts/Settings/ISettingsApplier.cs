using Game.Contracts.Settings.Models;

namespace Game.Contracts.Settings
{
    /// <summary>
    /// Runtime settings apply port.
    /// </summary>
    public interface ISettingsApplier
    {
        void ApplySettings(GameSettingsContract settings);
    }
}
