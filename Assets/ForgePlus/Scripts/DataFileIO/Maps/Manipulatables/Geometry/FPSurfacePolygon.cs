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
                // TODO: make this select the light in Lights mode
                SelectionManager.Instance.ToggleObjectSelection(parentFPPolygon, multiSelect: false);
            }
        }
    }
}
