using ForgePlus.LevelManipulation;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Palette
{
    [RequireComponent(typeof(Toggle))]
    public class SwatchFPMedia : MonoBehaviour
    {
        public FPMedia FPMedia;

        [SerializeField]
        private TextMeshProUGUI label = null;

        [SerializeField]
        private TextMeshProUGUI label_Type = null;

        private bool isSelected = false;

        public void SetInitialValues(FPMedia fpMedia, ToggleGroup toggleGroup)
        {
            FPMedia = fpMedia;

            label.text = fpMedia.Index.ToString();
            label_Type.text = fpMedia.WelandObject.Type.ToString();

            var toggle = GetComponent<Toggle>();
            toggle.group = toggleGroup;

            isSelected = toggle.isOn;
        }

        public void OnValueChanged(bool value)
        {
            if (isSelected != value)
            {
                isSelected = value;
                SelectionManager.Instance.ToggleObjectSelection(FPMedia, multiSelect: false);
            }
        }
    }
}
