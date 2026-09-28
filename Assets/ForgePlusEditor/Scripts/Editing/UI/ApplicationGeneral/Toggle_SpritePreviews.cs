using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public class Toggle_SpritePreviews : MonoBehaviour
    {
        public void OnValueChanged(bool value)
        {
            SettingsManager.Instance.SpritePreviewsEnabled = value;
        }

        public void OnEnable()
        {
            GetComponent<Toggle>().SetIsOnWithoutNotify(SettingsManager.Instance.SpritePreviewsEnabled);
        }
    }
}
