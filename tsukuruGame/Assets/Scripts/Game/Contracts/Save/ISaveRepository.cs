using Game.Contracts.Save.Models;

namespace Game.Contracts.Save
{
    /// <summary>
    /// Save data persistence port.
    /// </summary>
    public interface ISaveRepository
    {
        SaveDataContract LoadOrCreateDefault();

        void Save(SaveDataContract data);
    }
}
