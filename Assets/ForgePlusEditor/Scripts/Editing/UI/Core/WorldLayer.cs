using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A screen-space layer, behind all the panels, for what's drawn at points in the level
    public static class WorldLayer
    {
        public static VisualElement Create(VisualElement root, string name)
        {
            var layer = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            layer.AddToClassList("fp-world-layer");

            // First, so every panel is drawn over it
            root.Insert(0, layer);

            return layer;
        }

        // A point behind the camera, or outside its view, has no position
        public static bool TryGetPanelPosition(Vector3 worldPosition, Camera camera, IPanel panel, out Vector2 position)
        {
            position = default;

            if (!camera || panel == null)
            {
                return false;
            }

            var screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f || !camera.pixelRect.Contains(screenPosition))
            {
                return false;
            }

            position = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            return true;
        }
    }
}
