using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A list of the level's lights, each as its swatch in the light palette (its index, and a preview of its current
    // intensity), opened over the whole UI below a field (or above it, where there's more room). Choosing one closes
    // it, as does clicking outside it or pressing escape.
    public static class LightPicker
    {
        private const float MinimumWidth = 160f;
        private const float MaximumHeight = 360f;

        private static VisualElement openLayer;

        public static void Show(VisualElement anchor, int currentLight, Action<int> onChosen)
        {
            Close();

            var level = LevelEntity_Level.Instance;
            var root = anchor.panel?.visualTree.Q(className: "fp-root");
            if (!level || root == null)
            {
                return;
            }

            // Covers the UI, so a click outside the list closes it rather than reaching what's under it
            var layer = new VisualElement { focusable = true };
            layer.AddToClassList("fp-popup-layer");
            layer.RegisterCallback<PointerDownEvent>(pointerDownEvent =>
            {
                if (pointerDownEvent.target == layer)
                {
                    pointerDownEvent.StopPropagation();
                    Close();
                }
            });
            layer.RegisterCallback<KeyDownEvent>(keyDownEvent =>
            {
                if (keyDownEvent.keyCode == KeyCode.Escape)
                {
                    keyDownEvent.StopPropagation();
                    Close();
                }
            });

            var box = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            box.AddToClassList("fp-box");
            box.AddToClassList("fp-scroll");
            box.AddToClassList("fp-light-picker");
            layer.Add(box);

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
                if (light.NativeIndex == currentLight)
                {
                    currentToggle = toggle;
                }

                var index = light.NativeIndex;
                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    Close();
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
            layer.schedule.Execute(UpdatePreviews).Every(0);

            root.Add(layer);
            openLayer = layer;

            // Below the field (or above it, if there's more room there), at least as wide as it
            var anchorBounds = anchor.worldBound;
            var topLeft = root.WorldToLocal(new Vector2(anchorBounds.xMin, anchorBounds.yMin));
            var bottomLeft = root.WorldToLocal(new Vector2(anchorBounds.xMin, anchorBounds.yMax));
            var rootHeight = root.layout.height;
            var roomBelow = rootHeight - bottomLeft.y;
            var roomAbove = topLeft.y;
            var opensBelow = roomBelow >= Mathf.Min(MaximumHeight, roomAbove);

            box.style.left = Mathf.Max(0f, Mathf.Min(topLeft.x, root.layout.width - Mathf.Max(MinimumWidth, anchorBounds.width)));
            box.style.width = Mathf.Max(MinimumWidth, anchorBounds.width);
            box.style.maxHeight = Mathf.Min(MaximumHeight, opensBelow ? roomBelow : roomAbove);
            if (opensBelow)
            {
                box.style.top = bottomLeft.y;
            }
            else
            {
                box.style.bottom = rootHeight - topLeft.y;
            }

            layer.Focus();

            // After layout, so the current light has a position to scroll to
            if (currentToggle != null)
            {
                box.schedule.Execute(() => box.ScrollTo(currentToggle));
            }
        }

        public static void Close()
        {
            openLayer?.RemoveFromHierarchy();
            openLayer = null;
        }
    }
}
