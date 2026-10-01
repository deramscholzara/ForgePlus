using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A pop-up (such as a list to choose from) over the whole UI, below a field (or above it, where there's more room):
    // the layer it's on covers the UI, so clicking outside the pop-up (or pressing escape) closes it. One is open at a
    // time.
    public static class PopupLayer
    {
        private static VisualElement openLayer;

        // The pop-up's box (which its contents go in), or null if there's no UI to open it in
        public static ScrollView Open(VisualElement anchor, string boxClass, float minimumWidth, float maximumHeight)
        {
            Close();

            var root = anchor.panel?.visualTree.Q(className: "fp-root");
            if (root == null)
            {
                return null;
            }

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
            box.AddToClassList("fp-popup");
            box.AddToClassList(boxClass);
            layer.Add(box);

            root.Add(layer);
            openLayer = layer;

            // Below the field (or above it, if there's more room there), at least as wide as it
            var anchorBounds = anchor.worldBound;
            var topLeft = root.WorldToLocal(new Vector2(anchorBounds.xMin, anchorBounds.yMin));
            var bottomLeft = root.WorldToLocal(new Vector2(anchorBounds.xMin, anchorBounds.yMax));
            var rootHeight = root.layout.height;
            var roomBelow = rootHeight - bottomLeft.y;
            var roomAbove = topLeft.y;
            var opensBelow = roomBelow >= Mathf.Min(maximumHeight, roomAbove);
            var width = Mathf.Max(minimumWidth, anchorBounds.width);

            box.style.left = Mathf.Max(0f, Mathf.Min(topLeft.x, root.layout.width - width));
            box.style.width = width;
            box.style.maxHeight = Mathf.Min(maximumHeight, opensBelow ? roomBelow : roomAbove);
            if (opensBelow)
            {
                box.style.top = bottomLeft.y;
            }
            else
            {
                box.style.bottom = rootHeight - topLeft.y;
            }

            layer.Focus();

            return box;
        }

        // Whether the pop-up the box is in is still open
        public static bool IsOpen(VisualElement box)
        {
            return openLayer != null && box != null && box.parent == openLayer;
        }

        public static void Close()
        {
            openLayer?.RemoveFromHierarchy();
            openLayer = null;
        }
    }
}
