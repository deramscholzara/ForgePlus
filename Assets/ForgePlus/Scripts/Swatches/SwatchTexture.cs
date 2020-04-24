using ForgePlus.LevelManipulation;
using ForgePlus.ShapesCollections;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Weland;

namespace ForgePlus.Palette
{
    [RequireComponent(typeof(Toggle))]
    public class SwatchTexture : MonoBehaviour
    {
        public ShapeDescriptor ShapeDescriptor;

        [SerializeField]
        private TextMeshProUGUI label = null;

        [SerializeField]
        private RawImage texturePreview = null;

        [SerializeField]
        private GameObject usageIndicator = null;

        private bool isSelected = false;

        public void SetInitialValues(KeyValuePair<ShapeDescriptor, Texture2D> textureEntry, ToggleGroup toggleGroup)
        {
            ShapeDescriptor = textureEntry.Key;

            label.text = $"C: {ShapeDescriptor.Collection} B: {ShapeDescriptor.Bitmap}";
            texturePreview.texture = textureEntry.Value;
            texturePreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texturePreview.texture.width / (float)texturePreview.texture.height;

            var toggle = GetComponent<Toggle>();
            toggle.group = toggleGroup;

            isSelected = toggle.isOn;

            usageIndicator.SetActive(WallsCollection.GetTextureIsInUse(ShapeDescriptor));
        }

        public void OnValueChanged(bool value)
        {
            if (isSelected != value)
            {
                isSelected = value;
                // TODO: Add FPTexture class and InspectorFPTexture?
            }
        }
    }
}
