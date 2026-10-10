#if !NO_EDITING
using AlephOne;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgePlus.LevelManipulation
{
    public enum GeometryIssueKind
    {
        // Errors: the engines don't draw or play these as they look
        InsideOutPolygon,
        PolygonWithoutArea,
        ConcaveCorner,
        CrossingEdges,
        ZeroLengthLine,
        LineTooLong,
        TooManyMapIndexes,

        // Warnings: legal, but probably not meant
        RedundantStraightCorner,
        PointNearLine,
        MapIndexesAlephOneOnly,
    }

    // A problem with the level's geometry, and what it's in. Segments (pairs of point indexes) show where it is.
    public sealed class GeometryIssue
    {
        public GeometryIssueKind Kind;
        public short Polygon = cstypes.NONE;
        public short Line = cstypes.NONE;
        public short OtherLine = cstypes.NONE;
        public short Point = cstypes.NONE;
        public int Count;
        public readonly List<(short A, short B)> Segments = new List<(short, short)>();

        public bool IsWarning
        {
            get
            {
                return Kind >= GeometryIssueKind.RedundantStraightCorner;
            }
        }

        // Which issue this is, for telling whether it was there before an edit
        public (GeometryIssueKind, short, short, short, short) Key
        {
            get
            {
                return (Kind, Polygon, Line, OtherLine, Point);
            }
        }
    }

    // Finds what's wrong with the level's geometry: in Aleph One (and Marathon 2 and Infinity), polygons are convex, with
    // their corners clockwise (as the map shows them); lines have length, but can't be longer than a distance can be
    // (Marathon 2's distances wrap past 32767 world units, which Aleph One's long-distance physics clamp); and the map
    // indexes fit Marathon 2's and Infinity's signed counts (32767), or at most Aleph One's unsigned ones (65535).
    public static class GeometryValidation
    {
        public const int MaximumMarathonMapIndexes = short.MaxValue;
        public const int MaximumAlephOneMapIndexes = ushort.MaxValue - 1;

        // Points closer than this to a neighbor's line (without being on it) are almost certainly meant to be on it. Stock
        // Infinity levels have dozens of points within the engine's minimum distance from a wall (a quarter of a world unit)
        // by design, and a few within a sixteenth, but none within a sixty-fourth.
        public const int NearLineDistance = world.WORLD_ONE / 64;

        // Everything in the level
        public static List<GeometryIssue> CheckLevel(MapLevel level)
        {
            var issues = new List<GeometryIssue>();
            var lineCounts = LineCountsAtPoints(level);
            var straightCornerPoints = new HashSet<short>();

            for (short polygonIndex = 0; polygonIndex < level.PolygonList.Count; polygonIndex++)
            {
                CheckPolygon(level, polygonIndex, issues, lineCounts, straightCornerPoints);
            }

            for (short lineIndex = 0; lineIndex < level.LineList.Count; lineIndex++)
            {
                CheckLine(level, lineIndex, issues);
            }

            var pointPolygons = PolygonsAtPoints(level);
            for (short pointIndex = 0; pointIndex < level.EndpointList.Count; pointIndex++)
            {
                CheckPointNearLines(level, pointIndex, issues, pointPolygons[pointIndex]);
            }

            CheckMapIndexes(level, issues);

            return issues;
        }

        // The polygons and lines a point is in, and the point itself (as it's moved)
        public static List<GeometryIssue> CheckAroundPoint(MapLevel level, short pointIndex, IEnumerable<short> lineIndexes)
        {
            var issues = new List<GeometryIssue>();
            var lines = lineIndexes.ToList();
            var polygonIndexes = new HashSet<short>();

            foreach (var lineIndex in lines)
            {
                var line = level.LineList[lineIndex];
                if (line.clockwise_polygon_owner != cstypes.NONE)
                {
                    polygonIndexes.Add(line.clockwise_polygon_owner);
                }

                if (line.counterclockwise_polygon_owner != cstypes.NONE)
                {
                    polygonIndexes.Add(line.counterclockwise_polygon_owner);
                }

                CheckLine(level, lineIndex, issues);
            }

            var lineCounts = LineCountsAtPoints(level);
            var straightCornerPoints = new HashSet<short>();
            foreach (var polygonIndex in polygonIndexes)
            {
                CheckPolygon(level, polygonIndex, issues, lineCounts, straightCornerPoints);
            }

            CheckPointNearLines(level, pointIndex, issues);

            return issues;
        }

        // A polygon with a line of no length is checked as it'll be once the line collapses (without that corner)
        public static void CheckPolygon(MapLevel level, short polygonIndex, List<GeometryIssue> issues, int[] lineCounts, HashSet<short> straightCornerPoints)
        {
            var polygon = level.PolygonList[polygonIndex];
            var corners = new List<int>();

            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var previous = Vertex(level, polygon.endpoint_indexes[(i + polygon.vertex_count - 1) % polygon.vertex_count]);
                var current = Vertex(level, polygon.endpoint_indexes[i]);
                if (previous.x != current.x || previous.y != current.y)
                {
                    corners.Add(i);
                }
            }

            var count = corners.Count;
            if (count < 3)
            {
                // A line of no length collapses it away; otherwise (all its corners are in one place) it has no area
                if (count == polygon.vertex_count)
                {
                    issues.Add(PolygonIssue(GeometryIssueKind.PolygonWithoutArea, polygonIndex, polygon));
                }

                return;
            }

            long area = 0;
            for (var i = 0; i < count; i++)
            {
                var a = Vertex(level, polygon.endpoint_indexes[corners[i]]);
                var b = Vertex(level, polygon.endpoint_indexes[corners[(i + 1) % count]]);
                area += (long) a.x * b.y - (long) b.x * a.y;
            }

            if (area == 0)
            {
                issues.Add(PolygonIssue(GeometryIssueKind.PolygonWithoutArea, polygonIndex, polygon));
                return;
            }

            // Clockwise as the map shows it (its Y points down) is a positive area here
            if (area < 0)
            {
                issues.Add(PolygonIssue(GeometryIssueKind.InsideOutPolygon, polygonIndex, polygon));
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var previousIndex = polygon.endpoint_indexes[corners[(i + count - 1) % count]];
                var pointIndex = polygon.endpoint_indexes[corners[i]];
                var nextIndex = polygon.endpoint_indexes[corners[(i + 1) % count]];

                var previous = Vertex(level, previousIndex);
                var current = Vertex(level, pointIndex);
                var next = Vertex(level, nextIndex);

                var cross = Cross(previous, current, next);
                var dot = (long) (current.x - previous.x) * (next.x - current.x) + (long) (current.y - previous.y) * (next.y - current.y);

                if (cross < 0 || (cross == 0 && dot < 0))
                {
                    // Bending back on itself (or turning back along its edge)
                    var issue = new GeometryIssue { Kind = GeometryIssueKind.ConcaveCorner, Polygon = polygonIndex, Point = pointIndex };
                    issue.Segments.Add((previousIndex, pointIndex));
                    issue.Segments.Add((pointIndex, nextIndex));
                    issues.Add(issue);
                }
                else if (cross == 0 && straightCornerPoints.Add(pointIndex) && IsRedundantStraightCorner(level, polygonIndex, corners[i], lineCounts))
                {
                    var issue = new GeometryIssue { Kind = GeometryIssueKind.RedundantStraightCorner, Polygon = polygonIndex, Point = pointIndex };
                    issue.Segments.Add((previousIndex, pointIndex));
                    issue.Segments.Add((pointIndex, nextIndex));
                    issues.Add(issue);
                }
            }

            // Edges that cross (which can happen with every corner turning the same way, as in a star)
            for (var i = 0; i < count; i++)
            {
                for (var j = i + 2; j < count; j++)
                {
                    if (i == 0 && j == count - 1)
                    {
                        continue;
                    }

                    var a = polygon.endpoint_indexes[corners[i]];
                    var b = polygon.endpoint_indexes[corners[(i + 1) % count]];
                    var c = polygon.endpoint_indexes[corners[j]];
                    var d = polygon.endpoint_indexes[corners[(j + 1) % count]];

                    if (SegmentsCross(Vertex(level, a), Vertex(level, b), Vertex(level, c), Vertex(level, d)))
                    {
                        var issue = new GeometryIssue
                        {
                            Kind = GeometryIssueKind.CrossingEdges,
                            Polygon = polygonIndex,
                            Line = polygon.line_indexes[corners[i]],
                            OtherLine = polygon.line_indexes[corners[j]],
                        };
                        issue.Segments.Add((a, b));
                        issue.Segments.Add((c, d));
                        issues.Add(issue);
                    }
                }
            }
        }

        public static void CheckLine(MapLevel level, short lineIndex, List<GeometryIssue> issues)
        {
            var line = level.LineList[lineIndex];
            var a = Vertex(level, line.endpoint_indexes[0]);
            var b = Vertex(level, line.endpoint_indexes[1]);

            GeometryIssueKind kind;
            if (a.x == b.x && a.y == b.y)
            {
                kind = GeometryIssueKind.ZeroLengthLine;
            }
            else if (Length(a, b) > short.MaxValue)
            {
                kind = GeometryIssueKind.LineTooLong;
            }
            else
            {
                return;
            }

            var issue = new GeometryIssue { Kind = kind, Line = lineIndex, Point = line.endpoint_indexes[0] };
            issue.Segments.Add((line.endpoint_indexes[0], line.endpoint_indexes[1]));
            issues.Add(issue);
        }

        // Near the inside of a line of a polygon beside its own (not one it's a corner of), without being on it: what's
        // meant to be a T-junction, missed. Overlapping space (polygons that aren't beside each other) isn't checked.
        // (The polygons it's a corner of are found, unless they're given)
        public static void CheckPointNearLines(MapLevel level, short pointIndex, List<GeometryIssue> issues, ICollection<short> ownPolygons = null)
        {
            if (ownPolygons == null)
            {
                ownPolygons = new HashSet<short>();
                for (short polygonIndex = 0; polygonIndex < level.PolygonList.Count; polygonIndex++)
                {
                    var polygon = level.PolygonList[polygonIndex];
                    if (Array.IndexOf(polygon.endpoint_indexes, pointIndex, 0, polygon.vertex_count) >= 0)
                    {
                        ownPolygons.Add(polygonIndex);
                    }
                }
            }

            var neighbors = new HashSet<short>();
            foreach (var polygonIndex in ownPolygons)
            {
                var polygon = level.PolygonList[polygonIndex];
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    var neighbor = polygon.adjacent_polygon_indexes[i];
                    if (neighbor >= 0 && neighbor < level.PolygonList.Count && !ownPolygons.Contains(neighbor))
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }

            var point = Vertex(level, pointIndex);
            GeometryIssue nearest = null;
            var nearestDistance = double.MaxValue;

            foreach (var polygonIndex in neighbors)
            {
                var polygon = level.PolygonList[polygonIndex];
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    var lineIndex = polygon.line_indexes[i];
                    var line = level.LineList[lineIndex];
                    if (line.endpoint_indexes[0] == pointIndex || line.endpoint_indexes[1] == pointIndex)
                    {
                        continue;
                    }

                    var a = Vertex(level, line.endpoint_indexes[0]);
                    var b = Vertex(level, line.endpoint_indexes[1]);
                    double dx = b.x - a.x, dy = b.y - a.y;
                    var lengthSquared = dx * dx + dy * dy;
                    if (lengthSquared == 0)
                    {
                        continue;
                    }

                    var along = ((point.x - a.x) * dx + (point.y - a.y) * dy) / lengthSquared;
                    if (along <= 0 || along >= 1)
                    {
                        continue;
                    }

                    var distance = Math.Abs((point.x - a.x) * dy - (point.y - a.y) * dx) / Math.Sqrt(lengthSquared);
                    if (distance > 0 && distance < NearLineDistance && distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = new GeometryIssue { Kind = GeometryIssueKind.PointNearLine, Point = pointIndex, Line = lineIndex, Polygon = polygonIndex };
                        nearest.Segments.Add((line.endpoint_indexes[0], line.endpoint_indexes[1]));
                    }
                }
            }

            if (nearest != null)
            {
                issues.Add(nearest);
            }
        }

        public static void CheckMapIndexes(MapLevel level, List<GeometryIssue> issues)
        {
            var count = level.MapIndexList.Count;
            if (count > MaximumAlephOneMapIndexes)
            {
                issues.Add(new GeometryIssue { Kind = GeometryIssueKind.TooManyMapIndexes, Count = count });
            }
            else if (count > MaximumMarathonMapIndexes)
            {
                issues.Add(new GeometryIssue { Kind = GeometryIssueKind.MapIndexesAlephOneOnly, Count = count });
            }
        }

        // The polygons each point is a corner of
        public static List<short>[] PolygonsAtPoints(MapLevel level)
        {
            var polygons = new List<short>[level.EndpointList.Count];
            for (var i = 0; i < polygons.Length; i++)
            {
                polygons[i] = new List<short>(4);
            }

            for (short polygonIndex = 0; polygonIndex < level.PolygonList.Count; polygonIndex++)
            {
                var polygon = level.PolygonList[polygonIndex];
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    var pointIndex = polygon.endpoint_indexes[i];
                    if (pointIndex >= 0 && pointIndex < polygons.Length && !polygons[pointIndex].Contains(polygonIndex))
                    {
                        polygons[pointIndex].Add(polygonIndex);
                    }
                }
            }

            return polygons;
        }

        // How many lines meet at each point
        public static int[] LineCountsAtPoints(MapLevel level)
        {
            var counts = new int[level.EndpointList.Count];
            foreach (var line in level.LineList)
            {
                counts[line.endpoint_indexes[0]]++;
                counts[line.endpoint_indexes[1]]++;
            }

            return counts;
        }

        // A point in a straight edge, that nothing else meets, between two lines with the same polygons on each side and the
        // same sides, whose textures carry on across it: removing it wouldn't change anything (rather than one that splits a
        // wall's textures, their alignment, or its neighbors)
        // Whether a point could be removed without changing anything: its only two lines are in line, on either side of
        // it, and it's a redundant straight corner of each of its polygons
        public static bool IsRemovableStraightCorner(MapLevel level, short pointIndex)
        {
            var lineCounts = LineCountsAtPoints(level);
            if (pointIndex < 0 || pointIndex >= lineCounts.Length || lineCounts[pointIndex] != 2)
            {
                return false;
            }

            var hasPolygon = false;
            for (short polygonIndex = 0; polygonIndex < level.PolygonList.Count; polygonIndex++)
            {
                var polygon = level.PolygonList[polygonIndex];
                var corner = Array.IndexOf(polygon.endpoint_indexes, pointIndex, 0, polygon.vertex_count);
                if (corner < 0)
                {
                    continue;
                }

                hasPolygon = true;

                var previous = Vertex(level, polygon.endpoint_indexes[(corner + polygon.vertex_count - 1) % polygon.vertex_count]);
                var current = Vertex(level, pointIndex);
                var next = Vertex(level, polygon.endpoint_indexes[(corner + 1) % polygon.vertex_count]);
                var dot = (long) (current.x - previous.x) * (next.x - current.x) + (long) (current.y - previous.y) * (next.y - current.y);

                if (Cross(previous, current, next) != 0 || dot <= 0 || polygon.vertex_count <= 3 ||
                    !IsRedundantStraightCorner(level, polygonIndex, corner, lineCounts))
                {
                    return false;
                }
            }

            return hasPolygon;
        }

        private static bool IsRedundantStraightCorner(MapLevel level, short polygonIndex, int corner, int[] lineCounts)
        {
            var polygon = level.PolygonList[polygonIndex];
            if (lineCounts[polygon.endpoint_indexes[corner]] != 2)
            {
                return false;
            }

            var before = level.LineList[polygon.line_indexes[(corner + polygon.vertex_count - 1) % polygon.vertex_count]];
            var after = level.LineList[polygon.line_indexes[corner]];
            var otherBefore = before.clockwise_polygon_owner == polygonIndex ? before.counterclockwise_polygon_owner : before.clockwise_polygon_owner;
            var otherAfter = after.clockwise_polygon_owner == polygonIndex ? after.counterclockwise_polygon_owner : after.clockwise_polygon_owner;

            var pointIndex = polygon.endpoint_indexes[corner];

            return otherBefore == otherAfter && before.flags == after.flags &&
                   HaveSameSides(level, before, after, polygonIndex) && TexturesContinue(level, before, after, polygonIndex, pointIndex) &&
                   (otherBefore == cstypes.NONE || (HaveSameSides(level, before, after, otherBefore) && TexturesContinue(level, before, after, otherBefore, pointIndex)));
        }

        // A face's textures are at the same place in themselves at the point, from either line (each is offset from its
        // face's start: a line's first point for its clockwise side, and its second for its other side)
        private static bool TexturesContinue(MapLevel level, line_data before, line_data after, short polygonIndex, short pointIndex)
        {
            var sideBefore = before.clockwise_polygon_owner == polygonIndex ? before.clockwise_polygon_side_index : before.counterclockwise_polygon_side_index;
            var sideAfter = after.clockwise_polygon_owner == polygonIndex ? after.clockwise_polygon_side_index : after.counterclockwise_polygon_side_index;
            if (sideBefore < 0 || sideAfter < 0)
            {
                return true;
            }

            var point = Vertex(level, pointIndex);
            double DistanceFromStart(line_data line)
            {
                var start = Vertex(level, line.clockwise_polygon_owner == polygonIndex ? line.endpoint_indexes[0] : line.endpoint_indexes[1]);

                return Length(start, point);
            }

            var distanceBefore = DistanceFromStart(before);
            var distanceAfter = DistanceFromStart(after);
            var a = level.SideList[sideBefore];
            var b = level.SideList[sideAfter];

            bool Continues(side_texture_definition textureBefore, side_texture_definition textureAfter)
            {
                if (textureBefore.texture == cstypes.UNONE)
                {
                    return true;
                }

                var difference = (textureBefore.x0 + distanceBefore) - (textureAfter.x0 + distanceAfter);
                var wrapped = difference - Math.Round(difference / world.WORLD_ONE) * world.WORLD_ONE;

                return textureBefore.y0 == textureAfter.y0 && Math.Abs(wrapped) <= 1.0;
            }

            return Continues(a.primary_texture, b.primary_texture) &&
                   Continues(a.secondary_texture, b.secondary_texture) &&
                   Continues(a.transparent_texture, b.transparent_texture);
        }

        private static bool HaveSameSides(MapLevel level, line_data before, line_data after, short polygonIndex)
        {
            var sideBefore = before.clockwise_polygon_owner == polygonIndex ? before.clockwise_polygon_side_index : before.counterclockwise_polygon_side_index;
            var sideAfter = after.clockwise_polygon_owner == polygonIndex ? after.clockwise_polygon_side_index : after.counterclockwise_polygon_side_index;
            if (sideBefore < 0 || sideAfter < 0)
            {
                return sideBefore == sideAfter;
            }

            var a = level.SideList[sideBefore];
            var b = level.SideList[sideAfter];

            return a.type == b.type && a.flags == b.flags &&
                   a.primary_texture.texture == b.primary_texture.texture &&
                   a.secondary_texture.texture == b.secondary_texture.texture &&
                   a.transparent_texture.texture == b.transparent_texture.texture &&
                   a.primary_transfer_mode == b.primary_transfer_mode &&
                   a.secondary_transfer_mode == b.secondary_transfer_mode &&
                   a.transparent_transfer_mode == b.transparent_transfer_mode &&
                   a.primary_lightsource_index == b.primary_lightsource_index &&
                   a.secondary_lightsource_index == b.secondary_lightsource_index &&
                   a.transparent_lightsource_index == b.transparent_lightsource_index &&
                   a.control_panel_type == b.control_panel_type &&
                   a.control_panel_permutation == b.control_panel_permutation &&
                   a.ambient_delta == b.ambient_delta;
        }

        private static GeometryIssue PolygonIssue(GeometryIssueKind kind, short polygonIndex, polygon_data polygon)
        {
            var issue = new GeometryIssue { Kind = kind, Polygon = polygonIndex };
            for (var i = 0; i < polygon.vertex_count; i++)
            {
                issue.Segments.Add((polygon.endpoint_indexes[i], polygon.endpoint_indexes[(i + 1) % polygon.vertex_count]));
            }

            return issue;
        }

        private static world_point2d Vertex(MapLevel level, short pointIndex)
        {
            return level.EndpointList[pointIndex].vertex;
        }

        private static double Length(world_point2d a, world_point2d b)
        {
            double dx = b.x - a.x, dy = b.y - a.y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        // Positive for a clockwise turn (as the map shows it)
        private static long Cross(world_point2d a, world_point2d b, world_point2d c)
        {
            return (long) (b.x - a.x) * (c.y - b.y) - (long) (b.y - a.y) * (c.x - b.x);
        }

        // Crossing through each other (touching, or overlapping along a line, isn't crossing)
        private static bool SegmentsCross(world_point2d a, world_point2d b, world_point2d c, world_point2d d)
        {
            return Math.Sign(Orientation(a, b, c)) * Math.Sign(Orientation(a, b, d)) < 0 &&
                   Math.Sign(Orientation(c, d, a)) * Math.Sign(Orientation(c, d, b)) < 0;
        }

        private static long Orientation(world_point2d a, world_point2d b, world_point2d c)
        {
            return (long) (b.x - a.x) * (c.y - a.y) - (long) (b.y - a.y) * (c.x - a.x);
        }
    }
}
#endif
