using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    public class Toggle_FullScreen : MonoBehaviour
    {
        public void OnValueChanged(bool value)
        {
            SettingsManager.Instance.IsFullScreen = value;
        }
    }
}
