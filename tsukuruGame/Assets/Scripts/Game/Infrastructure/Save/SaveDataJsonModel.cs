using System;
using System.Collections.Generic;

namespace Game.Infrastructure.Save
{
    [Serializable]
    public sealed class SaveDataJsonModel
    {
        public int version;
        public List<StageProgressJsonModel> stages = new List<StageProgressJsonModel>();
        public GameSettingsJsonModel settings = new GameSettingsJsonModel();
    }

    [Serializable]
    public sealed class StageProgressJsonModel
    {
        public string stageId = string.Empty;
        public bool unlocked;
        public bool cleared;
        public string bestRank = string.Empty;
    }

    [Serializable]
    public sealed class GameSettingsJsonModel
    {
        public float bgmVolume = 1.0f;
        public bool bgmEnabled = true;
        public float seVolume = 1.0f;
        public bool seEnabled = true;
        public int width = 1920;
        public int height = 1080;
        public bool fullscreen = true;
    }
}
