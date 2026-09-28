using ForgePlus.LevelManipulation;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForgePlus.ApplicationGeneral
{
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

        private static readonly int minimumLightPropertyId = Shader.PropertyToID("_GlobalMinimumLight");

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
