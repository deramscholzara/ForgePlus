using ForgePlus.LevelManipulation;
using RuntimeCore.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Palette
{
    [RequireComponent(typeof(Toggle))]
    public class Swatch_Light : MonoBehaviour
    {
        public LevelEntity_Light RuntimeLight;

        [SerializeField]
        private TextMeshProUGUI label = null;

        [SerializeField]
        private Image lightPreview = null;

        public void SetInitialValues(LevelEntity_Light runtimeLight, ToggleGroup toggleGroup)
        {
            RuntimeLight = runtimeLight;

            label.text = runtimeLight.NativeIndex.ToString();

            var toggle = GetComponent<Toggle>();
            toggle.group = toggleGroup;
        }

        public void OnValueChanged(bool value)
        {
            if (value)
            {
                SelectionManager.Instance.ToggleObjectSelection(RuntimeLight, multiSelect: false);
            }
            else
            {
                SelectionManager.Instance.DeselectObject(RuntimeLight, multiSelect: false);
            }
        }

        private void Update()
        {
            lightPreview.color = new Color(RuntimeLight.CurrentDisplayIntensity, RuntimeLight.CurrentDisplayIntensity, RuntimeLight.CurrentDisplayIntensity, 1f);
        }
    }
}
