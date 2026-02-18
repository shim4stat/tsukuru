using System;
using System.Collections.Generic;
using Game.Contracts.Save.Models;
using Game.Contracts.Settings.Models;
using Game.Domain.Save;

namespace Game.Infrastructure.Save
{
    public static class SaveDataMapper
    {
        public static SaveDataJsonModel ToJsonModel(SaveDataContract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            GameSettingsContract settings = contract.Settings ?? new GameSettingsContract();
            VolumeSettingsContract volume = settings.Volume ?? new VolumeSettingsContract();
            GraphicsSettingsContract graphics = settings.Graphics ?? new GraphicsSettingsContract();

            SaveDataJsonModel model = new SaveDataJsonModel
            {
                version = NormalizeVersion(contract.Version),
                settings = new GameSettingsJsonModel
                {
                    bgmVolume = Clamp01(volume.BgmVolume),
                    bgmEnabled = volume.BgmEnabled,
                    seVolume = Clamp01(volume.SeVolume),
                    seEnabled = volume.SeEnabled,
                    width = NormalizeDimension(graphics.Width, 1920),
                    height = NormalizeDimension(graphics.Height, 1080),
                    fullscreen = graphics.Fullscreen,
                },
                stages = new List<StageProgressJsonModel>(),
            };

            if (contract.Stages != null)
            {
                for (int i = 0; i < contract.Stages.Count; i++)
                {
                    StageProgressContract stage = contract.Stages[i];
                    if (stage == null)
                        continue;

                    model.stages.Add(new StageProgressJsonModel
                    {
                        stageId = stage.StageId ?? string.Empty,
                        unlocked = stage.Unlocked,
                        cleared = stage.Cleared,
                        bestRank = NormalizeRankString(stage.BestRank),
                    });
                }
            }

            return model;
        }

        public static SaveDataContract ToContractNormalized(SaveDataJsonModel model)
        {
            SaveDataContract defaultContract = CreateDefaultContract();
            if (model == null)
                return defaultContract;

            GameSettingsContract settings = defaultContract.Settings;
            VolumeSettingsContract volume = settings.Volume;
            GraphicsSettingsContract graphics = settings.Graphics;

            if (model.settings != null)
            {
                volume.BgmVolume = Clamp01(model.settings.bgmVolume);
                volume.BgmEnabled = model.settings.bgmEnabled;
                volume.SeVolume = Clamp01(model.settings.seVolume);
                volume.SeEnabled = model.settings.seEnabled;
                graphics.Width = NormalizeDimension(model.settings.width, graphics.Width);
                graphics.Height = NormalizeDimension(model.settings.height, graphics.Height);
                graphics.Fullscreen = model.settings.fullscreen;
            }

            SaveDataContract normalized = new SaveDataContract
            {
                Version = NormalizeVersion(model.version),
                Settings = settings,
                Stages = new List<StageProgressContract>(),
            };

            if (model.stages != null)
            {
                for (int i = 0; i < model.stages.Count; i++)
                {
                    StageProgressJsonModel stage = model.stages[i];
                    if (stage == null)
                        continue;

                    normalized.Stages.Add(new StageProgressContract
                    {
                        StageId = stage.stageId ?? string.Empty,
                        Unlocked = stage.unlocked,
                        Cleared = stage.cleared,
                        BestRank = NormalizeRankString(stage.bestRank),
                    });
                }
            }

            return normalized;
        }

        public static SaveDataContract CreateDefaultContract()
        {
            SaveData defaultDomain = SaveData.CreateDefault();

            GameSettingsContract settings = new GameSettingsContract
            {
                Volume = new VolumeSettingsContract
                {
                    BgmVolume = defaultDomain.Settings.BgmVolume.Value,
                    BgmEnabled = defaultDomain.Settings.BgmVolume.IsEnabled,
                    SeVolume = defaultDomain.Settings.SeVolume.Value,
                    SeEnabled = defaultDomain.Settings.SeVolume.IsEnabled,
                },
                Graphics = new GraphicsSettingsContract
                {
                    Width = defaultDomain.Settings.GraphicsSettings.Width,
                    Height = defaultDomain.Settings.GraphicsSettings.Height,
                    Fullscreen = defaultDomain.Settings.GraphicsSettings.WindowMode == WindowMode.Fullscreen,
                },
            };

            SaveDataContract contract = new SaveDataContract
            {
                Version = defaultDomain.Version,
                Settings = settings,
                Stages = new List<StageProgressContract>(),
            };

            return contract;
        }

        private static string NormalizeRankString(string bestRank)
        {
            if (string.IsNullOrWhiteSpace(bestRank))
                return string.Empty;

            string normalized = bestRank.Trim().ToUpperInvariant();
            if (normalized == "S" || normalized == "A" || normalized == "B" || normalized == "C")
                return normalized;

            return string.Empty;
        }

        private static int NormalizeVersion(int version)
        {
            return version > 0 ? version : SaveData.CurrentVersion;
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
