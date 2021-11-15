using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Toggle_PostProcessing_AmbientOcclusion : MonoBehaviour
    {
        public void OnValueChanged(bool value)
        {
            SettingsManager.Instance.AmbientOcclusionEnabled = value;
        }

        public void OnEnable()
        {
            GetComponent<Toggle>().SetIsOnWithoutNotify(SettingsManager.Instance.AmbientOcclusionEnabled);
        }
    }
}
