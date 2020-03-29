using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    public class SettingsManager : SingletonMonoBehaviour<SettingsManager>
    {
        private const string PlayerPrefsSettingsKey_FullScreen = "Settings_FullScreen";

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

        private void Start()
        {
            IsFullScreen = IsFullScreen;
        }
    }
}
