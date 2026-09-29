using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Extensions;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Palette;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgePlus.LevelManipulation
{
    public class EditableSurface_Polygon : EditableSurface_Base
    {
        public LevelEntity_Polygon ParentPolygon = null;
        public LevelEntity_Polygon.DataSources DataSource;

        // TODO: Get rid of these and just attain them on the fly instead of preloading
        //       Maybe include a reference to the context-typed RuntimeSurfaceGeometry component, to help
        public ushort surfaceShapeDescriptor = cstypes.UNONE;
        [System.NonSerialized]
        public LevelEntity_Light RuntimeLight = null;
        [System.NonSerialized]
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

                        if (!selectedTexture.IsEmptyShapeDescriptor())
                        {
                            ParentPolygon.SetShapeDescriptor(DataSource, selectedTexture);
                        }
                    }
                    else if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing &&
                             ForgePlusInput.Editing.AlignToSelection.IsPressed())
                    {
                        var selectedSourceObject = SelectionManager.Instance.SelectedObject;
                        var selectedSourcePolygon = (selectedSourceObject is LevelEntity_Polygon) ? selectedSourceObject as LevelEntity_Polygon : null;

                        if (selectedSourcePolygon && selectedSourcePolygon != ParentPolygon)
                        {
                            ParentPolygon.SetOffset(DataSource,
                                                      DataSource == LevelEntity_Polygon.DataSources.Floor ? selectedSourcePolygon.NativeObject.floor_origin.x : selectedSourcePolygon.NativeObject.ceiling_origin.x,
                                                      DataSource == LevelEntity_Polygon.DataSources.Floor ? selectedSourcePolygon.NativeObject.floor_origin.y : selectedSourcePolygon.NativeObject.ceiling_origin.y,
                                                      rebatch: true);
                        }
                    }
                    else
                    {
                        SelectionManager.Instance.ToggleObjectSelection(ParentPolygon, multiSelect: false);
                        InputListener(ParentPolygon);

                        if (!surfaceShapeDescriptor.IsEmptyShapeDescriptor())
                        {
                            PaletteManager.Instance.SelectSwatchForTexture(surfaceShapeDescriptor);
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
                        PaletteManager.Instance.SelectSwatchForLight(RuntimeLight);
                    }

                    break;
                case ModeManager.PrimaryModes.Media:
                    if (Media != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(Media, multiSelect: false);
                        PaletteManager.Instance.SelectSwatchForMedia(Media);
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
                                  new Vector2(ParentPolygon.NativeObject.floor_origin.y, ParentPolygon.NativeObject.floor_origin.x) :
                                  new Vector2(ParentPolygon.NativeObject.ceiling_origin.y, ParentPolygon.NativeObject.ceiling_origin.x);

                var startingPosition = eventData.pointerPressRaycast.worldPosition;

                // Even for floor normals, use down here, because floors have U-flipped UVs 
                var surfaceWorldNormal = Vector3.down;

                var textureWorldUp = Vector3.left;

                uvDragPlane = new UVPlanarDrag(startingUVs,
                                               startingPosition,
                                               surfaceWorldNormal,
                                               textureWorldUp);

                if (ForgePlusInput.Editing.AlignContiguous.IsPressed())
                {
                    alignmentGroupedPolygons.Clear();

                    var commonElevation = DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                          ParentPolygon.NativeObject.floor_height :
                                          ParentPolygon.NativeObject.ceiling_height;

                    var commonShapeDescriptor = DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                                ParentPolygon.NativeObject.floor_texture :
                                                ParentPolygon.NativeObject.ceiling_texture;

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

                        var originalX = DataSource == LevelEntity_Polygon.DataSources.Floor ? ParentPolygon.NativeObject.floor_origin.x : ParentPolygon.NativeObject.ceiling_origin.x;
                        var originalY = DataSource == LevelEntity_Polygon.DataSources.Floor ? ParentPolygon.NativeObject.floor_origin.y : ParentPolygon.NativeObject.ceiling_origin.y;

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

        private void CollectSimilarAdjacentPolygons(LevelEntity_Polygon centralPolygon, short commonElevation, ushort commonShapeDescriptor)
        {
            for (var i = 0; i < map.MAXIMUM_VERTICES_PER_POLYGON; i++)
            {
                var adjacentPolygonIndex = ParentPolygon.NativeObject.adjacent_polygon_indexes[i];

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
                                        adjacentPolygon.NativeObject.floor_height :
                                        adjacentPolygon.NativeObject.ceiling_height;

                if (adjacentElevation != commonElevation)
                {
                    continue;
                }

                var adjacentShapeDescriptor = DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                              adjacentPolygon.NativeObject.floor_texture :
                                              adjacentPolygon.NativeObject.ceiling_texture;

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
