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
                // TODO: Make this select the media itself, if in Medias mode
                // TODO: make this select the light in Lights mode
                SelectionManager.Instance.ToggleObjectSelection(parentFPPolygon, multiSelect: false);
            }
        }

        public override void SetSelectability(bool enabled)
        {
            GetComponent<MeshCollider>().enabled = enabled;
        }
    }
}
