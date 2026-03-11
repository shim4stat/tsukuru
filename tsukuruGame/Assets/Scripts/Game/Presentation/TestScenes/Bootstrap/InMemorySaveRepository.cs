using System;
using System.Collections.Generic;
using Game.Contracts.Save;
using Game.Contracts.Save.Models;
using Game.Contracts.Settings.Models;
using Game.Infrastructure.Save;

namespace Game.Presentation.TestScenes.Bootstrap
{
    /// <summary>
    /// ファイルI/Oを行わないテスト用 SaveRepository。
    /// </summary>
    public sealed class InMemorySaveRepository : ISaveRepository
    {
        private SaveDataContract _current;

        public SaveDataContract LoadOrCreateDefault()
        {
            if (_current == null)
                _current = SaveDataMapper.CreateDefaultContract();

            return CloneSaveData(_current);
        }

        public void Save(SaveDataContract data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _current = CloneSaveData(data);
        }

        private static SaveDataContract CloneSaveData(SaveDataContract source)
        {
            SaveDataContract input = source ?? SaveDataMapper.CreateDefaultContract();
            SaveDataContract clone = new SaveDataContract
            {
                Version = input.Version,
                Settings = CloneSettings(input.Settings),
                Stages = new List<StageProgressContract>(),
            };

            if (input.Stages == null)
                return clone;

            for (int i = 0; i < input.Stages.Count; i++)
            {
                StageProgressContract stage = input.Stages[i];
                if (stage == null)
                    continue;

                clone.Stages.Add(new StageProgressContract
                {
                    StageId = stage.StageId ?? string.Empty,
                    Unlocked = stage.Unlocked,
                    Cleared = stage.Cleared,
                    BestRank = stage.BestRank ?? string.Empty,
                });
            }

            return clone;
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
