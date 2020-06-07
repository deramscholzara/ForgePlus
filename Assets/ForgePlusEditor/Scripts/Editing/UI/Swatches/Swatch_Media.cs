using ForgePlus.LevelManipulation;
using RuntimeCore.Entities.Geometry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Palette
{
    [RequireComponent(typeof(Toggle))]
    public class Swatch_Media : MonoBehaviour
    {
        public LevelEntity_Media Media;

        [SerializeField]
        private TextMeshProUGUI label = null;

        [SerializeField]
        private TextMeshProUGUI label_Type = null;

        public void SetInitialValues(LevelEntity_Media media, ToggleGroup toggleGroup)
        {
            Media = media;

            label.text = media.NativeIndex.ToString();
            label_Type.text = media.NativeObject.Type.ToString();

            var toggle = GetComponent<Toggle>();
            toggle.group = toggleGroup;
        }

        public void OnValueChanged(bool value)
        {
            if (value)
            {
                SelectionManager.Instance.ToggleObjectSelection(Media, multiSelect: false);
            }
            else
            {
                SelectionManager.Instance.DeselectObject(Media, multiSelect: false);
            }
        }
    }
}
