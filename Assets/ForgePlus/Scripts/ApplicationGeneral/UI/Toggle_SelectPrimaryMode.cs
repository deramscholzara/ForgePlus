using SimpleFileBrowser;
using System.Collections;
using UnityEngine;
using System;
using ForgePlus.DataFileIO.Extensions;
using ForgePlus.LevelManipulation;

namespace ForgePlus.DataFileIO
{
    public class Toggle_SelectPrimaryMode : MonoBehaviour
    {
        [SerializeField]
        private ModeManager.PrimaryModes mode = ModeManager.PrimaryModes.None;

        public void OnValueChanged(bool value)
        {
            if (value)
            {
                ModeManager.Instance.PrimaryMode = mode;
            }
        }
    }
}
