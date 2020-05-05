using ForgePlus.ApplicationGeneral;
using ForgePlus.Palette;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPInteractiveSurfacePolygon : FPInteractiveSurfaceBase
    {
        public FPPolygon ParentFPPolygon = null;
        public bool IsFloor;
        public ShapeDescriptor surfaceShapeDescriptor = ShapeDescriptor.Empty;
        public FPLight FPLight = null;
        public FPMedia FPMedia = null;
        public FPPlatform FPPlatform = null;

        private UVPlanarDrag uvDragPlane;
        private SurfaceBatchingManager.RuntimeSurfaceMaterialKey surfaceBatchKey;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerId == -1 && !eventData.dragging && isSelectable)
            {
                switch (ModeManager.Instance.PrimaryMode)
                {
                    case ModeManager.PrimaryModes.Geometry:
                        SelectionManager.Instance.ToggleObjectSelection(ParentFPPolygon, multiSelect: false);
                        break;
                    case ModeManager.PrimaryModes.Textures:
                        SelectionManager.Instance.ToggleObjectSelection(ParentFPPolygon, multiSelect: false);

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
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                base.OnBeginDrag(eventData);

                if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                    ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
                {
                    surfaceBatchKey = GetComponent<RuntimeSurfaceLight>().BatchingKey;

                    SurfaceBatchingManager.Instance.UnmergeBatch(surfaceBatchKey);

                    var startingU = IsFloor ? ParentFPPolygon.WelandObject.FloorOrigin.X : ParentFPPolygon.WelandObject.CeilingOrigin.X;
                    var startingV = IsFloor ? ParentFPPolygon.WelandObject.FloorOrigin.Y : ParentFPPolygon.WelandObject.CeilingOrigin.Y;
                    var startingUVs = new Vector2(startingU, startingV);

                    var startingPosition = eventData.pointerPressRaycast.worldPosition;

                    // Even for floor normals, use down here, because floors have U-flipped UVs 
                    var surfaceWorldNormal = Vector3.down; // TODO: Sides should pass actual Normal

                    var textureWorldUp = Vector3.left; // TODO: Sides should pass Vector3.up

                    uvDragPlane = new UVPlanarDrag(startingUVs,
                                                   startingPosition,
                                                   surfaceWorldNormal,
                                                   textureWorldUp);
                }
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            // TODO: Why doesn't this work when dragging over empty space?  Needs update method like SelectionManager?
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                base.OnDrag(eventData);

                if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                    ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
                {
                    var screenPosition = new Vector3(eventData.pointerCurrentRaycast.screenPosition.x,
                                                     eventData.pointerCurrentRaycast.screenPosition.y,
                                                     0f);

                    var pointerRay = Camera.main.ScreenPointToRay(screenPosition);

                    var newUVOffset = uvDragPlane.UVDraggedPosition(pointerRay);

                    ParentFPPolygon.SetOffset(IsFloor ? FPPolygon.PolygonSurfaceTypes.Floor : FPPolygon.PolygonSurfaceTypes.Ceiling,
                                                        uvChannel: 0,
                                                        (short)newUVOffset.x,
                                                        (short)newUVOffset.y,
                                                        rebatch: false);

                    // TODO: Figure out how to allow for drag-edit on overly TransparentSides?  Hotkey when drag begins?
                }
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                base.OnEndDrag(eventData);

                if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                    ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
                {
                    uvDragPlane = null;

                    SurfaceBatchingManager.Instance.MergeBatch(surfaceBatchKey);
                }
            }
        }
    }
}
