using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A small button showing a play triangle (in its text color). Its presses stay its own, so a swatch (a toggle) it's
    // in isn't also clicked.
    [UxmlElement]
    public partial class PlayButton : Button
    {
        public PlayButton()
        {
            generateVisualContent += DrawTriangle;

            RegisterCallback<PointerDownEvent>(StopPropagation);
            RegisterCallback<PointerUpEvent>(StopPropagation);
            RegisterCallback<ClickEvent>(StopPropagation);
            RegisterCallback<MouseDownEvent>(StopPropagation);
            RegisterCallback<MouseUpEvent>(StopPropagation);
        }

        private static void StopPropagation(EventBase evt)
        {
            evt.StopPropagation();
        }

        // Pointing right, centered, and most of its height
        private void DrawTriangle(MeshGenerationContext context)
        {
            var rect = contentRect;
            var size = Mathf.Min(rect.width, rect.height) * 0.7f;
            if (size <= 0f)
            {
                return;
            }

            var center = rect.center;
            var painter = context.painter2D;

            painter.fillColor = resolvedStyle.color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x - size * 0.4f, center.y - size * 0.5f));
            painter.LineTo(new Vector2(center.x + size * 0.5f, center.y));
            painter.LineTo(new Vector2(center.x - size * 0.4f, center.y + size * 0.5f));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
