using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A pop-up over the whole UI, below a field (or above it, where there's more room). One is open at a time, and
    // clicking outside it (or pressing escape) closes it.
    [AutoStaticsCleanup]
    public static partial class PopupLayer
    {
        private static VisualElement openLayer;

        // The pop-up's box, or null if there's no UI to open it in
        public static ScrollView Open(VisualElement anchor, float minimumWidth, float maximumHeight)
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
            layer.Add(box);

            root.Add(layer);
            openLayer = layer;

            // At least as wide as the field
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

        public static void Close()
        {
            openLayer?.RemoveFromHierarchy();
            openLayer = null;
        }
    }
}
