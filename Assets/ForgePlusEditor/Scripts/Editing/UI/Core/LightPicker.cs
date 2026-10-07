using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The level's lights, as the light palette shows them, in a pop-up (PopupLayer) that choosing one closes. Lights
    // below the minimum index are shown, but can't be chosen.
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

            var box = PopupLayer.Open(anchor, MinimumWidth, MaximumHeight);
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

                var index = light.NativeIndex;
                var toggle = instance.Q<Toggle>();
                toggle.SetValueWithoutNotify(index == currentLight);
                toggle.SetEnabled(index >= minimumLight);
                if (index == currentLight)
                {
                    currentToggle = toggle;
                }

                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    PopupLayer.Close();
                    onChosen(index);
                });

                previews.Add(new KeyValuePair<LevelEntity_Light, VisualElement>(light, Swatches.FillLight(instance, light)));

                swatches.Add(instance);
            }

            Swatches.ShowIntensities(previews);
            box.schedule.Execute(() => Swatches.ShowIntensities(previews)).Every(0);

            if (currentToggle != null)
            {
                box.ScrollToAfterLayout(currentToggle);
            }
        }
    }
}
