using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForgePlus.ApplicationGeneral
{
    public class SettingsManager : SingletonMonoBehaviour<SettingsManager>
    {
        public VolumeProfile EffectsVolumeProfile;
        
        private const string PlayerPrefsSettingsKey_FullScreen = "Settings_FullScreen";
        private const string PlayerPrefsSettingsKey_MinimumLight = "Settings_MinimumLight";
        private const string PlayerPrefsSettingsKey_AmbientOcclusion = "Settings_AmbientOcclusion";
        private const string PlayerPrefsSettingsKey_Bloom = "Settings_Bloom";
        private const string PlayerPrefsSettingsKey_ColorAdjustment = "Settings_ColorAdjustment";
        private const string PlayerPrefsSettingsKey_Vignette = "Settings_Vignette";

        private static readonly int minimumLightPropertyId = Shader.PropertyToID("_GlobalMinimumLight");

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

                QualitySettings.SetQualityLevel(value ? 0 : 1);
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
                
                if (EffectsVolumeProfile.TryGet<Bloom>(out var bloom) &&
                    bloom.active != value)
                {
                    bloom.active = value;
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

                if (EffectsVolumeProfile.TryGet<SplitToning>(out var splitToning) &&
                    splitToning.active != value)
                {
                    splitToning.active = value;
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

                if (EffectsVolumeProfile.TryGet<Vignette>(out var vignette) &&
                    vignette.active != value)
                {
                    vignette.active = value;
                }
            }
        }

        private void Start()
        {
            IsFullScreen = IsFullScreen;
            MinimumLight = MinimumLight;
        }
    }
}
