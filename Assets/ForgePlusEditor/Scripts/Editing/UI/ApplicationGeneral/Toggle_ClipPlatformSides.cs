using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Toggle_ClipPlatformSides : MonoBehaviour
    {
        public void OnValueChanged(bool value)
        {
            SettingsManager.Instance.ClipPlatformSidesEnabled = value;
        }

        public void OnEnable()
        {
            GetComponent<Toggle>().SetIsOnWithoutNotify(SettingsManager.Instance.ClipPlatformSidesEnabled);
        }
    }
}
