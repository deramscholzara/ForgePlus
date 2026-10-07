using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Name tags for points in the level, shown while their point is in view (and their anchor is active), which call
    // back their owner when clicked
    public class WorldLabels
    {
        private readonly VisualElement layer;
        private readonly Dictionary<Label, Transform> anchors = new Dictionary<Label, Transform>();

        public WorldLabels(VisualElement root)
        {
            layer = WorldLayer.Create(root, "world-labels");
        }

        public Label Add(Transform anchor, Action onClick)
        {
            // Level text isn't markup
            var label = new Label { enableRichText = false };
            label.AddToClassList("fp-world-label");
            label.style.display = DisplayStyle.None;
            label.RegisterCallback<ClickEvent>(clickEvent => onClick());

            layer.Add(label);
            anchors[label] = anchor;

            return label;
        }

        public void Remove(Label label)
        {
            anchors.Remove(label);
            label.RemoveFromHierarchy();
        }

        // After the camera has moved for the frame
        public void UpdatePositions()
        {
            var camera = Camera.main;
            var panel = layer.panel;

            foreach (var labelAnchor in anchors)
            {
                var label = labelAnchor.Key;
                var anchor = labelAnchor.Value;

                if (anchor && anchor.gameObject.activeInHierarchy && WorldLayer.TryGetPanelPosition(anchor.position, camera, panel, out var position))
                {
                    label.style.left = position.x;
                    label.style.top = position.y;
                    label.style.display = DisplayStyle.Flex;
                }
                else
                {
                    label.style.display = DisplayStyle.None;
                }
            }
        }
    }
}
