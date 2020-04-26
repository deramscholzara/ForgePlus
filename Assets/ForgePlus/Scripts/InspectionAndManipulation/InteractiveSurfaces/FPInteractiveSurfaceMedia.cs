using ForgePlus.Palette;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgePlus.LevelManipulation
{
    public class FPInteractiveSurfaceMedia : FPInteractiveSurfaceBase
    {
        public FPPolygon ParentFPPolygon = null;
        public FPLight FPLight = null;
        public FPMedia FPMedia = null;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerId == -1 && !eventData.dragging && isSelectable)
            {
                // TODO: Make this select the media itself, if in Medias mode
                switch (SelectionManager.Instance.CurrentSceneSelectionFilter)
                {
                    case SelectionManager.SceneSelectionFilters.Geometry:
                        SelectionManager.Instance.ToggleObjectSelection(ParentFPPolygon, multiSelect: false);
                        break;
                    case SelectionManager.SceneSelectionFilters.Lights:
                        SelectionManager.Instance.ToggleObjectSelection(FPLight, multiSelect: false);
                        PaletteManager.Instance.SelectSwatchForLight(FPLight, invokeToggleEvents: false);
                        break;
                    case SelectionManager.SceneSelectionFilters.Media:
                        if (FPMedia != null)
                        {
                            SelectionManager.Instance.ToggleObjectSelection(FPMedia, multiSelect: false);
                            PaletteManager.Instance.SelectSwatchForMedia(FPMedia, invokeToggleEvents: false);
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
