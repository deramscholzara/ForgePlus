﻿using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Palette;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPInteractiveSurfacePolygon : FPInteractiveSurfaceBase
    {
        public FPPolygon ParentFPPolygon = null;
        public FPPolygon.DataSources DataSource;
        public ShapeDescriptor surfaceShapeDescriptor = ShapeDescriptor.Empty;
        public FPLight FPLight = null;
        public FPMedia FPMedia = null;
        public FPPlatform FPPlatform = null;

        private UVPlanarDrag uvDragPlane;
        private bool endDragShouldRemergeBatch = false;

        private List<FPPolygon> alignmentGroupedPolygons = new List<FPPolygon>();
        private List<FPInteractiveSurfacePolygon> alignmentGroupedSurfaces = new List<FPInteractiveSurfacePolygon>();

        public override void OnValidatedPointerClick(PointerEventData eventData)
        {
            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    SelectionManager.Instance.ToggleObjectSelection(ParentFPPolygon, multiSelect: false);

                    break;
                case ModeManager.PrimaryModes.Textures:
                    if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
                    {
                        var selectedTexture = PaletteManager.Instance.GetSelectedTexture();

                        if (!selectedTexture.IsEmpty())
                        {
                            ParentFPPolygon.SetShapeDescriptor(GetComponent<RuntimeSurfaceLight>(), DataSource, selectedTexture);
                        }
                    }
                    else if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing &&
                             Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        var selectedSourceObject = SelectionManager.Instance.SelectedObject;
                        var selectedSourceFPPolygon = (selectedSourceObject is FPPolygon) ? selectedSourceObject as FPPolygon : null;

                        if (selectedSourceFPPolygon && selectedSourceFPPolygon != ParentFPPolygon)
                        {
                            ParentFPPolygon.SetOffset(this,
                                                      DataSource,
                                                      DataSource == FPPolygon.DataSources.Floor ? selectedSourceFPPolygon.WelandObject.FloorOrigin.X : selectedSourceFPPolygon.WelandObject.CeilingOrigin.X,
                                                      DataSource == FPPolygon.DataSources.Floor ? selectedSourceFPPolygon.WelandObject.FloorOrigin.Y : selectedSourceFPPolygon.WelandObject.CeilingOrigin.Y,
                                                      rebatch: true);
                        }
                    }
                    else
                    {
                        SelectionManager.Instance.ToggleObjectSelection(ParentFPPolygon, multiSelect: false);
                        InputListener(ParentFPPolygon);

                        if (!surfaceShapeDescriptor.IsEmpty())
                        {
                            PaletteManager.Instance.SelectSwatchForTexture(surfaceShapeDescriptor, invokeToggleEvents: false);
                        }
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
                    return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public override void OnValidatedBeginDrag(PointerEventData eventData)
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

                endDragShouldRemergeBatch = runtimeSurfaceLight.UnmergeBatch();

                // Note: Polygon surfaces have swapped UVs, so swap them here
                var startingUVs = DataSource == FPPolygon.DataSources.Floor ?
                                  new Vector2(ParentFPPolygon.WelandObject.FloorOrigin.Y, ParentFPPolygon.WelandObject.FloorOrigin.X) :
                                  new Vector2(ParentFPPolygon.WelandObject.CeilingOrigin.Y, ParentFPPolygon.WelandObject.CeilingOrigin.X);

                var startingPosition = eventData.pointerPressRaycast.worldPosition;

                // Even for floor normals, use down here, because floors have U-flipped UVs 
                var surfaceWorldNormal = Vector3.down;

                var textureWorldUp = Vector3.left;

                uvDragPlane = new UVPlanarDrag(startingUVs,
                                               startingPosition,
                                               surfaceWorldNormal,
                                               textureWorldUp);

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    alignmentGroupedPolygons.Clear();
                    alignmentGroupedSurfaces.Clear();

                    var commonElevation = DataSource == FPPolygon.DataSources.Floor ?
                                          ParentFPPolygon.WelandObject.FloorHeight :
                                          ParentFPPolygon.WelandObject.CeilingHeight;

                    var commonShapeDescriptor = DataSource == FPPolygon.DataSources.Floor ?
                                                ParentFPPolygon.WelandObject.FloorTexture :
                                                ParentFPPolygon.WelandObject.CeilingTexture;

                    CollectSimilarAdjacentPolygons(ParentFPPolygon, commonElevation, commonShapeDescriptor);
                }
            }
            else
            {
                return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public override void OnValidatedDrag(PointerEventData eventData)
        {
            if (uvDragPlane != null &&
                ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                var screenPosition = new Vector3(eventData.position.x,
                                                 eventData.position.y,
                                                 0f);

                var pointerRay = Camera.main.ScreenPointToRay(screenPosition);

                var newUVOffset = uvDragPlane.UVDraggedPosition(pointerRay);

                // Note: Polygon surfaces have swapped UVs, so swap them here
                ParentFPPolygon.SetOffset(this,
                                          DataSource,
                                          (short)newUVOffset.y,
                                          (short)newUVOffset.x,
                                          rebatch: false);

                for (var i = 0; i < alignmentGroupedPolygons.Count; i++)
                {
                    alignmentGroupedPolygons[i].SetOffset(alignmentGroupedSurfaces[i],
                                                          DataSource,
                                                          (short)newUVOffset.y,
                                                          (short)newUVOffset.x,
                                                          rebatch: false);
                }
            }
            else
            {
                return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public override void OnValidatedEndDrag(PointerEventData eventData)
        {
            if (uvDragPlane != null &&
                ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                uvDragPlane = null;

                if (endDragShouldRemergeBatch)
                {
                    endDragShouldRemergeBatch = false;
                    GetComponent<RuntimeSurfaceLight>().MergeBatch();

                    foreach (var alignmentGroupedSurface in alignmentGroupedSurfaces)
                    {
                        alignmentGroupedSurface.GetComponent<RuntimeSurfaceLight>().MergeBatch();
                    }
                }

                alignmentGroupedPolygons.Clear();
                alignmentGroupedSurfaces.Clear();
            }
            else
            {
                return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public override void OnDirectionalInputDown(Vector2 direction)
        {
            base.OnDirectionalInputDown(direction);

            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Textures:
                    if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
                    {
                        var newX = (short)(direction.y * GeometryUtilities.UnitsPerTextureOffetNudge);
                        var newY = (short)(direction.x * GeometryUtilities.UnitsPerTextureOffetNudge);

                        var originalX = DataSource == FPPolygon.DataSources.Floor ? ParentFPPolygon.WelandObject.FloorOrigin.X : ParentFPPolygon.WelandObject.CeilingOrigin.X;
                        var originalY = DataSource == FPPolygon.DataSources.Floor ? ParentFPPolygon.WelandObject.FloorOrigin.Y : ParentFPPolygon.WelandObject.CeilingOrigin.Y;

                        newX += (short)(Mathf.Round(originalX / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                        newY += (short)(Mathf.Round(originalY / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

                        ParentFPPolygon.SetOffset(this,
                                                  DataSource,
                                                  newX,
                                                  newY,
                                                  rebatch: true);
                    }

                    break;
                default:
                    return;
            }
        }

        private void CollectSimilarAdjacentPolygons(FPPolygon centralPolygon, short commonElevation, ShapeDescriptor commonShapeDescriptor)
        {
            for (var i = 0; i < Polygon.MaxVertexCount; i++)
            {
                var adjacentPolygonIndex = ParentFPPolygon.WelandObject.AdjacentPolygonIndexes[i];

                if (adjacentPolygonIndex < 0 || adjacentPolygonIndex == ParentFPPolygon.Index)
                {
                    continue;
                }

                var adjacentPolygon = FPLevel.Instance.FPPolygons[adjacentPolygonIndex];

                if (alignmentGroupedPolygons.Contains(adjacentPolygon))
                {
                    continue;
                }

                var adjacentElevation = DataSource == FPPolygon.DataSources.Floor ?
                                        adjacentPolygon.WelandObject.FloorHeight :
                                        adjacentPolygon.WelandObject.CeilingHeight;

                if (adjacentElevation != commonElevation)
                {
                    continue;
                }

                var adjacentShapeDescriptor = DataSource == FPPolygon.DataSources.Floor ?
                                              adjacentPolygon.WelandObject.FloorTexture :
                                              adjacentPolygon.WelandObject.CeilingTexture;

                if (!adjacentShapeDescriptor.Equals(commonShapeDescriptor))
                {
                    continue;
                }

                var alignmentGroupedSurface = DataSource == FPPolygon.DataSources.Floor ?
                                              adjacentPolygon.FloorSurface.GetComponent<FPInteractiveSurfacePolygon>() :
                                              adjacentPolygon.CeilingSurface.GetComponent<FPInteractiveSurfacePolygon>();

                alignmentGroupedPolygons.Add(adjacentPolygon);
                alignmentGroupedSurfaces.Add(alignmentGroupedSurface);

                alignmentGroupedSurface.GetComponent<RuntimeSurfaceLight>().UnmergeBatch();

                CollectSimilarAdjacentPolygons(adjacentPolygon, commonElevation, commonShapeDescriptor);
            }
        }
    }
}
