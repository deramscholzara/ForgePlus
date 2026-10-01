using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A list of the level's lights, each as its swatch in the light palette (its index, and a preview of its current
    // intensity), opened over the whole UI below a field (PopupLayer). Choosing one closes it. Lights below the minimum
    // index (such as light 0, which a sound source's volume can't follow) are shown, but can't be chosen.
    public static class LightPicker
    {
        private const float MinimumWidth = 160f;
        private const float MaximumHeight = 360f;

        public static void Show(VisualElement anchor, int currentLight, Action<int> onChosen, int minimumLight = 0)
        {
            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                return;
            }

            var box = PopupLayer.Open(anchor, "fp-light-picker", MinimumWidth, MaximumHeight);
            if (box == null)
            {
                return;
            }

            var swatches = new VisualElement();
            swatches.AddToClassList("fp-palette__swatches");
            box.Add(swatches);

            var template = Resources.Load<VisualTreeAsset>("UI/Templates/SwatchLight");
            var previews = new List<KeyValuePair<LevelEntity_Light, VisualElement>>();
            Toggle currentToggle = null;

            foreach (var light in level.Lights.OrderBy(pair => pair.Key).Select(pair => pair.Value))
            {
                var instance = template.Instantiate();
                instance.pickingMode = PickingMode.Ignore;

                var toggle = instance.Q<Toggle>();
                toggle.SetValueWithoutNotify(light.NativeIndex == currentLight);
                toggle.SetEnabled(light.NativeIndex >= minimumLight);
                if (light.NativeIndex == currentLight)
                {
                    currentToggle = toggle;
                }

                var index = light.NativeIndex;
                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    PopupLayer.Close();
                    onChosen(index);
                });

                instance.Q<Label>("index").text = light.NativeIndex.ToString();
                previews.Add(new KeyValuePair<LevelEntity_Light, VisualElement>(light, instance.Q("preview")));

                swatches.Add(instance);
            }

            // Lights animate, so their previews follow them every frame
            void UpdatePreviews()
            {
                foreach (var preview in previews)
                {
                    var intensity = preview.Key.CurrentDisplayIntensity;
                    preview.Value.style.backgroundColor = new Color(intensity, intensity, intensity, 1f);
                }
            }

            UpdatePreviews();
            box.schedule.Execute(UpdatePreviews).Every(0);

            // After layout, so the current light has a position to scroll to
            if (currentToggle != null)
            {
                box.schedule.Execute(() => box.ScrollTo(currentToggle));
            }
        }

        public static void Close()
        {
            PopupLayer.Close();
        }
    }
}
