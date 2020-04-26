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

        public void SetInitialValues(FPMedia fpMedia, ToggleGroup toggleGroup)
        {
            FPMedia = fpMedia;

            label.text = fpMedia.Index.ToString();
            label_Type.text = fpMedia.WelandObject.Type.ToString();

            var toggle = GetComponent<Toggle>();
            toggle.group = toggleGroup;
        }

        public void OnValueChanged(bool value)
        {
            if (value)
            {
                SelectionManager.Instance.ToggleObjectSelection(FPMedia, multiSelect: false);
            }
            else
            {
                SelectionManager.Instance.DeselectObject(FPMedia, multiSelect: false);
            }
        }
    }
}
