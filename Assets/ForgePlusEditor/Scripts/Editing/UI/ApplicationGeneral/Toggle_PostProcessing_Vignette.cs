using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Toggle_PostProcessing_Vignette : MonoBehaviour
    {
        public void OnValueChanged(bool value)
        {
            SettingsManager.Instance.VignetteEnabled = value;
        }

        public void OnEnable()
        {
            GetComponent<Toggle>().SetIsOnWithoutNotify(SettingsManager.Instance.VignetteEnabled);
        }
    }
}
