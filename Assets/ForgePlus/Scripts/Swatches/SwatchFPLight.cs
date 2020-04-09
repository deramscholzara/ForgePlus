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

        public void SetInitialValues(FPLight fpLight, ToggleGroup toggleGroup)
        {
            label.text = fpLight.Index.ToString();
            FPLight = fpLight;
            GetComponent<Toggle>().group = toggleGroup;
        }

        public void OnClick()
        {
            SelectionManager.Instance.ToggleObjectSelection(FPLight, multiSelect: false);
        }

        private void Update()
        {
            lightPreview.color = new Color(FPLight.CurrentIntensity, FPLight.CurrentIntensity, FPLight.CurrentIntensity, 1f);
        }
    }
}
