using System;
using Game.Contracts.Save;
using Game.Contracts.Save.Models;
using Game.Contracts.Settings;
using Game.Contracts.Settings.Models;

namespace Game.Application.Option
{
    public sealed class OptionUseCase
    {
        private const int DefaultWidth = 1920;
        private const int DefaultHeight = 1080;

        private readonly ISaveRepository _saveRepository;
        private readonly ISettingsApplier _settingsApplier;

        private GameSettingsContract _workingSettings;

        public OptionUseCase(ISaveRepository saveRepository, ISettingsApplier settingsApplier)
        {
            _saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
            _settingsApplier = settingsApplier ?? throw new ArgumentNullException(nameof(settingsApplier));
        }

        public GameSettingsContract OpenAndGetCurrentSettings()
        {
            SaveDataContract save = _saveRepository.LoadOrCreateDefault();
            GameSettingsContract current = NormalizeSettings(save.Settings);
            _workingSettings = CloneSettings(current);
            return CloneSettings(_workingSettings);
        }

        public void ApplyChange(GameSettingsContract nextSettings)
        {
            if (nextSettings == null)
                throw new ArgumentNullException(nameof(nextSettings));

            _workingSettings = NormalizeSettings(nextSettings);
            _settingsApplier.ApplySettings(CloneSettings(_workingSettings));
        }

        public void CloseAndSave()
        {
            if (_workingSettings == null)
            {
                OpenAndGetCurrentSettings();
            }

            SaveDataContract save = _saveRepository.LoadOrCreateDefault();
            save.Settings = CloneSettings(_workingSettings);
            _saveRepository.Save(save);
            _workingSettings = null;
        }

        public void Cancel()
        {
            _workingSettings = null;
        }

        private static GameSettingsContract NormalizeSettings(GameSettingsContract input)
        {
            GameSettingsContract source = input ?? new GameSettingsContract();
            VolumeSettingsContract sourceVolume = source.Volume ?? new VolumeSettingsContract();
            GraphicsSettingsContract sourceGraphics = source.Graphics ?? new GraphicsSettingsContract();

            return new GameSettingsContract
            {
                Volume = new VolumeSettingsContract
                {
                    BgmVolume = Clamp01(sourceVolume.BgmVolume),
                    BgmEnabled = sourceVolume.BgmEnabled,
                    SeVolume = Clamp01(sourceVolume.SeVolume),
                    SeEnabled = sourceVolume.SeEnabled,
                },
                Graphics = new GraphicsSettingsContract
                {
                    Width = NormalizeDimension(sourceGraphics.Width, DefaultWidth),
                    Height = NormalizeDimension(sourceGraphics.Height, DefaultHeight),
                    Fullscreen = sourceGraphics.Fullscreen,
                },
            };
        }

        private static GameSettingsContract CloneSettings(GameSettingsContract source)
        {
            if (source == null)
                return NormalizeSettings(null);

            return new GameSettingsContract
            {
                Volume = new VolumeSettingsContract
                {
                    BgmVolume = source.Volume?.BgmVolume ?? 1.0f,
                    BgmEnabled = source.Volume?.BgmEnabled ?? true,
                    SeVolume = source.Volume?.SeVolume ?? 1.0f,
                    SeEnabled = source.Volume?.SeEnabled ?? true,
                },
                Graphics = new GraphicsSettingsContract
                {
                    Width = source.Graphics?.Width ?? DefaultWidth,
                    Height = source.Graphics?.Height ?? DefaultHeight,
                    Fullscreen = source.Graphics?.Fullscreen ?? true,
                },
            };
        }

        private static int NormalizeDimension(int value, int fallback)
        {
            return value > 0 ? value : fallback;
        }

        private static float Clamp01(float value)
        {
            if (value < 0.0f)
                return 0.0f;
            if (value > 1.0f)
                return 1.0f;

            return value;
        }
    }
}
