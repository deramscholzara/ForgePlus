using ForgePlus.Palette;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgePlus.LevelManipulation
{
    public class EditableSurface_Media : EditableSurface_Base
    {
        public LevelEntity_Media FPMedia = null;

        // TODO: Get rid of these and just attain them on the fly instead of preloading
        //       Maybe include a reference to the context-typed RuntimeSurfaceGeometry component, to help
        public LevelEntity_Polygon ParentFPPolygon = null;
        public LevelEntity_Light FPLight = null;

        public override void OnValidatedPointerClick(PointerEventData eventData)
        {
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

        public override void OnValidatedBeginDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedEndDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void SetSelectability(bool enabled)
        {
            base.SetSelectability(enabled);

            GetComponent<MeshCollider>().enabled = enabled;
        }
    }
}
