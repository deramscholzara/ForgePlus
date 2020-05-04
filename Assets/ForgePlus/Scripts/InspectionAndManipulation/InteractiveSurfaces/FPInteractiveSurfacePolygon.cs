using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation.Utilities;
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
                    // TODO: only unmerge/merge the batch for this mesh - not all of them
                    SurfaceBatchingManager.Instance.UnmergeAllBatches();

                    var startingU = IsFloor ? ParentFPPolygon.WelandObject.FloorOrigin.X : ParentFPPolygon.WelandObject.CeilingOrigin.X;
                    var startingV = IsFloor ? ParentFPPolygon.WelandObject.FloorOrigin.Y : ParentFPPolygon.WelandObject.CeilingOrigin.Y;
                    var startingUVs = new Vector2(startingU, startingV);

                    var startingPosition = eventData.pointerPressRaycast.worldPosition;

                    // Use down even for floor normals here, because they have U-flipped UVs 
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
            // TODO: Why doesn't this work when dragging over empty space?
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

                    var newUVOffset = uvDragPlane.AdjustedUVs(pointerRay);

                    GetComponent<MeshFilter>().sharedMesh.SetUVs(0, ParentFPPolygon.GetUVs((short)newUVOffset.x, (short)newUVOffset.y));

                    // TODO: Tell the surface to apply to the WelandObject and fire a UV-only update to the surface mesh

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

                    // TODO: only unmerge/merge the batch for this mesh - not all of them
                    SurfaceBatchingManager.Instance.MergeAllBatches();
                }
            }
        }
    }

    public class UVPlanarDrag
    {
        private readonly Vector2 startingUVs;
        private readonly Matrix4x4 originLocalToWorldMatrix;
        private readonly Plane intersectionPlane;

        public UVPlanarDrag(Vector2 startingUVs, Vector3 startingPosition, Vector3 surfaceWorldNormal, Vector3 textureWorldUp)
        {
            this.startingUVs = startingUVs;

            originLocalToWorldMatrix = Matrix4x4.TRS(startingPosition,
                                         Quaternion.LookRotation(-surfaceWorldNormal, textureWorldUp),
                                         Vector3.one);

            intersectionPlane = new Plane(surfaceWorldNormal, startingPosition);
        }

        public Vector2 AdjustedUVs(Ray pointerRay)
        {
            if (intersectionPlane.Raycast(pointerRay, out float distanceToPlane))
            {
                var planarPosition = pointerRay.GetPoint(distanceToPlane);

                return AdjustedUVs(planarPosition);
            }
            else
            {
                return startingUVs;
            }
        }

        public Vector2 AdjustedUVs(Vector3 currentPosition)
        {
            var positionOffset = originLocalToWorldMatrix.inverse.MultiplyPoint(currentPosition);

            positionOffset.x = Mathf.Clamp(positionOffset.x, -16f, 16f);
            positionOffset.y = Mathf.Clamp(positionOffset.y, -16f, 16f);
            positionOffset.z = 0f;

            Debug.DrawLine(originLocalToWorldMatrix.MultiplyPoint(Vector3.zero), originLocalToWorldMatrix.MultiplyPoint(positionOffset));

            var uvOffset = new Vector2(positionOffset.y * GeometryUtilities.WorldUnitIncrementsPerMeter,
                                       -positionOffset.x * GeometryUtilities.WorldUnitIncrementsPerMeter);
            uvOffset += startingUVs;

            uvOffset.x %= GeometryUtilities.WorldUnitIncrementsPerWorldUnit;
            uvOffset.y %= GeometryUtilities.WorldUnitIncrementsPerWorldUnit;

            return uvOffset;
        }
    }
}
