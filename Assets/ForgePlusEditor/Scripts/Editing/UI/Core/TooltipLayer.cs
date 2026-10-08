using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Shows the tooltip of what's under the pointer (or of the nearest element around it with one), once the pointer
    // has rested on it for a moment. Runtime panels keep elements' tooltips but never show them, as the Editor's do.
    public class TooltipLayer
    {
        private const long ShowDelayMilliseconds = 500;

        // How far below the pointer it's shown (so the cursor doesn't cover it)
        private const float PointerOffset = 20f;

        private readonly VisualElement root;
        private readonly VisualElement layerRoot;
        private readonly Label label;

        private VisualElement owner;
        private Vector2 pointerPosition;
        private IVisualElementScheduledItem pendingShow;

        // Pressing hides the tooltip until the pointer moves onto something else
        private bool isSuppressed;

        public TooltipLayer(VisualElement root)
        {
            this.root = root;
            layerRoot = root.Q(className: "fp-root") ?? root;

            // Tooltips aren't markup
            label = new Label { enableRichText = false, pickingMode = PickingMode.Ignore };
            label.AddToClassList("fp-tooltip");
            label.style.display = DisplayStyle.None;
            label.RegisterCallback<GeometryChangedEvent>(geometryChangedEvent => KeepInBounds());
            layerRoot.Add(label);

            // Trickling down, so the root hears about every element the pointer is over
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(pointerDownEvent => Suppress(), TrickleDown.TrickleDown);
            root.RegisterCallback<WheelEvent>(wheelEvent => Suppress(), TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(pointerLeaveEvent => SetOwner(null));
        }

        private void OnPointerMove(PointerMoveEvent pointerMoveEvent)
        {
            pointerPosition = pointerMoveEvent.position;
            SetOwner(TooltipOwner(pointerMoveEvent.target as VisualElement));
        }

        // The innermost element with a tooltip (ignoring a "@Table/Key" that hasn't been localized yet)
        private static VisualElement TooltipOwner(VisualElement element)
        {
            for (; element != null; element = element.parent)
            {
                if (!string.IsNullOrEmpty(element.tooltip) && element.tooltip[0] != '@')
                {
                    return element;
                }
            }

            return null;
        }

        private void SetOwner(VisualElement newOwner)
        {
            if (newOwner == owner)
            {
                return;
            }

            Hide();
            owner = newOwner;
            isSuppressed = false;

            if (owner != null)
            {
                pendingShow = root.schedule.Execute(Show).StartingIn(ShowDelayMilliseconds);
            }
        }

        private void Suppress()
        {
            Hide();
            isSuppressed = true;
        }

        private void Show()
        {
            pendingShow = null;
            if (isSuppressed || owner?.panel == null || string.IsNullOrEmpty(owner.tooltip))
            {
                return;
            }

            var position = layerRoot.WorldToLocal(pointerPosition);
            label.text = owner.tooltip;
            label.style.left = position.x;
            label.style.top = position.y + PointerOffset;
            label.style.display = DisplayStyle.Flex;

            // Over pop-ups and dialogs opened since
            label.BringToFront();
        }

        private void Hide()
        {
            pendingShow?.Pause();
            pendingShow = null;
            label.style.display = DisplayStyle.None;
        }

        // Once its size is known: shifted left to fit, and shown above the pointer where there's no room below
        private void KeepInBounds()
        {
            if (label.resolvedStyle.display == DisplayStyle.None)
            {
                return;
            }

            var bounds = layerRoot.layout;
            var size = label.layout.size;
            var position = layerRoot.WorldToLocal(pointerPosition);

            var left = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, bounds.width - size.x));
            var top = position.y + PointerOffset;
            if (top + size.y > bounds.height)
            {
                top = Mathf.Max(0f, position.y - size.y - 5f);
            }

            if (!Mathf.Approximately(label.style.left.value.value, left) || !Mathf.Approximately(label.style.top.value.value, top))
            {
                label.style.left = left;
                label.style.top = top;
            }
        }
    }
}
