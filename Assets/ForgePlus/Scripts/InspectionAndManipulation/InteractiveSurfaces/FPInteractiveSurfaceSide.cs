using ForgePlus.Palette;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPInteractiveSurfaceSide : FPInteractiveSurfaceBase
    {
        public FPSide ParentFPSide = null;
        public FPSide.SideDataSources DataSource;
        public ShapeDescriptor surfaceShapeDescriptor = ShapeDescriptor.Empty;
        public FPLight FPLight = null;
        public FPMedia FPMedia = null;
        public FPPlatform FPPlatform = null;

        private UVPlanarDrag uvDragPlane;

        public override void OnPointerClickValidated(PointerEventData eventData)
        {
            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    SelectionManager.Instance.ToggleObjectSelection(ParentFPSide, multiSelect: false);
                    break;
                case ModeManager.PrimaryModes.Textures:
                    SelectionManager.Instance.ToggleObjectSelection(ParentFPSide, multiSelect: false);

                    if ((ushort)surfaceShapeDescriptor != (ushort)ShapeDescriptor.Empty)
                    {
                        PaletteManager.Instance.SelectSwatchForTexture(surfaceShapeDescriptor, invokeToggleEvents: false);
                    }

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
                case ModeManager.PrimaryModes.Platforms:
                    if (FPPlatform != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(FPPlatform, multiSelect: false);
                    }

                    break;
                default:
                    Debug.LogError($"Selection in mode \"{ModeManager.Instance.PrimaryMode}\" is not supported.");
                    break;
            }
        }

        public override void OnBeginDragValidated(PointerEventData eventData)
        {
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                var runtimeSurfaceLight = GetComponent<RuntimeSurfaceLight>();

                if (!runtimeSurfaceLight)
                {
                    // If there's no light on this surface,
                    // it's either a landscape or an unassigned surface,
                    // so don't bother with offset adjustment.
                    return;
                }

                runtimeSurfaceLight.UnmergeBatch();

                Vector2 startingUVs;

                switch (DataSource)
                {
                    case FPSide.SideDataSources.Primary:
                        startingUVs = new Vector2(ParentFPSide.WelandObject.Primary.X, ParentFPSide.WelandObject.Primary.Y);
                        break;
                    case FPSide.SideDataSources.Secondary:
                        startingUVs = new Vector2(ParentFPSide.WelandObject.Secondary.X, ParentFPSide.WelandObject.Secondary.Y);
                        break;
                    case FPSide.SideDataSources.Transparent:
                        startingUVs = new Vector2(ParentFPSide.WelandObject.Transparent.X, ParentFPSide.WelandObject.Transparent.Y);
                        break;
                    default:
                        return;
                }

                var startingPosition = eventData.pointerPressRaycast.worldPosition;

                var surfaceWorldNormal = eventData.pointerCurrentRaycast.worldNormal;

                var textureWorldUp = Vector3.up;

                uvDragPlane = new UVPlanarDrag(startingUVs,
                                                startingPosition,
                                                surfaceWorldNormal,
                                                textureWorldUp);
            }
        }

        public override void OnDragValidated(PointerEventData eventData)
        {
            // TODO: Why doesn't this work when dragging over empty space?  It's because no proper pointer is automatically generated when not hitting anything.
            if (uvDragPlane != null &&
                ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                var screenPosition = new Vector3(eventData.pointerCurrentRaycast.screenPosition.x,
                                                 eventData.pointerCurrentRaycast.screenPosition.y,
                                                 0f);

                var pointerRay = Camera.main.ScreenPointToRay(screenPosition);

                var newUVOffset = uvDragPlane.UVDraggedPosition(pointerRay);

                ParentFPSide.SetOffset(this,
                                       DataSource,
                                       uvChannel: 0, // TODO: Figure out how to allow for drag-edit on overlay TransparentSides (uv channel 2)?  Hotkey when drag begins?
                                       (short)newUVOffset.x,
                                       (short)newUVOffset.y,
                                       rebatch: false);
            }
        }

        public override void OnEndDragValidated(PointerEventData eventData)
        {
            if (uvDragPlane != null &&
                ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                uvDragPlane = null;

                GetComponent<RuntimeSurfaceLight>().MergeBatch();
            }
        }
    }
}
