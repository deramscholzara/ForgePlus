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
                switch (ModeManager.Instance.PrimaryMode)
                {
                    case ModeManager.PrimaryModes.Geometry:
                        SelectionManager.Instance.ToggleObjectSelection(ParentFPPolygon, multiSelect: false);
                        break;
                    case ModeManager.PrimaryModes.Lights:
                        SelectionManager.Instance.ToggleObjectSelection(FPLight, multiSelect: false);
                        PaletteManager.Instance.SelectSwatchForLight(FPLight, invokeToggleEvents: false);
                        break;
                    case ModeManager.PrimaryModes.Media:
                        if (FPMedia != null)
                        {
                            SelectionManager.Instance.ToggleObjectSelection(FPMedia, multiSelect: false);
                            PaletteManager.Instance.SelectSwatchForMedia(FPMedia, invokeToggleEvents: false);
                        }

                        break;
                    default:
                        Debug.LogError($"Selection in mode \"{ModeManager.Instance.PrimaryMode}\" is not supported.");
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
