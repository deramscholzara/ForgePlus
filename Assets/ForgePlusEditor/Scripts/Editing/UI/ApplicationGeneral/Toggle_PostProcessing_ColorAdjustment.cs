using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Toggle_PostProcessing_ColorAdjustment : MonoBehaviour
    {
        public void OnValueChanged(bool value)
        {
            SettingsManager.Instance.ColorCorrectionEnabled = value;
        }

        public void OnEnable()
        {
            GetComponent<Toggle>().SetIsOnWithoutNotify(SettingsManager.Instance.ColorCorrectionEnabled);
        }
    }
}
