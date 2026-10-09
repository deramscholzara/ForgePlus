#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Localization;
using ForgePlus.PolygonContainment;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Moving points (the level's endpoints). What's worked out from where they are is kept up to date as they move (lines'
    // lengths, sides' exclusion zones, polygons' areas and centers, and the faces that meet them), and the rest once a
    // move is put down: lines left with no length collapse into single points (GeometryRepair), objects and annotations
    // the move left outside their polygons (or heights) are put back in (ObjectPlacement), and the map indexes (what's
    // near each polygon, and which sound sources it hears) are worked out again.
    //
    // While Prevent Invalid Moves is on (the setting), a point stops short of where it would make a polygon around it
    // invalid (GeometryValidation's errors that weren't there before the move); holding Allow Invalid Move (shift) while
    // dragging its handle lets it go there anyway. What a move would break, or breaks, is shown as it goes (Feedback).
    //
    // Textures stay where they were at the lines' other ends (for sides) and in the world (for floors and ceilings), or
    // while Keep Textures With Point (ctrl) is held, move with the point instead. Each step of a move works them out from
    // how they were before it, as though the modifier had been as it is now for the whole move.
    [AutoStaticsCleanup]
    public static partial class PointEditing
    {
        // A point being moved, and the textures around it, as they were before it moved
        private sealed class MovedPoint
        {
            public world_point2d StartingLocation;
            public readonly Dictionary<short, short> LineLengths = new Dictionary<short, short>();

            // Each side's primary, secondary and transparent texture offsets
            public readonly Dictionary<short, short[]> SideOffsets = new Dictionary<short, short[]>();

            public readonly Dictionary<short, (world_point2d Floor, world_point2d Ceiling)> PolygonOrigins = new Dictionary<short, (world_point2d, world_point2d)>();

            // The errors around it before it moved (which it can keep), and what was in the polygons around it
            public HashSet<(GeometryIssueKind, short, short, short, short)> StartingProblems;
            public ObjectPlacement.Snapshot Placements;

            // Where it last was without new errors
            public world_point2d LastValidLocation;

            // The highest of its polygons' ceilings (in meters), where its feedback is shown unless a handle was grabbed
            public float TopHeight;
        }

        // What a move would break (where the pointer is, if it's held back), or breaks, for showing as it goes
        public sealed class MoveFeedback
        {
            public short PointIndex;
            public bool IsBlocked;
            public world_point2d BlockedLocation;

            // Where it last was without new errors, while it's somewhere with them
            public bool IsInvalid;
            public world_point2d LastValidLocation;

            // New since the move began
            public readonly List<GeometryIssue> Issues = new List<GeometryIssue>();
            public string Message;

            // In meters
            public float Height;

            public bool HasErrors
            {
                get
                {
                    return Issues.Any(issue => !issue.IsWarning);
                }
            }
        }

        // Null while there's nothing to show
        public static MoveFeedback Feedback { get; private set; }

        // The height of the handle that's being dragged (in meters), or null for a move that isn't by a handle
        public static float? DragHeight { get; set; }

        // Moved since the move (or drag) began
        private static readonly Dictionary<short, MovedPoint> movedPoints = new Dictionary<short, MovedPoint>();

        private static bool isMoving;

        // Each move's, so a watch left from an earlier move stops
        private static int moveNumber;

        private static bool TexturesFollowPoints
        {
            get
            {
                return ForgePlusInput.Editing.KeepTexturesWithPoint.IsPressed();
            }
        }

        private static bool PreventsInvalidMoves(bool canBeAllowed)
        {
            return SettingsManager.Instance.PreventInvalidGeometryEnabled && !(canBeAllowed && ForgePlusInput.Editing.AllowInvalidGeometry.IsPressed());
        }

        // Typed in, as one action
        public static void MovePoint(LevelEntity_Point point, world_point2d location)
        {
            BeginDrag();

            try
            {
                DragPoint(point, location);
            }
            finally
            {
                EndDrag();
            }
        }

        public static void BeginDrag()
        {
            // Recorded as one action when it ends
            LevelHistory.BeginGesture();
            movedPoints.Clear();
            SurfaceBatchingManager.Instance.DeferMerging(true);

            isMoving = true;
            WatchTextureModifier(++moveNumber);
        }

        // Dragging a handle can let a point go where it'd make a polygon invalid (while Allow Invalid Move is held)
        public static void DragPoint(LevelEntity_Point point, world_point2d location, bool canBeAllowedInvalid = false)
        {
            var level = point.ParentLevel;
            var endpoint = point.NativeObject;

            if (!movedPoints.TryGetValue(point.NativeIndex, out var movedPoint))
            {
                if (endpoint.vertex.x == location.x && endpoint.vertex.y == location.y)
                {
                    return;
                }

                movedPoint = Capture(level, point.NativeIndex);
                movedPoints[point.NativeIndex] = movedPoint;
            }

            var target = location;
            if (PreventsInvalidMoves(canBeAllowedInvalid))
            {
                location = LimitToValid(level, point.NativeIndex, movedPoint, endpoint.vertex, target);
            }

            var isBlocked = location.x != target.x || location.y != target.y;

            if (endpoint.vertex.x != location.x || endpoint.vertex.y != location.y)
            {
                endpoint.vertex = location;

                RecalculateAround(level, point.NativeIndex);
                ApplyTextures(level, point.NativeIndex, movedPoint, TexturesFollowPoints);
                ApplyGeometry(level, new[] { point.NativeIndex });
            }

            UpdateFeedback(level, point.NativeIndex, movedPoint, isBlocked, target);
        }

        public static void EndDrag()
        {
            isMoving = false;
            Feedback = null;
            DragHeight = null;

            try
            {
                if (movedPoints.Count > 0)
                {
                    Commit(LevelEntity_Level.Instance, movedPoints);
                }
            }
            finally
            {
                movedPoints.Clear();
                SurfaceBatchingManager.Instance.DeferMerging(false);
                LevelHistory.EndGesture();
            }
        }

        // The errors around the point (other than lines of no length, which collapse once it's put down)
        private static IEnumerable<GeometryIssue> ProblemsAround(LevelEntity_Level level, short pointIndex, bool includeWarnings)
        {
            var data = level.Level;
            var lineIndexes = level.EndpointLines[pointIndex];
            var issues = GeometryValidation.CheckAroundPoint(data, pointIndex, lineIndexes);

            return issues.Where(issue => issue.Kind != GeometryIssueKind.ZeroLengthLine && (includeWarnings || !issue.IsWarning));
        }

        // Whether the point, put there, would leave no errors around it that weren't there before the move
        private static bool IsValidAt(LevelEntity_Level level, short pointIndex, MovedPoint movedPoint, world_point2d location)
        {
            var endpoint = level.Level.EndpointList[pointIndex];
            var current = endpoint.vertex;
            endpoint.vertex = location;

            try
            {
                return ProblemsAround(level, pointIndex, includeWarnings: false).All(issue => movedPoint.StartingProblems.Contains(issue.Key));
            }
            finally
            {
                endpoint.vertex = current;
            }
        }

        // As far toward the target as the point can go without new errors (on the grid, while snapping), from where it
        // is (or, if it's somewhere invalid, as allowed before, from where it last wasn't)
        private static world_point2d LimitToValid(LevelEntity_Level level, short pointIndex, MovedPoint movedPoint, world_point2d from, world_point2d target)
        {
            if (IsValidAt(level, pointIndex, movedPoint, target))
            {
                return target;
            }

            if (!IsValidAt(level, pointIndex, movedPoint, from))
            {
                from = movedPoint.LastValidLocation;
            }

            float valid = 0f, invalid = 1f;
            for (var step = 0; step < 16; step++)
            {
                var middle = (valid + invalid) * 0.5f;
                if (IsValidAt(level, pointIndex, movedPoint, Between(from, target, middle)))
                {
                    valid = middle;
                }
                else
                {
                    invalid = middle;
                }
            }

            if (!AxisLocks.Instance.SnapToGrid)
            {
                return Between(from, target, valid);
            }

            // Back along the way, the first grid mark that's valid
            var distance = Mathf.Sqrt(Mathf.Pow(target.x - from.x, 2f) + Mathf.Pow(target.y - from.y, 2f));
            var stepFraction = distance > 0f ? HeightsEditing.SnapIncrement / distance : 1f;

            for (var fraction = valid; fraction >= 0f; fraction -= stepFraction * 0.5f)
            {
                var snapped = Snap(Between(from, target, fraction));
                if (IsValidAt(level, pointIndex, movedPoint, snapped))
                {
                    return snapped;
                }
            }

            return from;
        }

        private static world_point2d Between(world_point2d from, world_point2d to, float fraction)
        {
            return new world_point2d((short) Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, fraction)), (short) Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, fraction)));
        }

        private static world_point2d Snap(world_point2d location)
        {
            short SnapCoordinate(short coordinate)
            {
                return (short) Mathf.Clamp(Mathf.Round((float) coordinate / HeightsEditing.SnapIncrement) * HeightsEditing.SnapIncrement, short.MinValue, short.MaxValue);
            }

            return new world_point2d(SnapCoordinate(location.x), SnapCoordinate(location.y));
        }

        // New errors and warnings where the point is (or where it's held back from), for showing as it goes
        private static void UpdateFeedback(LevelEntity_Level level, short pointIndex, MovedPoint movedPoint, bool isBlocked, world_point2d target)
        {
            var endpoint = level.Level.EndpointList[pointIndex];
            var feedback = new MoveFeedback
            {
                PointIndex = pointIndex,
                IsBlocked = isBlocked,
                BlockedLocation = target,
                Height = DragHeight ?? movedPoint.TopHeight,
            };

            var current = endpoint.vertex;
            if (isBlocked)
            {
                endpoint.vertex = target;
            }

            try
            {
                feedback.Issues.AddRange(ProblemsAround(level, pointIndex, includeWarnings: true).Where(issue => !movedPoint.StartingProblems.Contains(issue.Key)));
            }
            finally
            {
                endpoint.vertex = current;
            }

            if (!isBlocked)
            {
                if (feedback.HasErrors)
                {
                    feedback.IsInvalid = true;
                    feedback.LastValidLocation = movedPoint.LastValidLocation;
                }
                else
                {
                    movedPoint.LastValidLocation = current;
                }
            }

            if (feedback.Issues.Count == 0)
            {
                Feedback = null;
                return;
            }

            // Errors first
            feedback.Issues.Sort((a, b) => a.IsWarning.CompareTo(b.IsWarning));

            var message = GeometryErrors.ShortDescription(feedback.Issues[0]);
            if (feedback.Issues.Count > 1)
            {
                message += Strings.Get(Strings.Common, "PointMove.More", feedback.Issues.Count - 1);
            }

            feedback.Message = isBlocked ? Strings.Get(Strings.Common, "PointMove.Blocked", message) : message;
            Feedback = feedback;
        }

        // Pressing or letting go of the modifier during a move changes its textures, even while the point stays put
        private static async void WatchTextureModifier(int move)
        {
            var texturesFollowPoints = TexturesFollowPoints;

            while (isMoving && move == moveNumber)
            {
                await Awaitable.NextFrameAsync();

                if (!isMoving || move != moveNumber || TexturesFollowPoints == texturesFollowPoints)
                {
                    continue;
                }

                texturesFollowPoints = TexturesFollowPoints;

                var level = LevelEntity_Level.Instance;
                if (!level || movedPoints.Count == 0)
                {
                    continue;
                }

                foreach (var movedPoint in movedPoints)
                {
                    ApplyTextures(level, movedPoint.Key, movedPoint.Value, texturesFollowPoints);
                }

                ApplyGeometry(level, movedPoints.Keys.ToList());
            }
        }

        // Shows the points' positions in the faces that meet them, and the lines that meet them are built again
        private static void ApplyGeometry(LevelEntity_Level level, IEnumerable<short> pointIndexes)
        {
            var linesToRegenerate = new HashSet<short>();
            ApplyShapes(level, pointIndexes, linesToRegenerate);

            foreach (var lineIndex in linesToRegenerate)
            {
                if (level.Lines.TryGetValue(lineIndex, out var line))
                {
                    line.RegenerateSurfaces();
                }
            }
        }

        // Shows the points' positions in the floors, ceilings and media that meet them (also after undoing or redoing),
        // and adds the lines that meet them (whose sides are built again from them) to those to rebuild
        public static void ApplyShapes(LevelEntity_Level level, IEnumerable<short> pointIndexes, HashSet<short> linesToRegenerate)
        {
            var polygonIndexes = new HashSet<short>();
            CollectAround(level, pointIndexes, linesToRegenerate, polygonIndexes);

            foreach (var polygonIndex in polygonIndexes)
            {
                if (level.Polygons.TryGetValue(polygonIndex, out var polygon))
                {
                    polygon.ApplyShape();
                }
            }

            PolygonContainmentMap.MarkChanged();
        }

        // The data worked out from the point's position, in the lines and polygons that meet it (map_constructors.cpp:
        // recalculate_redundant_line_data and recalculate_redundant_polygon_data, without what doesn't depend on where it is)
        private static void RecalculateAround(LevelEntity_Level level, short pointIndex)
        {
            var data = level.Level;
            var lineIndexes = new HashSet<short>();
            var polygonIndexes = new HashSet<short>();
            CollectAround(level, new[] { pointIndex }, lineIndexes, polygonIndexes);

            foreach (var lineIndex in lineIndexes)
            {
                var line = data.LineList[lineIndex];
                line.length = world.distance2d(data.EndpointList[line.endpoint_indexes[0]].vertex, data.EndpointList[line.endpoint_indexes[1]].vertex);

                if (line.clockwise_polygon_side_index != cstypes.NONE)
                {
                    map_constructors.recalculate_redundant_side_data(data, line.clockwise_polygon_side_index, lineIndex);
                }

                if (line.counterclockwise_polygon_side_index != cstypes.NONE)
                {
                    map_constructors.recalculate_redundant_side_data(data, line.counterclockwise_polygon_side_index, lineIndex);
                }
            }

            foreach (var polygonIndex in polygonIndexes)
            {
                var polygon = data.PolygonList[polygonIndex];
                if (map.POLYGON_IS_DETACHED(polygon))
                {
                    continue;
                }

                polygon.area = map_constructors.calculate_polygon_area(data, polygonIndex);
                map.find_center_of_polygon(data, polygonIndex, out polygon.center);
            }
        }

        // The point, and the textures around it, before it moves
        private static MovedPoint Capture(LevelEntity_Level level, short pointIndex)
        {
            var data = level.Level;
            var movedPoint = new MovedPoint { StartingLocation = data.EndpointList[pointIndex].vertex };

            var lineIndexes = new HashSet<short>();
            var polygonIndexes = new HashSet<short>();
            CollectAround(level, new[] { pointIndex }, lineIndexes, polygonIndexes);

            foreach (var lineIndex in lineIndexes)
            {
                var line = data.LineList[lineIndex];
                movedPoint.LineLengths[lineIndex] = line.length;

                foreach (var sideIndex in new[] { line.clockwise_polygon_side_index, line.counterclockwise_polygon_side_index })
                {
                    if (sideIndex != cstypes.NONE)
                    {
                        var side = data.SideList[sideIndex];
                        movedPoint.SideOffsets[sideIndex] = new[] { side.primary_texture.x0, side.secondary_texture.x0, side.transparent_texture.x0 };
                    }
                }
            }

            var topHeight = short.MinValue;
            foreach (var polygonIndex in polygonIndexes)
            {
                var polygon = data.PolygonList[polygonIndex];
                movedPoint.PolygonOrigins[polygonIndex] = (polygon.floor_origin, polygon.ceiling_origin);
                topHeight = (short) Mathf.Max(topHeight, polygon.ceiling_height);
            }

            movedPoint.TopHeight = topHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
            movedPoint.LastValidLocation = movedPoint.StartingLocation;
            movedPoint.StartingProblems = new HashSet<(GeometryIssueKind, short, short, short, short)>(ProblemsAround(level, pointIndex, includeWarnings: true).Select(issue => issue.Key));
            movedPoint.Placements = ObjectPlacement.Capture(data, polygonIndexes);

            return movedPoint;
        }

        // From how they were before the point moved. A side's texture starts at its first point (the line's first, for its
        // clockwise side), and a floor's or ceiling's at its origin in the world.
        private static void ApplyTextures(LevelEntity_Level level, short pointIndex, MovedPoint movedPoint, bool texturesFollowPoint)
        {
            var data = level.Level;

            foreach (var lineLength in movedPoint.LineLengths)
            {
                var line = data.LineList[lineLength.Key];
                var lengthChange = lineLength.Value - line.length;

                foreach (var isClockwise in new[] { true, false })
                {
                    var sideIndex = isClockwise ? line.clockwise_polygon_side_index : line.counterclockwise_polygon_side_index;
                    if (sideIndex == cstypes.NONE || !movedPoint.SideOffsets.TryGetValue(sideIndex, out var offsets))
                    {
                        continue;
                    }

                    // The end that stays put is the other end, or with the modifier, the point's own
                    var startsAtPoint = (isClockwise ? line.endpoint_indexes[0] : line.endpoint_indexes[1]) == pointIndex;
                    var shift = startsAtPoint != texturesFollowPoint ? lengthChange : 0;

                    var side = data.SideList[sideIndex];
                    SetSideOffset(side.primary_texture, side.primary_transfer_mode, offsets[0], shift);
                    SetSideOffset(side.secondary_texture, side.secondary_transfer_mode, offsets[1], shift);
                    SetSideOffset(side.transparent_texture, side.transparent_transfer_mode, offsets[2], shift);
                }
            }

            // Moving with the point, a texture's origin moves the other way (its coordinates are a corner's plus its origin)
            var location = data.EndpointList[pointIndex].vertex;
            var movement = texturesFollowPoint ?
                           new world_point2d((short) (location.x - movedPoint.StartingLocation.x), (short) (location.y - movedPoint.StartingLocation.y)) :
                           new world_point2d(0, 0);

            foreach (var polygonOrigins in movedPoint.PolygonOrigins)
            {
                var polygon = data.PolygonList[polygonOrigins.Key];
                var (floorOrigin, ceilingOrigin) = polygonOrigins.Value;

                if (IsOffset(polygon.floor_texture, polygon.floor_transfer_mode))
                {
                    polygon.floor_origin = new world_point2d(WrapOffset(floorOrigin.x - movement.x), WrapOffset(floorOrigin.y - movement.y));
                }

                if (IsOffset(polygon.ceiling_texture, polygon.ceiling_transfer_mode))
                {
                    polygon.ceiling_origin = new world_point2d(WrapOffset(ceilingOrigin.x - movement.x), WrapOffset(ceilingOrigin.y - movement.y));
                }
            }
        }

        private static void SetSideOffset(side_texture_definition texture, short transferMode, short startingOffset, int shift)
        {
            if (IsOffset(texture.texture, transferMode))
            {
                texture.x0 = WrapOffset(startingOffset + shift);
            }
        }

        // A texture that's there, and isn't a landscape (which isn't offset)
        private static bool IsOffset(ushort shapeDescriptor, short transferMode)
        {
            return !shapeDescriptor.IsEmptyShapeDescriptor() &&
                   !shapeDescriptor.UsesLandscapeCollection() &&
                   !AlephOneExtensions.IsLandscapeTransferMode(transferMode);
        }

        // Textures repeat every world unit, so an offset that would overflow is wrapped by whole world units
        private static short WrapOffset(int offset)
        {
            return (short) (offset < short.MinValue || offset > short.MaxValue ? offset % world.WORLD_ONE : offset);
        }

        // Once a move is put down. Lines it left with no length collapse (which rebuilds the level); otherwise objects and
        // annotations it left outside their polygons (or heights) are put back in.
        private static void Commit(LevelEntity_Level level, Dictionary<short, MovedPoint> moved)
        {
            var data = level.Level;

            var placements = new ObjectPlacement.Snapshot();
            foreach (var movedPoint in moved.Values)
            {
                foreach (var entry in movedPoint.Placements.Objects.Where(entry => !placements.Objects.ContainsKey(entry.Key)))
                {
                    placements.Objects[entry.Key] = entry.Value;
                }

                foreach (var entry in movedPoint.Placements.Annotations.Where(entry => !placements.Annotations.ContainsKey(entry.Key)))
                {
                    placements.Annotations[entry.Key] = entry.Value;
                }
            }

            if (!GeometryRepair.CollapseAtPoints(level, moved.Keys.ToList(), placements, selectKeptPoint: true))
            {
                LevelEditing.RecalculateMapIndexes(data);

                var (objectIndexes, annotationIndexes) = ObjectPlacement.Apply(data, placements);

                foreach (var objectIndex in objectIndexes)
                {
                    if (level.MapObjects.TryGetValue(objectIndex, out var mapObject))
                    {
                        mapObject.ApplyPlacement();
                    }
                }

                foreach (var annotationIndex in annotationIndexes)
                {
                    if (level.Annotations.TryGetValue(annotationIndex, out var annotation))
                    {
                        annotation.RefreshPosition();
                    }
                }
            }

            var ui = ForgePlusUI.Instance;
            if (ui && ui.Errors != null)
            {
                ui.Errors.RequestRefresh();
            }

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        // The lines that meet the points, and the polygons on either side of them
        private static void CollectAround(LevelEntity_Level level, IEnumerable<short> pointIndexes, HashSet<short> lineIndexes, HashSet<short> polygonIndexes)
        {
            var data = level.Level;

            foreach (var pointIndex in pointIndexes)
            {
                if (pointIndex < 0 || pointIndex >= level.EndpointLines.Length)
                {
                    continue;
                }

                foreach (var lineIndex in level.EndpointLines[pointIndex])
                {
                    lineIndexes.Add(lineIndex);

                    var line = data.LineList[lineIndex];
                    if (line.clockwise_polygon_owner != cstypes.NONE)
                    {
                        polygonIndexes.Add(line.clockwise_polygon_owner);
                    }

                    if (line.counterclockwise_polygon_owner != cstypes.NONE)
                    {
                        polygonIndexes.Add(line.counterclockwise_polygon_owner);
                    }
                }
            }
        }
    }
}
#endif
