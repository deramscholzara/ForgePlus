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
    // Moving points (the level's endpoints), one at a time or several together (such as a polygon's corners, by the same
    // amount). What's worked out from where they are is kept up to date as they move (lines'
    // lengths, sides' exclusion zones, polygons' areas and centers, and the faces that meet them), and the rest once a
    // move is put down: lines left with no length collapse into single points (GeometryRepair), objects and annotations
    // the move left outside their polygons (or heights) are put back in (ObjectPlacement), and the map indexes (what's
    // near each polygon, and which sound sources it hears) are worked out again.
    //
    // While Prevent Invalid Moves is on (the setting), a point (or points moved together) stops short of where it would
    // make a polygon around it invalid (GeometryValidation's errors that weren't there before the move); holding Allow
    // Invalid Move (shift) while dragging lets it go there anyway. What a move would break, or breaks, is shown as it goes
    // (Feedback).
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
            DragPoints(point.ParentLevel, new[] { point.NativeIndex }, point.NativeIndex, location, snapsMovement: false, canBeAllowedInvalid);
        }

        // Moves the points together, each as far as the reference point moves to the location (from where they all were
        // when the move began). The movement itself is what snaps to the grid, if snapsMovement, so points off the grid
        // stay as far off it; otherwise the reference point's location is what snaps. Returns how far they moved.
        public static Vector2Int DragPoints(LevelEntity_Level level, IReadOnlyList<short> pointIndexes, short referenceIndex, world_point2d location, bool snapsMovement, bool canBeAllowedInvalid = false)
        {
            var data = level.Level;
            var reference = data.EndpointList[referenceIndex].vertex;

            if (!movedPoints.ContainsKey(referenceIndex) && reference.x == location.x && reference.y == location.y)
            {
                return Vector2Int.zero;
            }

            var group = new List<(short Index, MovedPoint Moved)>();
            foreach (var pointIndex in pointIndexes)
            {
                if (!movedPoints.TryGetValue(pointIndex, out var movedPoint))
                {
                    movedPoint = Capture(level, pointIndex);
                    movedPoints[pointIndex] = movedPoint;
                }

                group.Add((pointIndex, movedPoint));
            }

            var referenceStart = movedPoints[referenceIndex].StartingLocation;
            var target = new Vector2Int(location.x - referenceStart.x, location.y - referenceStart.y);
            var current = new Vector2Int(reference.x - referenceStart.x, reference.y - referenceStart.y);

            var movement = target;
            if (PreventsInvalidMoves(canBeAllowedInvalid))
            {
                movement = LimitToValid(level, group, referenceIndex, current, target, snapsMovement);
            }

            var isBlocked = movement != target;

            if (movement != current)
            {
                foreach (var (pointIndex, movedPoint) in group)
                {
                    data.EndpointList[pointIndex].vertex = Moved(movedPoint.StartingLocation, movement);
                }

                RecalculateAround(level, pointIndexes);

                foreach (var (pointIndex, movedPoint) in group)
                {
                    ApplyTextures(level, pointIndex, movedPoint, TexturesFollowPoints);
                }

                ApplyGeometry(level, pointIndexes);
            }

            UpdateFeedback(level, group, referenceIndex, isBlocked, target);

            return movement;
        }

        private static world_point2d Moved(world_point2d start, Vector2Int movement)
        {
            return new world_point2d((short) Mathf.Clamp(start.x + movement.x, short.MinValue, short.MaxValue),
                                     (short) Mathf.Clamp(start.y + movement.y, short.MinValue, short.MaxValue));
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

        // The points, each moved this far from where it began, while the move is checked (and then put back)
        private static T WithMovement<T>(LevelEntity_Level level, List<(short Index, MovedPoint Moved)> group, Vector2Int movement, System.Func<T> check)
        {
            var endpoints = level.Level.EndpointList;
            var currents = new world_point2d[group.Count];

            for (var i = 0; i < group.Count; i++)
            {
                currents[i] = endpoints[group[i].Index].vertex;
                endpoints[group[i].Index].vertex = Moved(group[i].Moved.StartingLocation, movement);
            }

            try
            {
                return check();
            }
            finally
            {
                for (var i = 0; i < group.Count; i++)
                {
                    endpoints[group[i].Index].vertex = currents[i];
                }
            }
        }

        // The errors (and warnings, if included) around the points that weren't there before the move, each once
        private static List<GeometryIssue> NewProblems(LevelEntity_Level level, List<(short Index, MovedPoint Moved)> group, bool includeWarnings)
        {
            var keys = new HashSet<(GeometryIssueKind, short, short, short, short)>();
            var problems = new List<GeometryIssue>();

            foreach (var (pointIndex, movedPoint) in group)
            {
                foreach (var issue in ProblemsAround(level, pointIndex, includeWarnings))
                {
                    if (!group.Any(member => member.Moved.StartingProblems.Contains(issue.Key)) && keys.Add(issue.Key))
                    {
                        problems.Add(issue);
                    }
                }
            }

            return problems;
        }

        // Whether the points, moved this far, would leave no errors around them that weren't there before the move
        private static bool IsValidAt(LevelEntity_Level level, List<(short Index, MovedPoint Moved)> group, Vector2Int movement)
        {
            return WithMovement(level, group, movement, () => NewProblems(level, group, includeWarnings: false).Count == 0);
        }

        // As far toward the target as the points can go without new errors (on the grid, while snapping), from where they
        // are (or, if they're somewhere invalid, as allowed before, from where they last weren't)
        private static Vector2Int LimitToValid(LevelEntity_Level level, List<(short Index, MovedPoint Moved)> group, short referenceIndex, Vector2Int from, Vector2Int target, bool snapsMovement)
        {
            if (IsValidAt(level, group, target))
            {
                return target;
            }

            var referenceMoved = movedPoints[referenceIndex];
            if (!IsValidAt(level, group, from))
            {
                var lastValid = referenceMoved.LastValidLocation;
                from = new Vector2Int(lastValid.x - referenceMoved.StartingLocation.x, lastValid.y - referenceMoved.StartingLocation.y);
            }

            float valid = 0f, invalid = 1f;
            for (var step = 0; step < 16; step++)
            {
                var middle = (valid + invalid) * 0.5f;
                if (IsValidAt(level, group, Between(from, target, middle)))
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

            // Back along the way, the first grid mark that's valid (for the movement, or the reference point)
            var distance = Vector2.Distance(from, target);
            var stepFraction = distance > 0f ? HeightsEditing.SnapIncrement / distance : 1f;
            var referenceStart = new Vector2Int(referenceMoved.StartingLocation.x, referenceMoved.StartingLocation.y);

            for (var fraction = valid; fraction >= 0f; fraction -= stepFraction * 0.5f)
            {
                var between = Between(from, target, fraction);
                var snapped = snapsMovement ? Snap(between) : Snap(referenceStart + between) - referenceStart;
                if (IsValidAt(level, group, snapped))
                {
                    return snapped;
                }
            }

            return from;
        }

        private static Vector2Int Between(Vector2Int from, Vector2Int to, float fraction)
        {
            return new Vector2Int(Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, fraction)), Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, fraction)));
        }

        private static Vector2Int Snap(Vector2Int value)
        {
            int SnapCoordinate(int coordinate)
            {
                return Mathf.RoundToInt(Mathf.Round((float) coordinate / HeightsEditing.SnapIncrement) * HeightsEditing.SnapIncrement);
            }

            return new Vector2Int(SnapCoordinate(value.x), SnapCoordinate(value.y));
        }

        // New errors and warnings where the points are (or where they're held back from), for showing as they go, by the
        // reference point
        private static void UpdateFeedback(LevelEntity_Level level, List<(short Index, MovedPoint Moved)> group, short referenceIndex, bool isBlocked, Vector2Int target)
        {
            var referenceMoved = movedPoints[referenceIndex];
            var current = level.Level.EndpointList[referenceIndex].vertex;
            var feedback = new MoveFeedback
            {
                PointIndex = referenceIndex,
                IsBlocked = isBlocked,
                BlockedLocation = Moved(referenceMoved.StartingLocation, target),
                Height = DragHeight ?? group.Max(member => member.Moved.TopHeight),
            };

            feedback.Issues.AddRange(isBlocked ?
                                     WithMovement(level, group, target, () => NewProblems(level, group, includeWarnings: true)) :
                                     NewProblems(level, group, includeWarnings: true));

            if (!isBlocked)
            {
                if (feedback.HasErrors)
                {
                    feedback.IsInvalid = true;
                    feedback.LastValidLocation = referenceMoved.LastValidLocation;
                }
                else
                {
                    referenceMoved.LastValidLocation = current;
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

        // The data worked out from the points' positions, in the lines and polygons that meet them (map_constructors.cpp:
        // recalculate_redundant_line_data and recalculate_redundant_polygon_data, without what doesn't depend on where they
        // are)
        private static void RecalculateAround(LevelEntity_Level level, IEnumerable<short> pointIndexes)
        {
            var data = level.Level;
            var lineIndexes = new HashSet<short>();
            var polygonIndexes = new HashSet<short>();
            CollectAround(level, pointIndexes, lineIndexes, polygonIndexes);

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
