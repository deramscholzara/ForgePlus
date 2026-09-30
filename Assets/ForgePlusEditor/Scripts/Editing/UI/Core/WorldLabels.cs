using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Labels for points in the level, like name tags: screen-space text centered on its point, in a layer behind all the
    // panels. A label is shown only while its point is in the camera's view (and its anchor is active), and clicking it
    // calls back its owner (such as to select it).
    public class WorldLabels
    {
        private readonly VisualElement layer;
        private readonly Dictionary<Label, Transform> anchors = new Dictionary<Label, Transform>();

        public WorldLabels(VisualElement root)
        {
            layer = new VisualElement { name = "world-labels", pickingMode = PickingMode.Ignore };
            layer.AddToClassList("fp-world-labels");

            // First, so every panel is drawn over it
            root.Insert(0, layer);
        }

        public Label Add(Transform anchor, Action onClick)
        {
            // Plain text (level text isn't markup)
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
                var isShown = false;

                if (camera && panel != null && anchor && anchor.gameObject.activeInHierarchy)
                {
                    // Behind the camera, or outside its view (even if some of the text would be in it), isn't shown
                    var screenPosition = camera.WorldToScreenPoint(anchor.position);
                    if (screenPosition.z > 0f && camera.pixelRect.Contains(screenPosition))
                    {
                        var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
                        label.style.left = panelPosition.x;
                        label.style.top = panelPosition.y;
                        isShown = true;
                    }
                }

                label.style.display = isShown ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
