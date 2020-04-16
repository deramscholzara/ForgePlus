using ForgePlus.Palette;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public class FPInteractiveSurfaceMedia : FPInteractiveSurfaceBase
    {
        public FPPolygon ParentFPPolygon = null;
        public FPLight FPLight = null;
        public FPMedia FPMedia = null;

        public override void OnMouseUpAsButton()
        {
            if (isSelectable)
            {
                // TODO: Make this select the media itself, if in Medias mode
                switch (SelectionManager.Instance.CurrentSceneSelectionFilter)
                {
                    case SelectionManager.SceneSelectionFilters.Geometry:
                        SelectionManager.Instance.ToggleObjectSelection(ParentFPPolygon, multiSelect: false);
                        break;
                    case SelectionManager.SceneSelectionFilters.Lights:
                        PaletteManager.Instance.SelectSwatchForLight(FPLight);
                        break;
                    case SelectionManager.SceneSelectionFilters.Media:
                        if (FPMedia != null)
                        {
                            PaletteManager.Instance.SelectSwatchForMedia(FPMedia);
                        }

                        break;
                    default:
                        Debug.LogError($"Selection in mode \"{SelectionManager.Instance.CurrentSceneSelectionFilter}\" is not supported.");
                        break;
                }
            }
        }

        public override void SetSelectability(bool enabled)
        {
            base.SetSelectability(enabled);
            GetComponent<MeshCollider>().enabled = enabled;
        }
    }
}
