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
        private readonly Dictionary<Label, LabelAnchor> anchors = new Dictionary<Label, LabelAnchor>();

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
            anchors[label] = new LabelAnchor { Anchor = anchor };

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
                var shown = labelAnchor.Value;
                var anchor = shown.Anchor;

                // Styles are only written when they change, as each write restyles the label even when it's the same
                if (anchor && anchor.gameObject.activeInHierarchy && WorldLayer.TryGetPanelPosition(anchor.position, camera, panel, out var position))
                {
                    if (position.x != shown.Left)
                    {
                        label.style.left = position.x;
                        shown.Left = position.x;
                    }

                    if (position.y != shown.Top)
                    {
                        label.style.top = position.y;
                        shown.Top = position.y;
                    }

                    if (!shown.IsShown)
                    {
                        label.style.display = DisplayStyle.Flex;
                        shown.IsShown = true;
                    }
                }
                else if (shown.IsShown)
                {
                    label.style.display = DisplayStyle.None;
                    shown.IsShown = false;
                }
            }
        }

        // A label's point, and the style values last written to the label (it starts hidden, with no position yet)
        private class LabelAnchor
        {
            public Transform Anchor;
            public bool IsShown;
            public float Left = float.NaN;
            public float Top = float.NaN;
        }
    }
}
