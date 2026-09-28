using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Toggle_ObjectIcons : MonoBehaviour
    {
        public void OnValueChanged(bool value)
        {
            SettingsManager.Instance.ObjectIconsEnabled = value;
        }

        private void Start()
        {
            SettingsManager.Instance.OnObjectVisibilityChanged += OnObjectVisibilityChanged;
        }

        private void OnObjectVisibilityChanged()
        {
            // Shown as on, and grayed out, while Object mode forces icons on
            var isForcedOn = SettingsManager.Instance.ObjectIconsForcedOn;
            var toggle = GetComponent<Toggle>();

            toggle.interactable = !isForcedOn;
            toggle.SetIsOnWithoutNotify(isForcedOn || SettingsManager.Instance.ObjectIconsEnabled);
        }
    }
}
