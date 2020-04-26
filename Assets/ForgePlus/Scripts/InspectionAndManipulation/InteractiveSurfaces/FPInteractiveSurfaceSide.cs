using ForgePlus.Palette;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPInteractiveSurfaceSide : FPInteractiveSurfaceBase
    {
        public FPSide ParentFPSide = null;
        public ShapeDescriptor surfaceShapeDescriptor = ShapeDescriptor.Empty;
        public FPLight FPLight = null;
        public FPMedia FPMedia = null;
        public FPPlatform FPPlatform = null;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerId == -1 && !eventData.dragging && isSelectable)
            {
                switch (SelectionManager.Instance.CurrentSceneSelectionFilter)
                {
                    case SelectionManager.SceneSelectionFilters.Geometry:
                        SelectionManager.Instance.ToggleObjectSelection(ParentFPSide, multiSelect: false);
                        break;
                    case SelectionManager.SceneSelectionFilters.Textures:
                        if ((ushort)surfaceShapeDescriptor != (ushort)ShapeDescriptor.Empty)
                        {
                            SelectionManager.Instance.ToggleObjectSelection(ParentFPSide, multiSelect: false);
                            PaletteManager.Instance.SelectSwatchForTexture(surfaceShapeDescriptor);
                        }

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
                    case SelectionManager.SceneSelectionFilters.Platforms:
                        if (FPPlatform != null)
                        {
                            SelectionManager.Instance.ToggleObjectSelection(FPPlatform, multiSelect: false);
                        }

                        break;
                    default:
                        Debug.LogError($"Selection in mode \"{SelectionManager.Instance.CurrentSceneSelectionFilter}\" is not supported.");
                        break;
                }
            }
        }
    }
}
