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

        // The faces a height drag moves (this one first, then the rest of the selection's polygons' same faces), and their
        // heights when it began
        private readonly List<(LevelEntity_Polygon Polygon, short StartingHeight)> heightDragged = new List<(LevelEntity_Polygon, short)>();
        private PointsMove polygonMove;

        // The polygons whose same faces continue this one's texture (found as a texture drag begins), and their offsets
        // before it: while Align Contiguous (ctrl) is held during the drag, they're aligned to this one; otherwise, they're
        // as they were
        private readonly List<LevelEntity_Polygon> alignmentGroupedPolygons = new List<LevelEntity_Polygon>();
        private readonly Dictionary<LevelEntity_Polygon, (short X, short Y)> alignmentStartingOffsets = new Dictionary<LevelEntity_Polygon, (short, short)>();
        private Vector2 draggedUVOffset;
        private bool isAligningGroup;

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
                                                      DataSource == LevelEntity_Polygon.DataSources.Floor ? selectedSourcePolygon.NativeObject.floor_origin.y : selectedSourcePolygon.NativeObject.ceiling_origin.y);
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

                // Its axis locks swapped, to lock as they do when moving geometry across the level
                uvDragPlane = new UVPlanarDrag(startingUVs,
                                               startingPosition,
                                               surfaceWorldNormal,
                                               textureWorldUp,
                                               swapsAxisLocks: true);

                // Recorded as one action when it ends
                LevelHistory.BeginGesture();

                alignmentGroupedPolygons.Clear();
                alignmentStartingOffsets.Clear();

                CollectSimilarAdjacentPolygons(HeightsEditing.GetHeight(ParentPolygon, DataSource), GetShapeDescriptor(ParentPolygon));

                foreach (var polygon in alignmentGroupedPolygons)
                {
                    var origin = DataSource == LevelEntity_Polygon.DataSources.Floor ? polygon.NativeObject.floor_origin : polygon.NativeObject.ceiling_origin;
                    alignmentStartingOffsets[polygon] = (origin.x, origin.y);
                }

                draggedUVOffset = startingUVs;
                isAligningGroup = ForgePlusInput.Editing.AlignContiguous.IsPressed();
                WatchAlignContiguous(uvDragPlane);
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
                draggedUVOffset = newUVOffset;

                // Note: Polygon surfaces have swapped UVs, so swap them here
                ParentPolygon.SetOffset(DataSource,
                                          (short)newUVOffset.y,
                                          (short)newUVOffset.x);

                ApplyAlignmentGroup();
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

                alignmentGroupedPolygons.Clear();
                alignmentStartingOffsets.Clear();
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
                                                  newY);
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

            heightDragged.Clear();
            heightDragged.Add((ParentPolygon, HeightsEditing.GetHeight(ParentPolygon, DataSource)));
            foreach (var selected in SelectionManager.Instance.Selection)
            {
                if (selected is LevelEntity_Polygon polygon && polygon != ParentPolygon && HeightsEditing.CanEdit(polygon))
                {
                    heightDragged.Add((polygon, HeightsEditing.GetHeight(polygon, DataSource)));
                }
            }

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
            polygonMove = PointsMove.ForSelection(ParentPolygon.ParentLevel, ParentPolygon, eventData.PressWorldPosition);
        }

        private void DragHeight(WorldPointerEventData eventData)
        {
            // Each selected face moves as far as this one is dragged, kept to its own polygon (a floor no higher than its
            // ceiling, and a ceiling no lower than its floor)
            var pointerRay = Camera.main.ScreenPointToRay(new Vector3(eventData.Position.x, eventData.Position.y, 0f));
            var offset = heightDrag.DraggedHeight(pointerRay) - heightDragged[0].StartingHeight;

            foreach (var (polygon, startingHeight) in heightDragged)
            {
                HeightsEditing.DragHeight(polygon, DataSource, HeightsEditing.ClampHeight(polygon, DataSource, startingHeight + offset));
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        private void EndHeightDrag()
        {
            heightDrag = null;
            heightDragged.Clear();

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

        // Aligned to this face's texture while Align Contiguous (ctrl) is held, and otherwise as they were before the drag
        private void ApplyAlignmentGroup()
        {
            foreach (var polygon in alignmentGroupedPolygons)
            {
                var (x, y) = isAligningGroup ? ((short) draggedUVOffset.y, (short) draggedUVOffset.x) : alignmentStartingOffsets[polygon];
                polygon.SetOffset(DataSource, x, y);
            }
        }

        // Pressing or letting go of Align Contiguous during the drag changes the group, even while the pointer stays put
        private async void WatchAlignContiguous(UVPlanarDrag drag)
        {
            while (Application.isPlaying && this && uvDragPlane == drag)
            {
                await Awaitable.NextFrameAsync();

                var isPressed = ForgePlusInput.Editing.AlignContiguous.IsPressed();
                if (!this || uvDragPlane != drag || isPressed == isAligningGroup)
                {
                    continue;
                }

                isAligningGroup = isPressed;
                ApplyAlignmentGroup();
                InspectorPanel.Instance.RefreshAllInspectors();
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
