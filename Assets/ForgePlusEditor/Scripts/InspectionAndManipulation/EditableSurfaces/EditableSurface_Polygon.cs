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

namespace ForgePlus.LevelManipulation
{
    public class EditableSurface_Polygon : EditableSurface_Base
    {
        public LevelEntity_Polygon ParentPolygon = null;
        public LevelEntity_Polygon.DataSources DataSource;

        public LevelEntity_Platform Platform = null;

        // Read from the polygon each time, so they follow what's painted onto it
        public ushort SurfaceShapeDescriptor
        {
            get
            {
                return DataSource == LevelEntity_Polygon.DataSources.Floor ?
                       ParentPolygon.NativeObject.floor_texture :
                       ParentPolygon.NativeObject.ceiling_texture;
            }
        }

        public LevelEntity_Light RuntimeLight
        {
            get
            {
                return ParentPolygon.ParentLevel.Lights[DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                                        ParentPolygon.NativeObject.floor_lightsource_index :
                                                        ParentPolygon.NativeObject.ceiling_lightsource_index];
            }
        }

        private UVPlanarDrag uvDragPlane;

        private readonly List<LevelEntity_Polygon> alignmentGroupedPolygons = new List<LevelEntity_Polygon>();

        public override void OnValidatedPointerClick(WorldPointerEventData eventData)
        {
            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    SelectionManager.Instance.ToggleObjectSelection(ParentPolygon, multiSelect: false);

                    break;
                case ModeManager.PrimaryModes.Textures:
                    if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
                    {
                        if (PaletteManager.Instance.TryGetSelectedTexture(out var selectedTexture))
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

                        // An unassigned surface selects "None"
                        PaletteManager.Instance.SelectSwatchForTexture(SurfaceShapeDescriptor);
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
                    ClickPolygonInMediaMode(ParentPolygon);

                    break;
                case ModeManager.PrimaryModes.Sounds:
                    ClickPolygonInSoundsMode(ParentPolygon);

                    break;
                case ModeManager.PrimaryModes.Platforms:
                    // Any face of a platform (either surface of one that goes both ways, or the sides that move with it)
                    // selects it, or deselects it if it's selected
                    if (Platform != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(Platform.SelectablePlatform, multiSelect: false);
                    }

                    break;
                case ModeManager.PrimaryModes.Annotations:
                    LevelEntity_Annotation.ClickPolygon(ParentPolygon);

                    break;
                default:
                    Debug.LogError($"Selection in mode \"{ModeManager.Instance.PrimaryMode}\" is not supported.");
                    return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public override void OnValidatedBeginDrag(WorldPointerEventData eventData)
        {
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                // Note: Polygon surfaces have swapped UVs, so swap them here
                var startingUVs = DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                  new Vector2(ParentPolygon.NativeObject.floor_origin.y, ParentPolygon.NativeObject.floor_origin.x) :
                                  new Vector2(ParentPolygon.NativeObject.ceiling_origin.y, ParentPolygon.NativeObject.ceiling_origin.x);

                var startingPosition = eventData.PressWorldPosition;

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

                    CollectSimilarAdjacentPolygons(GetElevation(ParentPolygon), GetShapeDescriptor(ParentPolygon));
                }
            }
            else
            {
                return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public override void OnValidatedDrag(WorldPointerEventData eventData)
        {
            if (uvDragPlane != null &&
                ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Textures &&
                ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Editing)
            {
                var screenPosition = new Vector3(eventData.Position.x,
                                                 eventData.Position.y,
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

        public override void OnValidatedEndDrag(WorldPointerEventData eventData)
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

        // Spreads out from this polygon through shared edges, collecting every polygon its surface continues into (at
        // the same height, with the same texture), until no more neighbors continue it
        private void CollectSimilarAdjacentPolygons(short commonElevation, ushort commonShapeDescriptor)
        {
            var checkedPolygons = new HashSet<LevelEntity_Polygon> { ParentPolygon };
            var polygonsToSpreadFrom = new Stack<LevelEntity_Polygon>();
            polygonsToSpreadFrom.Push(ParentPolygon);

            while (polygonsToSpreadFrom.Count > 0)
            {
                var polygonData = polygonsToSpreadFrom.Pop().NativeObject;

                for (var i = 0; i < polygonData.vertex_count; i++)
                {
                    // Edges with no polygon on the other side have no adjacent polygon (NONE)
                    if (!LevelEntity_Level.Instance.Polygons.TryGetValue(polygonData.adjacent_polygon_indexes[i], out var adjacentPolygon) ||
                        !checkedPolygons.Add(adjacentPolygon))
                    {
                        continue;
                    }

                    if (GetElevation(adjacentPolygon) != commonElevation ||
                        !GetShapeDescriptor(adjacentPolygon).Equals(commonShapeDescriptor))
                    {
                        continue;
                    }

                    alignmentGroupedPolygons.Add(adjacentPolygon);
                    polygonsToSpreadFrom.Push(adjacentPolygon);
                }
            }
        }

        private short GetElevation(LevelEntity_Polygon polygon)
        {
            return DataSource == LevelEntity_Polygon.DataSources.Floor ?
                   polygon.NativeObject.floor_height :
                   polygon.NativeObject.ceiling_height;
        }

        private ushort GetShapeDescriptor(LevelEntity_Polygon polygon)
        {
            return DataSource == LevelEntity_Polygon.DataSources.Floor ?
                   polygon.NativeObject.floor_texture :
                   polygon.NativeObject.ceiling_texture;
        }
    }
}
