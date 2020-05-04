using SimpleFileBrowser;
using System.Collections;
using UnityEngine;
using System;
using ForgePlus.DataFileIO.Extensions;
using ForgePlus.LevelManipulation;

namespace ForgePlus.DataFileIO
{
    public class Toggle_SelectSecondaryMode : MonoBehaviour
    {
        [SerializeField]
        private ModeManager.SecondaryModes mode = ModeManager.SecondaryModes.None;

        public void OnValueChanged(bool value)
        {
            if (value)
            {
                ModeManager.Instance.SecondaryMode = mode;
            }
        }
    }
}
