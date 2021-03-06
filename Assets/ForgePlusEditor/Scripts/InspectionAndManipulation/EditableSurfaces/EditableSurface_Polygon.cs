﻿using ForgePlus.ApplicationGeneral;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Palette;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class EditableSurface_Polygon : EditableSurface_Base
    {
        public LevelEntity_Polygon ParentPolygon = null;
        public LevelEntity_Polygon.DataSources DataSource;

        // TODO: Get rid of these and just attain them on the fly instead of preloading
        //       Maybe include a reference to the context-typed RuntimeSurfaceGeometry component, to help
        public ShapeDescriptor surfaceShapeDescriptor = ShapeDescriptor.Empty;
        public LevelEntity_Light RuntimeLight = null;
        public LevelEntity_Media Media = null;
        public LevelEntity_Platform Platform = null;

        private UVPlanarDrag uvDragPlane;

        private readonly List<LevelEntity_Polygon> alignmentGroupedPolygons = new List<LevelEntity_Polygon>();

        public override void OnValidatedPointerClick(PointerEventData eventData)
        {
            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    SelectionManager.Instance.ToggleObjectSelection(ParentPolygon, multiSelect: false);

                    break;
                case ModeManager.PrimaryModes.Textures:
                    if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
                    {
                        var selectedTexture = PaletteManager.Instance.GetSelectedTexture();

                        if (!selectedTexture.IsEmpty())
                        {
                            ParentPolygon.SetShapeDescriptor(DataSource, selectedTexture);
                        }
                    }
                    else if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing &&
                             Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        var selectedSourceObject = SelectionManager.Instance.SelectedObject;
                        var selectedSourcePolygon = (selectedSourceObject is LevelEntity_Polygon) ? selectedSourceObject as LevelEntity_Polygon : null;

                        if (selectedSourcePolygon && selectedSourcePolygon != ParentPolygon)
                        {
                            ParentPolygon.SetOffset(DataSource,
                                                      DataSource == LevelEntity_Polygon.DataSources.Floor ? selectedSourcePolygon.NativeObject.FloorOrigin.X : selectedSourcePolygon.NativeObject.CeilingOrigin.X,
                                                      DataSource == LevelEntity_Polygon.DataSources.Floor ? selectedSourcePolygon.NativeObject.FloorOrigin.Y : selectedSourcePolygon.NativeObject.CeilingOrigin.Y,
                                                      rebatch: true);
                        }
                    }
                    else
                    {
                        SelectionManager.Instance.ToggleObjectSelection(ParentPolygon, multiSelect: false);
                        InputListener(ParentPolygon);

                        if (!surfaceShapeDescriptor.IsEmpty())
                        {
                            PaletteManager.Instance.SelectSwatchForTexture(surfaceShapeDescriptor, invokeToggleEvents: false);
                        }
                    }

                    break;
                case ModeManager.PrimaryModes.Lights:
                    if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
                    {
                        var selectedLight = PaletteManager.Instance.GetSelectedLight();

                        if (selectedLight != null)
                        {
                            ParentPolygon.SetLight(DataSource, selectedLight.NativeIndex);
                        }
                    }
                    else
                    {
                        SelectionManager.Instance.ToggleObjectSelection(RuntimeLight, multiSelect: false);
                        PaletteManager.Instance.SelectSwatchForLight(RuntimeLight, invokeToggleEvents: false);
                    }

                    break;
                case ModeManager.PrimaryModes.Media:
                    if (Media != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(Media, multiSelect: false);
                        PaletteManager.Instance.SelectSwatchForMedia(Media, invokeToggleEvents: false);
                    }

                    break;
                case ModeManager.PrimaryModes.Platforms:
                    if (Platform != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(Platform, multiSelect: false);
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
                // Note: Polygon surfaces have swapped UVs, so swap them here
                var startingUVs = DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                  new Vector2(ParentPolygon.NativeObject.FloorOrigin.Y, ParentPolygon.NativeObject.FloorOrigin.X) :
                                  new Vector2(ParentPolygon.NativeObject.CeilingOrigin.Y, ParentPolygon.NativeObject.CeilingOrigin.X);

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

                    var commonElevation = DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                          ParentPolygon.NativeObject.FloorHeight :
                                          ParentPolygon.NativeObject.CeilingHeight;

                    var commonShapeDescriptor = DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                                ParentPolygon.NativeObject.FloorTexture :
                                                ParentPolygon.NativeObject.CeilingTexture;

                    CollectSimilarAdjacentPolygons(ParentPolygon, commonElevation, commonShapeDescriptor);
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
                ParentPolygon.SetOffset(DataSource,
                                          (short)newUVOffset.y,
                                          (short)newUVOffset.x,
                                          rebatch: false);

                for (var i = 0; i < alignmentGroupedPolygons.Count; i++)
                {
                    alignmentGroupedPolygons[i].SetOffset(DataSource,
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

                if (SurfaceBatchingManager.BatchingEnabled)
                {
                    SurfaceBatchingManager.Instance.MergeAllBatches();
                }

                alignmentGroupedPolygons.Clear();
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

                        var originalX = DataSource == LevelEntity_Polygon.DataSources.Floor ? ParentPolygon.NativeObject.FloorOrigin.X : ParentPolygon.NativeObject.CeilingOrigin.X;
                        var originalY = DataSource == LevelEntity_Polygon.DataSources.Floor ? ParentPolygon.NativeObject.FloorOrigin.Y : ParentPolygon.NativeObject.CeilingOrigin.Y;

                        newX += (short)(Mathf.Round(originalX / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);
                        newY += (short)(Mathf.Round(originalY / GeometryUtilities.UnitsPerTextureOffetNudge) * GeometryUtilities.UnitsPerTextureOffetNudge);

                        ParentPolygon.SetOffset(DataSource,
                                                  newX,
                                                  newY,
                                                  rebatch: true);
                    }

                    break;
                default:
                    return;
            }
        }

        private void CollectSimilarAdjacentPolygons(LevelEntity_Polygon centralPolygon, short commonElevation, ShapeDescriptor commonShapeDescriptor)
        {
            for (var i = 0; i < Polygon.MaxVertexCount; i++)
            {
                var adjacentPolygonIndex = ParentPolygon.NativeObject.AdjacentPolygonIndexes[i];

                if (adjacentPolygonIndex < 0 || adjacentPolygonIndex == ParentPolygon.NativeIndex)
                {
                    continue;
                }

                var adjacentPolygon = LevelEntity_Level.Instance.Polygons[adjacentPolygonIndex];

                if (alignmentGroupedPolygons.Contains(adjacentPolygon))
                {
                    continue;
                }

                var adjacentElevation = DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                        adjacentPolygon.NativeObject.FloorHeight :
                                        adjacentPolygon.NativeObject.CeilingHeight;

                if (adjacentElevation != commonElevation)
                {
                    continue;
                }

                var adjacentShapeDescriptor = DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                              adjacentPolygon.NativeObject.FloorTexture :
                                              adjacentPolygon.NativeObject.CeilingTexture;

                if (!adjacentShapeDescriptor.Equals(commonShapeDescriptor))
                {
                    continue;
                }

                var alignmentGroupedSurface = DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                              adjacentPolygon.FloorSurface.GetComponent<EditableSurface_Polygon>() :
                                              adjacentPolygon.CeilingSurface.GetComponent<EditableSurface_Polygon>();

                alignmentGroupedPolygons.Add(adjacentPolygon);

                CollectSimilarAdjacentPolygons(adjacentPolygon, commonElevation, commonShapeDescriptor);
            }
        }
    }
}
