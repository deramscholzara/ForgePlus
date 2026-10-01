using ForgePlus.LevelManipulation;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForgePlus.ApplicationGeneral
{
    // What Save Merged does with the loaded map file's checksum
    public enum MergedSaveChecksums
    {
        // Asks each time, in a dialog that can remember the choice
        Ask,
        Keep,
        Regenerate,
    }

    // Early, so it's ready before the settings UI binds to it
    [DefaultExecutionOrder(-100)]
    public class SettingsManager : SingletonMonoBehaviour<SettingsManager>
    {
        public VolumeProfile effectsDisabledVolumeProfile;
        public Camera mainCamera;

        private const string PlayerPrefsSettingsKey_FullScreen = "Settings_FullScreen";
        private const string PlayerPrefsSettingsKey_MinimumLight = "Settings_MinimumLight";
        private const string PlayerPrefsSettingsKey_AmbientOcclusion = "Settings_AmbientOcclusion";
        private const string PlayerPrefsSettingsKey_Bloom = "Settings_Bloom";
        private const string PlayerPrefsSettingsKey_ColorAdjustment = "Settings_ColorAdjustment";
        private const string PlayerPrefsSettingsKey_Vignette = "Settings_Vignette";
        private const string PlayerPrefsSettingsKey_ClipPlatformSides = "Settings_ClipPlatformSides";
        private const string PlayerPrefsSettingsKey_MergedSaveChecksum = "Settings_MergedSaveChecksum";

        private static readonly int minimumLightPropertyId = Shader.PropertyToID("_GlobalMinimumLight");

        // The name of the setting (its property) that changed, which SettingsViewModel's properties share
        public event Action<string> OnSettingChanged;

        private event Action OnObjectVisibilityChanged_Sender;
        public event Action OnObjectVisibilityChanged
        {
            add
            {
                OnObjectVisibilityChanged_Sender += value;
                value.Invoke();
            }
            remove
            {
                OnObjectVisibilityChanged_Sender -= value;
            }
        }

        // These last for the session only (they aren't saved)
        private bool objectIconsEnabled = true;
        private bool spritePreviewsEnabled = true;

        public bool IsFullScreen
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_FullScreen, 1) == 0 ? false : true;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_FullScreen, value ? 1 : 0);

                if (Screen.fullScreen != value)
                {
                    Screen.fullScreen = value;
                }

                OnSettingChanged?.Invoke(nameof(IsFullScreen));
            }
        }

        public float MinimumLight
        {
            get
            {
                return PlayerPrefs.GetFloat(PlayerPrefsSettingsKey_MinimumLight, 0.025f);
            }
            set
            {
                PlayerPrefs.SetFloat(PlayerPrefsSettingsKey_MinimumLight, value);

                Shader.SetGlobalFloat(minimumLightPropertyId, value);

                OnSettingChanged?.Invoke(nameof(MinimumLight));
            }
        }

        public bool AmbientOcclusionEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_AmbientOcclusion, 1) == 0 ? false : true;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_AmbientOcclusion, value ? 1 : 0);

                if (effectsDisabledVolumeProfile.TryGet<ScreenSpaceAmbientOcclusionVolumeOverride>(out var profileComponent))
                {
                    profileComponent.active = !value;
                    mainCamera.UpdateVolumeStack();
                }

                OnSettingChanged?.Invoke(nameof(AmbientOcclusionEnabled));
            }
        }

        public bool BloomEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_Bloom, 1) == 0 ? false : true;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_Bloom, value ? 1 : 0);

                if (effectsDisabledVolumeProfile.TryGet<Bloom>(out var profileComponent))
                {
                    profileComponent.active = !value;
                    mainCamera.UpdateVolumeStack();
                }

                OnSettingChanged?.Invoke(nameof(BloomEnabled));
            }
        }

        public bool ColorCorrectionEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_ColorAdjustment, 1) == 0 ? false : true;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_ColorAdjustment, value ? 1 : 0);

                if (effectsDisabledVolumeProfile.TryGet<SplitToning>(out var profileComponent))
                {
                    profileComponent.active = !value;
                    mainCamera.UpdateVolumeStack();
                }

                OnSettingChanged?.Invoke(nameof(ColorCorrectionEnabled));
            }
        }

        public bool VignetteEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_Vignette, 1) == 0 ? false : true;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_Vignette, value ? 1 : 0);

                if (effectsDisabledVolumeProfile.TryGet<Vignette>(out var profileComponent))
                {
                    profileComponent.active = !value;
                    mainCamera.UpdateVolumeStack();
                }

                OnSettingChanged?.Invoke(nameof(VignetteEnabled));
            }
        }

        public bool ClipPlatformSidesEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_ClipPlatformSides, 1) == 0 ? false : true;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_ClipPlatformSides, value ? 1 : 0);

                PlatformSideClipping.ClippingEnabled = value;

                OnSettingChanged?.Invoke(nameof(ClipPlatformSidesEnabled));
            }
        }

        public MergedSaveChecksums MergedSaveChecksum
        {
            get
            {
                var value = PlayerPrefs.GetInt(PlayerPrefsSettingsKey_MergedSaveChecksum, (int) MergedSaveChecksums.Ask);

                return Enum.IsDefined(typeof(MergedSaveChecksums), value) ? (MergedSaveChecksums) value : MergedSaveChecksums.Ask;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_MergedSaveChecksum, (int) value);

                OnSettingChanged?.Invoke(nameof(MergedSaveChecksum));
            }
        }

        public bool ObjectIconsEnabled
        {
            get
            {
                return objectIconsEnabled;
            }
            set
            {
                objectIconsEnabled = value;

                ApplyObjectVisibility();

                OnSettingChanged?.Invoke(nameof(ObjectIconsEnabled));
            }
        }

        // Object mode always shows icons, since they're how objects are selected there
        public bool ObjectIconsForcedOn
        {
            get
            {
                return ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Objects;
            }
        }

        public bool SpritePreviewsEnabled
        {
            get
            {
                return spritePreviewsEnabled;
            }
            set
            {
                spritePreviewsEnabled = value;

                ApplyObjectVisibility();

                OnSettingChanged?.Invoke(nameof(SpritePreviewsEnabled));
            }
        }

        private void ApplyObjectVisibility()
        {
            LevelEntity_MapObject.IconsAreVisible = objectIconsEnabled || ObjectIconsForcedOn;
            LevelEntity_MapObject.SpritePreviewsAreVisible = spritePreviewsEnabled;

            OnObjectVisibilityChanged_Sender?.Invoke();
        }

        private void OnPrimaryModeChanged(ModeManager.PrimaryModes primaryMode)
        {
            ApplyObjectVisibility();
        }

        private void Start()
        {
            IsFullScreen = IsFullScreen;
            MinimumLight = MinimumLight;
            ClipPlatformSidesEnabled = ClipPlatformSidesEnabled;

            ModeManager.Instance.OnPrimaryModeChanged += OnPrimaryModeChanged;
        }
    }
}
