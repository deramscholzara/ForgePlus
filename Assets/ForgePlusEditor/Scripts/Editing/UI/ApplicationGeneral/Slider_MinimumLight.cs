using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Slider_MinimumLight : MonoBehaviour
    {
        public void OnValueChanged(float value)
        {
            // Square to convert to gamma-space values (only needed if the project is in Linear space)
            SettingsManager.Instance.MinimumLight = value * value;
        }

        public void OnEnable()
        {
            // Square-root to convert from gamma-space values (only needed if the project is in Linear space)
            GetComponent<Slider>().SetValueWithoutNotify(Mathf.Sqrt(SettingsManager.Instance.MinimumLight));
        }
    }
}
