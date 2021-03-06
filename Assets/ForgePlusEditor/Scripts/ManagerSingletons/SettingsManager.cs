﻿using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    public class SettingsManager : SingletonMonoBehaviour<SettingsManager>
    {
        private const string PlayerPrefsSettingsKey_FullScreen = "Settings_FullScreen";
        private const string PlayerPrefsSettingsKey_MinimumLight = "Settings_MinimumLight";

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

        private void Start()
        {
            IsFullScreen = IsFullScreen;
            MinimumLight = MinimumLight;
        }
    }
}
