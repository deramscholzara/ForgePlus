using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public class FPSurfaceSide : SurfaceBase
    {
        public FPSide parentFPSide = null;

        // TODO: Use this for selecting the platform this surface is parented to (in platforms tool mode)
        public FPPolygon parentFPPolygon = null;

        public override void OnMouseUpAsButton()
        {
            if (isSelectable)
            {
                SelectionManager.Instance.ToggleObjectSelection(parentFPSide, multiSelect: false);
            }
        }
    }
}
