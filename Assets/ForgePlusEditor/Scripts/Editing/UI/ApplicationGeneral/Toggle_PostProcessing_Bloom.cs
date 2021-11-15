using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Toggle_PostProcessing_Bloom : MonoBehaviour
    {
        public void OnValueChanged(bool value)
        {
            SettingsManager.Instance.BloomEnabled = value;
        }

        public void OnEnable()
        {
            GetComponent<Toggle>().SetIsOnWithoutNotify(SettingsManager.Instance.BloomEnabled);
        }
    }
}
