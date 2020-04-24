using ForgePlus.LevelManipulation;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Palette
{
    [RequireComponent(typeof(Toggle))]
    public class SwatchFPLight : MonoBehaviour
    {
        public FPLight FPLight;

        [SerializeField]
        private TextMeshProUGUI label = null;

        [SerializeField]
        private Image lightPreview = null;

        private bool isSelected = false;

        public void SetInitialValues(FPLight fpLight, ToggleGroup toggleGroup)
        {
            FPLight = fpLight;

            label.text = fpLight.Index.ToString();

            var toggle = GetComponent<Toggle>();
            toggle.group = toggleGroup;

            isSelected = toggle.isOn;
        }

        public void OnValueChanged(bool value)
        {
            if (isSelected != value)
            {
                isSelected = value;
                SelectionManager.Instance.ToggleObjectSelection(FPLight, multiSelect: false);
            }
        }

        private void Update()
        {
            lightPreview.color = new Color(FPLight.CurrentGammaIntensity, FPLight.CurrentGammaIntensity, FPLight.CurrentGammaIntensity, 1f);
        }
    }
}
