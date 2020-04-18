using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Toggle_FullScreen : MonoBehaviour
    {
        public void OnValueChanged(bool value)
        {
            SettingsManager.Instance.IsFullScreen = value;
        }

        public void OnEnable()
        {
            GetComponent<Toggle>().SetIsOnWithoutNotify(SettingsManager.Instance.IsFullScreen);
        }
    }
}
