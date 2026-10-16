#if !NO_EDITING
using AlephOne;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Steps shared by the tools that add geometry (LineExtrusion, LineDrawing, PolygonClosing): adding points, lines,
    // polygons and sides to the level's data, and telling whether a new line would cross or touch what's already there.
    // Locations are in world units.
    public static class GeometryConstruction
    {
        public static short AddEndpoint(MapLevel data, world_point2d location)
        {
            data.EndpointList.Add(new endpoint_data
            {
                vertex = location,
                supporting_polygon_index = cstypes.NONE,
            });

            return (short) (data.EndpointList.Count - 1);
        }

        // Between the points, with the polygons (or NONE) on its sides, and no sides yet
        public static short AddLine(MapLevel data, short firstIndex, short lastIndex, short clockwiseOwner, short counterclockwiseOwner)
        {
            var line = new line_data
            {
                clockwise_polygon_owner = clockwiseOwner,
                counterclockwise_polygon_owner = counterclockwiseOwner,
                clockwise_polygon_side_index = cstypes.NONE,
                counterclockwise_polygon_side_index = cstypes.NONE,
            };

            line.endpoint_indexes[0] = firstIndex;
            line.endpoint_indexes[1] = lastIndex;
            line.length = world.distance2d(data.EndpointList[firstIndex].vertex, data.EndpointList[lastIndex].vertex);

            data.LineList.Add(line);

            return (short) (data.LineList.Count - 1);
        }

        // A plain polygon (not a platform, and with no media or sounds) with the corners (clockwise) and the lines from each
        // to the next, and the source's floor and ceiling: their heights, textures, lights, transfer modes and origins (so
        // its textures line up with the source's). Its sides and the data worked out from its shape are left to the caller.
        public static polygon_data NewPolygonLike(polygon_data source, IReadOnlyList<short> corners, IReadOnlyList<short> lines)
        {
            var polygon = new polygon_data
            {
                type = map._polygon_is_normal,
                vertex_count = (ushort) corners.Count,
                floor_texture = source.floor_texture,
                ceiling_texture = source.ceiling_texture,
                floor_height = source.floor_height,
                ceiling_height = source.ceiling_height,
                floor_lightsource_index = source.floor_lightsource_index,
                ceiling_lightsource_index = source.ceiling_lightsource_index,
                floor_transfer_mode = source.floor_transfer_mode,
                ceiling_transfer_mode = source.ceiling_transfer_mode,
                floor_origin = source.floor_origin,
                ceiling_origin = source.ceiling_origin,
                first_object = cstypes.NONE,
                media_index = cstypes.NONE,
                media_lightsource_index = source.media_lightsource_index,
                sound_source_indexes = cstypes.NONE,
                ambient_sound_image_index = cstypes.NONE,
                random_sound_image_index = cstypes.NONE,
            };

            for (var i = 0; i < map.MAXIMUM_VERTICES_PER_POLYGON; i++)
            {
                polygon.endpoint_indexes[i] = i < corners.Count ? corners[i] : cstypes.NONE;
                polygon.line_indexes[i] = i < lines.Count ? lines[i] : cstypes.NONE;
                polygon.adjacent_polygon_indexes[i] = cstypes.NONE;
                polygon.side_indexes[i] = cstypes.NONE;
            }

            return polygon;
        }

        // A copy of the source side (its textures, lights and transfer modes, but not a control panel), or a blank one if
        // there's none (map_constructors.cpp: new_side), on the polygon's side of the line (which the polygon owns), of the
        // type its face calls for (FaceType: full where it only fills the opening). Returns its index.
        public static short AddSide(MapLevel data, short polygonIndex, short lineIndex, side_data source)
        {
            if (source == null)
            {
                var blankIndex = map_constructors.new_side(data, polygonIndex, lineIndex);
                data.SideList[blankIndex].type = SideTypeForHeights(data, blankIndex);
                return blankIndex;
            }

            var line = data.LineList[lineIndex];
            var side = source.Clone();
            side.flags = 0;
            side.control_panel_type = 0;
            side.control_panel_permutation = 0;
            side.polygon_index = polygonIndex;
            side.line_index = lineIndex;

            var sideIndex = (short) data.SideList.Count;
            data.SideList.Add(side);

            if (line.clockwise_polygon_owner == polygonIndex)
            {
                line.clockwise_polygon_side_index = sideIndex;
            }
            else
            {
                line.counterclockwise_polygon_side_index = sideIndex;
            }

            map_constructors.recalculate_redundant_side_data(data, sideIndex, lineIndex);
            side.type = SideTypeForHeights(data, sideIndex);

            return sideIndex;
        }

        // Whether there's a way through the line between its polygons (one on each side) that a side could fill: their
        // heights leave a gap, or one is a platform (which can open it as it moves)
        public static bool HasOpening(MapLevel data, short lineIndex)
        {
            var line = data.LineList[lineIndex];
            if (line.clockwise_polygon_owner == cstypes.NONE || line.counterclockwise_polygon_owner == cstypes.NONE)
            {
                return false;
            }

            var a = data.PolygonList[line.clockwise_polygon_owner];
            var b = data.PolygonList[line.counterclockwise_polygon_owner];

            return a.type == map._polygon_is_platform || b.type == map._polygon_is_platform ||
                   System.Math.Min(a.ceiling_height, b.ceiling_height) > System.Math.Max(a.floor_height, b.floor_height);
        }

        // The type a face's side takes (map_constructors.cpp: recalculate_side_type, which works it out for an existing
        // side): high, low or split for the panels its polygons' heights expose (a drop down to a lower ceiling beyond, and
        // a step up to a higher floor beyond, across a platform's whole travel), and full where they expose neither, or
        // there's no polygon beyond (a wall). A side between two polygons whose heights expose no panels (such as a window
        // out of a constriction, or between polygons of the same heights) only has data to keep if it fills the opening,
        // which a full side does (Aleph One draws it floor to ceiling: RenderRasterize.cpp, render_node), so it's full, or
        // there's no side there at all.
        public static short FaceType(MapLevel data, short lineIndex, bool isClockwise)
        {
            var line = data.LineList[lineIndex];
            var facingIndex = isClockwise ? line.clockwise_polygon_owner : line.counterclockwise_polygon_owner;
            var beyondIndex = isClockwise ? line.counterclockwise_polygon_owner : line.clockwise_polygon_owner;

            if (facingIndex == cstypes.NONE || beyondIndex == cstypes.NONE)
            {
                return map._full_side;
            }

            var facing = data.PolygonList[facingIndex];
            var beyond = data.PolygonList[beyondIndex];

            int ceiling = facing.ceiling_height;
            int floor = facing.floor_height;
            if (facing.type == map._polygon_is_platform)
            {
                var platform = platforms.get_platform_data(data, facing.permutation);
                ceiling = platform.maximum_ceiling_height;
                floor = platform.minimum_floor_height;
            }

            int beyondCeiling = beyond.ceiling_height;
            int beyondFloor = beyond.floor_height;
            if (beyond.type == map._polygon_is_platform)
            {
                var platform = platforms.get_platform_data(data, beyond.permutation);
                beyondCeiling = platform.minimum_ceiling_height;
                beyondFloor = platform.maximum_floor_height;
            }

            if (beyondCeiling < ceiling && beyondFloor > floor)
            {
                return map._split_side;
            }

            if (beyondFloor > floor)
            {
                return map._low_side;
            }

            return beyondCeiling < ceiling ? map._high_side : map._full_side;
        }

        // Whether the face is between two polygons whose heights expose no panels (so a side there only fills the opening)
        public static bool IsWindowFace(MapLevel data, short lineIndex, bool isClockwise)
        {
            var line = data.LineList[lineIndex];

            return line.clockwise_polygon_owner != cstypes.NONE && line.counterclockwise_polygon_owner != cstypes.NONE &&
                   FaceType(data, lineIndex, isClockwise) == map._full_side;
        }

        // The type the side's face calls for (FaceType)
        public static short SideTypeForHeights(MapLevel data, short sideIndex)
        {
            var side = data.SideList[sideIndex];
            var line = data.LineList[side.line_index];

            return FaceType(data, side.line_index, line.clockwise_polygon_side_index == sideIndex);
        }

        // A side of the polygon to copy textures and lights from (one with a primary texture, other than on the excluded
        // line): on one of the preferred lines (as it is now), or else on a line at one of the points, or else any of its
        // sides (or null, if it has none)
        public static side_data FindSideToCopy(MapLevel data, short polygonIndex, ICollection<short> preferredLines, ICollection<short> nearPoints, short excludedLine = cstypes.NONE)
        {
            var polygon = data.PolygonList[polygonIndex];
            side_data fallback = null;
            side_data nearPoint = null;

            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var lineIndex = polygon.line_indexes[i];
                var line = data.LineList[lineIndex];
                var sideIndex = line.clockwise_polygon_owner == polygonIndex ? line.clockwise_polygon_side_index : line.counterclockwise_polygon_side_index;
                if (sideIndex == cstypes.NONE || lineIndex == excludedLine || data.SideList[sideIndex].primary_texture.texture == cstypes.UNONE)
                {
                    continue;
                }

                var side = data.SideList[sideIndex];
                if (preferredLines.Contains(lineIndex))
                {
                    return side.Clone();
                }

                if (nearPoint == null && (nearPoints.Contains(line.endpoint_indexes[0]) || nearPoints.Contains(line.endpoint_indexes[1])))
                {
                    nearPoint = side;
                }

                fallback = fallback ?? side;
            }

            return (nearPoint ?? fallback)?.Clone();
        }

        // Once geometry is added: the data worked out from the polygons (the new ones, and those beside them, whose corners,
        // neighbors and sides change), and from the lines the addition touched (their sides' types and exclusion zones,
        // their flags, and their points). Other lines and sides are left as they were (as the level has them, which may
        // differ from what they'd be worked out to be, such as a high side where map_constructors.cpp would make a full
        // one), then the platforms beside the polygons and the map indexes are worked out again.
        public static void RecalculateAfterAdding(MapLevel data, IEnumerable<polygon_data> polygons, IEnumerable<short> touchedLines)
        {
            var polygonIndexes = new HashSet<short>();
            foreach (var polygon in polygons)
            {
                var polygonIndex = (short) data.PolygonList.IndexOf(polygon);
                if (polygonIndex >= 0 && polygonIndexes.Add(polygonIndex))
                {
                    map_constructors.recalculate_redundant_polygon_data(data, polygonIndex);
                }
            }

            var pointIndexes = new HashSet<short>();
            foreach (var lineIndex in new HashSet<short>(touchedLines))
            {
                var line = data.LineList[lineIndex];
                map_constructors.recalculate_redundant_line_data(data, lineIndex);

                foreach (var sideIndex in new[] { line.clockwise_polygon_side_index, line.counterclockwise_polygon_side_index })
                {
                    if (sideIndex != cstypes.NONE)
                    {
                        data.SideList[sideIndex].type = SideTypeForHeights(data, sideIndex);
                        LineFlagsEditing.UpdateForSide(data, data.SideList[sideIndex]);
                    }
                }

                pointIndexes.Add(line.endpoint_indexes[0]);
                pointIndexes.Add(line.endpoint_indexes[1]);
            }

            foreach (var pointIndex in pointIndexes)
            {
                map_constructors.recalculate_redundant_endpoint_data(data, pointIndex);
            }

            HeightsEditing.RefreshPlatformsAround(data, polygonIndexes);
            LevelEditing.RecalculateMapIndexes(data);
        }

        // Whether a new line from start to end would cross or touch the level's lines (other than the ignored one). Its
        // ends may be existing points (their indexes, or NONE): lines that meet it there only count if they run along it,
        // and a line already joining both counts.
        public static bool SegmentTouchesLines(MapLevel data, Vector2Int start, Vector2Int end, short startIndex, short endIndex, short ignoredLineIndex = cstypes.NONE)
        {
            for (short lineIndex = 0; lineIndex < data.LineList.Count; lineIndex++)
            {
                if (lineIndex == ignoredLineIndex)
                {
                    continue;
                }

                var line = data.LineList[lineIndex];
                var p = ToVector(data.EndpointList[line.endpoint_indexes[0]].vertex);
                var q = ToVector(data.EndpointList[line.endpoint_indexes[1]].vertex);

                var sharesStart = startIndex != cstypes.NONE && (line.endpoint_indexes[0] == startIndex || line.endpoint_indexes[1] == startIndex);
                var sharesEnd = endIndex != cstypes.NONE && (line.endpoint_indexes[0] == endIndex || line.endpoint_indexes[1] == endIndex);

                if (sharesStart && sharesEnd)
                {
                    return true;
                }

                if (sharesStart || sharesEnd)
                {
                    // Meeting at a point, they only overlap if they run along each other
                    var sharedIndex = sharesStart ? startIndex : endIndex;
                    var lineFar = line.endpoint_indexes[0] == sharedIndex ? q : p;
                    var segmentFar = sharesStart ? end : start;

                    if (IsOnSegment(start, end, lineFar) || IsOnSegment(p, q, segmentFar))
                    {
                        return true;
                    }
                }
                else if (SegmentsTouch(start, end, p, q))
                {
                    return true;
                }
            }

            return false;
        }

        // Whether any point (other than its ends) is on the segment, such as one no line reaches
        public static bool SegmentTouchesPoints(MapLevel data, Vector2Int start, Vector2Int end, short startIndex, short endIndex)
        {
            for (short pointIndex = 0; pointIndex < data.EndpointList.Count; pointIndex++)
            {
                if (pointIndex != startIndex && pointIndex != endIndex && IsOnSegment(start, end, ToVector(data.EndpointList[pointIndex].vertex)))
                {
                    return true;
                }
            }

            return false;
        }

        // Twice the area its corners enclose, positive for corners that go clockwise as the map shows them (its Y down)
        public static long SignedArea(MapLevel data, polygon_data polygon)
        {
            long area = 0;
            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var current = data.EndpointList[polygon.endpoint_indexes[i]].vertex;
                var next = data.EndpointList[polygon.endpoint_indexes[(i + 1) % polygon.vertex_count]].vertex;
                area += (long) current.x * next.y - (long) next.x * current.y;
            }

            return area;
        }

        // Strictly inside the (convex) polygon, not on its edges
        public static bool IsInsidePolygon(MapLevel data, polygon_data polygon, Vector2 location)
        {
            var orientation = Mathf.Sign(SignedArea(data, polygon));
            if (orientation == 0f || polygon.vertex_count < 3)
            {
                return false;
            }

            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var current = data.EndpointList[polygon.endpoint_indexes[i]].vertex;
                var next = data.EndpointList[polygon.endpoint_indexes[(i + 1) % polygon.vertex_count]].vertex;
                var cross = ((double) next.x - current.x) * (location.y - current.y) - ((double) next.y - current.y) * (location.x - current.x);

                if (cross * orientation <= 0d)
                {
                    return false;
                }
            }

            return true;
        }

        public static Vector2Int ToVector(world_point2d point)
        {
            return new Vector2Int(point.x, point.y);
        }

        public static long Cross(Vector2Int origin, Vector2Int a, Vector2Int b)
        {
            return (long) (a.x - origin.x) * (b.y - origin.y) - (long) (a.y - origin.y) * (b.x - origin.x);
        }

        // On the segment, including its ends
        public static bool IsOnSegment(Vector2Int start, Vector2Int end, Vector2Int point)
        {
            return Cross(start, end, point) == 0 &&
                   point.x >= Mathf.Min(start.x, end.x) && point.x <= Mathf.Max(start.x, end.x) &&
                   point.y >= Mathf.Min(start.y, end.y) && point.y <= Mathf.Max(start.y, end.y);
        }

        // Crossing or touching
        public static bool SegmentsTouch(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
        {
            var d1 = System.Math.Sign(Cross(c, d, a));
            var d2 = System.Math.Sign(Cross(c, d, b));
            var d3 = System.Math.Sign(Cross(a, b, c));
            var d4 = System.Math.Sign(Cross(a, b, d));

            if (d1 * d2 < 0 && d3 * d4 < 0)
            {
                return true;
            }

            return d1 == 0 && IsOnSegment(c, d, a) ||
                   d2 == 0 && IsOnSegment(c, d, b) ||
                   d3 == 0 && IsOnSegment(a, b, c) ||
                   d4 == 0 && IsOnSegment(a, b, d);
        }
    }
}
#endif
