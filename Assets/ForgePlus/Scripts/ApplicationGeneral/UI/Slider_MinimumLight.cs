using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Slider_MinimumLight : MonoBehaviour
    {
        public void OnValueChanged(float value)
        {
            SettingsManager.Instance.MinimumLight = value;
        }

        public void OnEnable()
        {
            GetComponent<Slider>().SetValueWithoutNotify(SettingsManager.Instance.MinimumLight);
        }
    }
}
