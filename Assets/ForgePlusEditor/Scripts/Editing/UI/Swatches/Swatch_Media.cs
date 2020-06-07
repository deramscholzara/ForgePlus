using ForgePlus.LevelManipulation;
using RuntimeCore.Entities.Geometry;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Palette
{
    [RequireComponent(typeof(Toggle))]
    public class Swatch_Media : MonoBehaviour
    {
        public LevelEntity_Media FPMedia;

        [SerializeField]
        private TextMeshProUGUI label = null;

        [SerializeField]
        private TextMeshProUGUI label_Type = null;

        public void SetInitialValues(LevelEntity_Media fpMedia, ToggleGroup toggleGroup)
        {
            FPMedia = fpMedia;

            label.text = fpMedia.NativeIndex.ToString();
            label_Type.text = fpMedia.NativeObject.Type.ToString();

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
