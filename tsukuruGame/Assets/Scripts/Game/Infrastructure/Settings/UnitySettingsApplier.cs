using System;
using System.Collections.Generic;
using Game.Contracts.Settings;
using Game.Contracts.Settings.Models;
using UnityEngine;

namespace Game.Infrastructure.Settings
{
    public sealed class UnitySettingsApplier : ISettingsApplier
    {
        private const float DefaultVolume = 1.0f;
        private const int DefaultWidth = 1920;
        private const int DefaultHeight = 1080;

        private readonly Dictionary<AudioSource, float> _bgmSources = new Dictionary<AudioSource, float>();
        private readonly Dictionary<AudioSource, float> _seSources = new Dictionary<AudioSource, float>();

        private GameSettingsContract _currentSettings = CreateDefaultSettings();

        public GameSettingsContract CurrentSettings => CloneSettings(_currentSettings);

        public void ApplySettings(GameSettingsContract settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _currentSettings = NormalizeSettings(settings);
            CleanupDestroyedSources(_bgmSources);
            CleanupDestroyedSources(_seSources);

            ApplyToSources(_bgmSources, _currentSettings.Volume.BgmVolume, _currentSettings.Volume.BgmEnabled);
            ApplyToSources(_seSources, _currentSettings.Volume.SeVolume, _currentSettings.Volume.SeEnabled);

#if UNITY_STANDALONE
            int width = NormalizeDimension(_currentSettings.Graphics.Width, DefaultWidth);
            int height = NormalizeDimension(_currentSettings.Graphics.Height, DefaultHeight);
            FullScreenMode mode = _currentSettings.Graphics.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(width, height, mode);
#endif
        }

        public void RegisterBgmSource(AudioSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _seSources.Remove(source);
            if (!_bgmSources.ContainsKey(source))
                _bgmSources.Add(source, source.volume);

            ApplyToSource(source, _bgmSources[source], _currentSettings.Volume.BgmVolume, _currentSettings.Volume.BgmEnabled);
        }

        public void RegisterSeSource(AudioSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _bgmSources.Remove(source);
            if (!_seSources.ContainsKey(source))
                _seSources.Add(source, source.volume);

            ApplyToSource(source, _seSources[source], _currentSettings.Volume.SeVolume, _currentSettings.Volume.SeEnabled);
        }

        public void UnregisterSource(AudioSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _bgmSources.Remove(source);
            _seSources.Remove(source);
        }

        private static void ApplyToSources(Dictionary<AudioSource, float> sources, float settingVolume, bool enabled)
        {
            foreach (KeyValuePair<AudioSource, float> pair in sources)
                ApplyToSource(pair.Key, pair.Value, settingVolume, enabled);
        }

        private static void ApplyToSource(AudioSource source, float baseVolume, float settingVolume, bool enabled)
        {
            if (source == null)
                return;

            source.mute = !enabled;
            source.volume = baseVolume * Clamp01(settingVolume);
        }

        private static void CleanupDestroyedSources(Dictionary<AudioSource, float> sources)
        {
            List<AudioSource> deadSources = null;
            foreach (KeyValuePair<AudioSource, float> pair in sources)
            {
                if (pair.Key != null)
                    continue;

                if (deadSources == null)
                    deadSources = new List<AudioSource>();

                deadSources.Add(pair.Key);
            }

            if (deadSources == null)
                return;

            for (int i = 0; i < deadSources.Count; i++)
                sources.Remove(deadSources[i]);
        }

        private static GameSettingsContract NormalizeSettings(GameSettingsContract settings)
        {
            GameSettingsContract normalized = new GameSettingsContract
            {
                Volume = new VolumeSettingsContract(),
                Graphics = new GraphicsSettingsContract(),
            };

            VolumeSettingsContract inputVolume = settings.Volume ?? new VolumeSettingsContract();
            GraphicsSettingsContract inputGraphics = settings.Graphics ?? new GraphicsSettingsContract();

            normalized.Volume.BgmVolume = Clamp01(inputVolume.BgmVolume);
            normalized.Volume.BgmEnabled = inputVolume.BgmEnabled;
            normalized.Volume.SeVolume = Clamp01(inputVolume.SeVolume);
            normalized.Volume.SeEnabled = inputVolume.SeEnabled;

            normalized.Graphics.Width = NormalizeDimension(inputGraphics.Width, DefaultWidth);
            normalized.Graphics.Height = NormalizeDimension(inputGraphics.Height, DefaultHeight);
            normalized.Graphics.Fullscreen = inputGraphics.Fullscreen;

            return normalized;
        }

        private static GameSettingsContract CloneSettings(GameSettingsContract source)
        {
            return new GameSettingsContract
            {
                Volume = new VolumeSettingsContract
                {
                    BgmVolume = source.Volume.BgmVolume,
                    BgmEnabled = source.Volume.BgmEnabled,
                    SeVolume = source.Volume.SeVolume,
                    SeEnabled = source.Volume.SeEnabled,
                },
                Graphics = new GraphicsSettingsContract
                {
                    Width = source.Graphics.Width,
                    Height = source.Graphics.Height,
                    Fullscreen = source.Graphics.Fullscreen,
                },
            };
        }

        private static GameSettingsContract CreateDefaultSettings()
        {
            return new GameSettingsContract
            {
                Volume = new VolumeSettingsContract
                {
                    BgmVolume = DefaultVolume,
                    BgmEnabled = true,
                    SeVolume = DefaultVolume,
                    SeEnabled = true,
                },
                Graphics = new GraphicsSettingsContract
                {
                    Width = DefaultWidth,
                    Height = DefaultHeight,
                    Fullscreen = true,
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
