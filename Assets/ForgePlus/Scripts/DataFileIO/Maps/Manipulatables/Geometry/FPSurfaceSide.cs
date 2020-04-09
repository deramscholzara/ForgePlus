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
                // TODO: make this select the light in Lights mode
                SelectionManager.Instance.ToggleObjectSelection(parentFPSide, multiSelect: false);
            }
        }
    }
}
