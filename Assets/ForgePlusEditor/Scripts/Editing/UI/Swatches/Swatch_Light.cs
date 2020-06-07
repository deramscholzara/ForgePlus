using ForgePlus.LevelManipulation;
using RuntimeCore.Entities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Palette
{
    [RequireComponent(typeof(Toggle))]
    public class Swatch_Light : MonoBehaviour
    {
        public LevelEntity_Light FPLight;

        [SerializeField]
        private TextMeshProUGUI label = null;

        [SerializeField]
        private Image lightPreview = null;

        public void SetInitialValues(LevelEntity_Light fpLight, ToggleGroup toggleGroup)
        {
            FPLight = fpLight;

            label.text = fpLight.NativeIndex.ToString();

            var toggle = GetComponent<Toggle>();
            toggle.group = toggleGroup;
        }

        public void OnValueChanged(bool value)
        {
            if (value)
            {
                SelectionManager.Instance.ToggleObjectSelection(FPLight, multiSelect: false);
            }
            else
            {
                SelectionManager.Instance.DeselectObject(FPLight, multiSelect: false);
            }
        }

        private void Update()
        {
            lightPreview.color = new Color(FPLight.CurrentGammaIntensity, FPLight.CurrentGammaIntensity, FPLight.CurrentGammaIntensity, 1f);
        }
    }
}
