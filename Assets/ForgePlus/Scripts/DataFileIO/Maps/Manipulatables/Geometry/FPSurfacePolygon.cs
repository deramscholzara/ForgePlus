using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public class FPSurfacePolygon : SurfaceBase
    {
        public FPPolygon parentFPPolygon = null;

        public override void OnMouseUpAsButton()
        {
            if (isSelectable)
            {
                SelectionManager.Instance.ToggleObjectSelection(parentFPPolygon, multiSelect: false);
            }
        }
    }
}
