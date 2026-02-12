using System;
using System.Collections.Generic;
using Game.Contracts.Settings.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Presentation.Title
{
    public sealed class OptionWindowView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Toggle bgmToggle;
        [SerializeField] private Slider seSlider;
        [SerializeField] private Toggle seToggle;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button closeButton;
        [SerializeField] private Selectable initialFocus;

        private readonly ResolutionOption[] _resolutionOptions =
        {
            new ResolutionOption(1280, 720),
            new ResolutionOption(1600, 900),
            new ResolutionOption(1920, 1080),
        };

        private bool _suppressEvents;

        public event Action<GameSettingsContract> SettingsChanged;
        public event Action CloseRequested;

        public bool IsVisible => root != null && root.activeSelf;

        public void Bind()
        {
            ValidateBindings();
            SetupResolutionDropdownIfNeeded();

            bgmSlider.onValueChanged.AddListener(OnAnySettingsChanged);
            bgmToggle.onValueChanged.AddListener(OnAnySettingsChanged);
            seSlider.onValueChanged.AddListener(OnAnySettingsChanged);
            seToggle.onValueChanged.AddListener(OnAnySettingsChanged);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            fullscreenToggle.onValueChanged.AddListener(OnAnySettingsChanged);
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        public void Unbind()
        {
            if (bgmSlider != null)
                bgmSlider.onValueChanged.RemoveListener(OnAnySettingsChanged);
            if (bgmToggle != null)
                bgmToggle.onValueChanged.RemoveListener(OnAnySettingsChanged);
            if (seSlider != null)
                seSlider.onValueChanged.RemoveListener(OnAnySettingsChanged);
            if (seToggle != null)
                seToggle.onValueChanged.RemoveListener(OnAnySettingsChanged);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveListener(OnAnySettingsChanged);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        public void Show()
        {
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        public void SetSettings(GameSettingsContract settings)
        {
            ValidateBindings();
            GameSettingsContract normalized = Normalize(settings);

            _suppressEvents = true;
            try
            {
                bgmSlider.SetValueWithoutNotify(normalized.Volume.BgmVolume);
                bgmToggle.SetIsOnWithoutNotify(normalized.Volume.BgmEnabled);
                seSlider.SetValueWithoutNotify(normalized.Volume.SeVolume);
                seToggle.SetIsOnWithoutNotify(normalized.Volume.SeEnabled);
                fullscreenToggle.SetIsOnWithoutNotify(normalized.Graphics.Fullscreen);

                int resolutionIndex = FindResolutionIndex(normalized.Graphics.Width, normalized.Graphics.Height);
                resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        public void FocusInitial()
        {
            if (EventSystem.current == null)
                return;

            Selectable focus = initialFocus != null ? initialFocus : bgmSlider;
            if (focus == null)
                return;

            EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }

        private void SetupResolutionDropdownIfNeeded()
        {
            resolutionDropdown.options.Clear();
            for (int i = 0; i < _resolutionOptions.Length; i++)
                resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(_resolutionOptions[i].Label));

            resolutionDropdown.RefreshShownValue();
        }

        private void OnAnySettingsChanged(float _)
        {
            TryEmitSettingsChanged();
        }

        private void OnAnySettingsChanged(bool _)
        {
            TryEmitSettingsChanged();
        }

        private void OnResolutionChanged(int _)
        {
            TryEmitSettingsChanged();
        }

        private void OnCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void TryEmitSettingsChanged()
        {
            if (_suppressEvents)
                return;

            SettingsChanged?.Invoke(BuildCurrentSettings());
        }

        private GameSettingsContract BuildCurrentSettings()
        {
            ResolutionOption resolution = ResolveResolutionOption(resolutionDropdown.value);

            return new GameSettingsContract
            {
                Volume = new VolumeSettingsContract
                {
                    BgmVolume = bgmSlider.value,
                    BgmEnabled = bgmToggle.isOn,
                    SeVolume = seSlider.value,
                    SeEnabled = seToggle.isOn,
                },
                Graphics = new GraphicsSettingsContract
                {
                    Width = resolution.Width,
                    Height = resolution.Height,
                    Fullscreen = fullscreenToggle.isOn,
                },
            };
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < _resolutionOptions.Length; i++)
            {
                if (_resolutionOptions[i].Width == width && _resolutionOptions[i].Height == height)
                    return i;
            }

            for (int i = 0; i < _resolutionOptions.Length; i++)
            {
                if (_resolutionOptions[i].Width == 1920 && _resolutionOptions[i].Height == 1080)
                    return i;
            }

            return 0;
        }

        private ResolutionOption ResolveResolutionOption(int index)
        {
            if (index < 0 || index >= _resolutionOptions.Length)
                return _resolutionOptions[0];

            return _resolutionOptions[index];
        }

        private static GameSettingsContract Normalize(GameSettingsContract settings)
        {
            if (settings == null)
                settings = new GameSettingsContract();

            VolumeSettingsContract volume = settings.Volume ?? new VolumeSettingsContract();
            GraphicsSettingsContract graphics = settings.Graphics ?? new GraphicsSettingsContract();

            return new GameSettingsContract
            {
                Volume = new VolumeSettingsContract
                {
                    BgmVolume = Mathf.Clamp01(volume.BgmVolume),
                    BgmEnabled = volume.BgmEnabled,
                    SeVolume = Mathf.Clamp01(volume.SeVolume),
                    SeEnabled = volume.SeEnabled,
                },
                Graphics = new GraphicsSettingsContract
                {
                    Width = graphics.Width > 0 ? graphics.Width : 1920,
                    Height = graphics.Height > 0 ? graphics.Height : 1080,
                    Fullscreen = graphics.Fullscreen,
                },
            };
        }

        private void ValidateBindings()
        {
            if (root == null)
                throw new InvalidOperationException("OptionWindowView.root is not assigned.");
            if (bgmSlider == null)
                throw new InvalidOperationException("OptionWindowView.bgmSlider is not assigned.");
            if (bgmToggle == null)
                throw new InvalidOperationException("OptionWindowView.bgmToggle is not assigned.");
            if (seSlider == null)
                throw new InvalidOperationException("OptionWindowView.seSlider is not assigned.");
            if (seToggle == null)
                throw new InvalidOperationException("OptionWindowView.seToggle is not assigned.");
            if (resolutionDropdown == null)
                throw new InvalidOperationException("OptionWindowView.resolutionDropdown is not assigned.");
            if (fullscreenToggle == null)
                throw new InvalidOperationException("OptionWindowView.fullscreenToggle is not assigned.");
            if (closeButton == null)
                throw new InvalidOperationException("OptionWindowView.closeButton is not assigned.");
        }
    }
}
