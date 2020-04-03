using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public class FPSurfaceMedia : SurfaceBase
    {
        public FPPolygon parentFPPolygon = null;

        public override void OnMouseUpAsButton()
        {
            if (isSelectable)
            {
                SelectionManager.Instance.ToggleObjectSelection(parentFPPolygon, multiSelect: false);
            }
        }

        public override void SetSelectability(bool enabled)
        {
            GetComponent<MeshCollider>().enabled = enabled;
        }
    }
}
