using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Palette;
using ForgePlus.UI;
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

        private UVPlanarDrag uvDragPlane;
        private HeightDrag heightDrag;
        private PolygonMove polygonMove;

        private readonly List<LevelEntity_Polygon> alignmentGroupedPolygons = new List<LevelEntity_Polygon>();

        public override ushort SurfaceShapeDescriptor
        {
            get
            {
                return GetShapeDescriptor(ParentPolygon);
            }
        }

        public override LevelEntity_Light RuntimeLight
        {
            get
            {
                return ParentPolygon.ParentLevel.Lights[DataSource == LevelEntity_Polygon.DataSources.Floor ?
                                                        ParentPolygon.NativeObject.floor_lightsource_index :
                                                        ParentPolygon.NativeObject.ceiling_lightsource_index];
            }
        }

        protected override bool IsCeilingFace
        {
            get
            {
                return DataSource == LevelEntity_Polygon.DataSources.Ceiling;
            }
        }

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
                        ToggleLightSelection();
                    }

                    break;
                case ModeManager.PrimaryModes.Media:
                case ModeManager.PrimaryModes.Sounds:
                case ModeManager.PrimaryModes.Annotations:
                    ClickPolygonInMode(ParentPolygon);

                    break;
                case ModeManager.PrimaryModes.Heights:
                    ClickInHeightsMode();

                    break;
                case ModeManager.PrimaryModes.Platforms:
                    // Any of a platform's faces (including the sides that move with it) selects it
                    if (Platform != null)
                    {
                        SelectionManager.Instance.ToggleObjectSelection(Platform.SelectablePlatform, multiSelect: false);
                    }

                    break;
                default:
                    Debug.LogError($"Selection in mode \"{ModeManager.Instance.PrimaryMode}\" is not supported.");
                    return;
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public override void OnValidatedBeginDrag(WorldPointerEventData eventData)
        {
            // Looking straight down, there's no raising or lowering it, so in Geometry mode it moves across the level instead
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry && ForgePlusUI.Instance.EditorCamera.IsOrthographic)
            {
                BeginPolygonMove(eventData);
                return;
            }

            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Heights ||
                ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry)
            {
                BeginHeightDrag(eventData);
                return;
            }

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

                // Recorded as one action when it ends
                LevelHistory.BeginGesture();

                if (ForgePlusInput.Editing.AlignContiguous.IsPressed())
                {
                    alignmentGroupedPolygons.Clear();

                    CollectSimilarAdjacentPolygons(HeightsEditing.GetHeight(ParentPolygon, DataSource), GetShapeDescriptor(ParentPolygon));
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
            if (heightDrag != null)
            {
                DragHeight(eventData);
                return;
            }

            if (polygonMove != null)
            {
                polygonMove.Drag(Camera.main.ScreenPointToRay(new Vector3(eventData.Position.x, eventData.Position.y, 0f)));
                InspectorPanel.Instance.RefreshAllInspectors();
                return;
            }

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
            if (uvDragPlane != null)
            {
                LevelHistory.EndGesture();
            }

            if (heightDrag != null)
            {
                EndHeightDrag();
                return;
            }

            if (polygonMove != null)
            {
                var move = polygonMove;
                polygonMove = null;
                move.End();

                InspectorPanel.Instance.RefreshAllInspectors();
                return;
            }

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

        // Paints the palette's height onto the floor or ceiling its swatch is for, or picks the clicked face's height (the
        // selected polygon's other face picks that one's, rather than deselecting it)
        private void ClickInHeightsMode()
        {
            if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting)
            {
                if (PaletteManager.Instance.TryGetSelectedHeight(out var dataSource, out var height))
                {
                    HeightsEditing.SetHeights(new[] { ParentPolygon }, dataSource, height);
                }

                return;
            }

            if (SelectionManager.Instance.GetIsSelected(ParentPolygon) && !PaletteManager.Instance.ShowsHeightOf(ParentPolygon, DataSource))
            {
                PaletteManager.Instance.SelectSwatchForHeight(ParentPolygon, DataSource);
                return;
            }

            SelectionManager.Instance.ToggleObjectSelection(ParentPolygon, multiSelect: false);

            if (SelectionManager.Instance.GetIsSelected(ParentPolygon))
            {
                PaletteManager.Instance.SelectSwatchForHeight(ParentPolygon, DataSource);
            }
        }

        // In Select mode, dragging the face raises or lowers it (in Geometry mode, only once its polygon is selected, so
        // dragging across unselected geometry can't change it by accident)
        private void BeginHeightDrag(WorldPointerEventData eventData)
        {
            if (ModeManager.Instance.SecondaryMode != ModeManager.SecondaryModes.Selection || !HeightsEditing.CanEdit(ParentPolygon))
            {
                return;
            }

            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry)
            {
                if (!SelectionManager.Instance.GetIsSelected(ParentPolygon))
                {
                    return;
                }
            }
            else
            {
                SelectionManager.Instance.SelectObject(ParentPolygon, multiSelect: false);
            }

            SelectionManager.Instance.ClickedSurface = this;
            SelectHeightSwatch();

            heightDrag = new HeightDrag(eventData.PressWorldPosition, HeightsEditing.GetHeight(ParentPolygon, DataSource), Camera.main);
            HeightsEditing.BeginDrag();

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        // In Select mode, once its polygon is selected (so dragging across unselected geometry can't move it by accident)
        private void BeginPolygonMove(WorldPointerEventData eventData)
        {
            if (ModeManager.Instance.SecondaryMode != ModeManager.SecondaryModes.Selection || !SelectionManager.Instance.GetIsSelected(ParentPolygon))
            {
                return;
            }

            SelectionManager.Instance.ClickedSurface = this;
            polygonMove = new PolygonMove(ParentPolygon, eventData.PressWorldPosition);
        }

        private void DragHeight(WorldPointerEventData eventData)
        {
            var pointerRay = Camera.main.ScreenPointToRay(new Vector3(eventData.Position.x, eventData.Position.y, 0f));
            var height = HeightsEditing.ClampHeight(ParentPolygon, DataSource, heightDrag.DraggedHeight(pointerRay));

            HeightsEditing.DragHeight(ParentPolygon, DataSource, height);

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        private void EndHeightDrag()
        {
            heightDrag = null;

            HeightsEditing.EndDrag();

            SelectHeightSwatch();
        }

        // Only the Heights palette lists heights
        private void SelectHeightSwatch()
        {
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Heights)
            {
                PaletteManager.Instance.SelectSwatchForHeight(ParentPolygon, DataSource);
            }
        }

        // Every polygon this surface continues into through shared edges (at the same height, with the same texture)
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
                    // An edge with no polygon on the other side has NONE
                    if (!LevelEntity_Level.Instance.Polygons.TryGetValue(polygonData.adjacent_polygon_indexes[i], out var adjacentPolygon) ||
                        !checkedPolygons.Add(adjacentPolygon))
                    {
                        continue;
                    }

                    if (HeightsEditing.GetHeight(adjacentPolygon, DataSource) != commonElevation ||
                        !GetShapeDescriptor(adjacentPolygon).Equals(commonShapeDescriptor))
                    {
                        continue;
                    }

                    alignmentGroupedPolygons.Add(adjacentPolygon);
                    polygonsToSpreadFrom.Push(adjacentPolygon);
                }
            }
        }

        private ushort GetShapeDescriptor(LevelEntity_Polygon polygon)
        {
            return DataSource == LevelEntity_Polygon.DataSources.Floor ?
                   polygon.NativeObject.floor_texture :
                   polygon.NativeObject.ceiling_texture;
        }
    }
}
