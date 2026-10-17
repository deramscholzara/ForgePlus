#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AlephOne.map;

namespace ForgePlus.LevelManipulation
{
    // A line drawn to an existing point (LineDrawing) can close a loop of lines into a new polygon, on either side of it.
    // Each side's loop is found by following the lines around from the new line's end, turning as tightly as possible
    // toward that side at each point, until it's back at the new line's start. It becomes a polygon when it has at most
    // eight corners (Marathon's limit) and is convex, nothing is inside it, and none of its lines already has a polygon on
    // its side. The polygon copies the floor and ceiling (heights, textures, lights) of a polygon at the point the line
    // was drawn to (one beside the new polygon, if any, or the one with the lowest floor), and its walls copy that
    // polygon's side, while lines it shares with other polygons are opened between them (unless their heights close
    // them), with sides added or removed as the heights between them expose or hide them (SideExposure).
    public static class PolygonClosing
    {
        // A loop that can become a polygon: its corners (clockwise) and the lines from each to the next (NONE for a new
        // line not yet added), and the polygon it copies
        public sealed class Loop
        {
            public readonly List<short> Corners = new List<short>();
            public readonly List<short> Lines = new List<short>();
            public short Source;
        }

        // The loops a new line (NONE if it isn't added yet) from start to end closes
        public static List<Loop> FindLoops(MapLevel data, List<short>[] endpointLines, short startIndex, short endIndex, short newLineIndex)
        {
            var loops = new List<Loop>();

            foreach (var turnsLeft in new[] { true, false })
            {
                var loop = TryFollow(data, endpointLines, startIndex, endIndex, newLineIndex, turnsLeft, requiresSource: true);
                if (loop != null && !loops.Any(other => new HashSet<short>(other.Corners).SetEquals(loop.Corners)))
                {
                    loops.Add(loop);
                }
            }

            return loops;
        }

        // Every loop of existing lines that could be filled with a polygon (FillablePolygons): followed from each line
        // with no polygon on a side. A loop with no polygon at any of its corners has no source (NONE) to copy, and is
        // filled with a plain one (AddPolygon).
        public static List<Loop> FindEmptyLoops(MapLevel data, List<short>[] endpointLines)
        {
            var loops = new List<Loop>();
            var found = new HashSet<string>();

            for (short lineIndex = 0; lineIndex < data.LineList.Count; lineIndex++)
            {
                var line = data.LineList[lineIndex];
                if (line.clockwise_polygon_owner != cstypes.NONE && line.counterclockwise_polygon_owner != cstypes.NONE)
                {
                    continue;
                }

                foreach (var turnsLeft in new[] { true, false })
                {
                    var loop = TryFollow(data, endpointLines, line.endpoint_indexes[0], line.endpoint_indexes[1], lineIndex, turnsLeft, requiresSource: false);
                    if (loop != null && found.Add(string.Join(",", loop.Corners.OrderBy(corner => corner))))
                    {
                        loops.Add(loop);
                    }
                }
            }

            return loops;
        }

        private static Loop TryFollow(MapLevel data, List<short>[] endpointLines, short startIndex, short endIndex, short newLineIndex, bool turnsLeft, bool requiresSource)
        {
            var corners = new List<short> { startIndex, endIndex };
            var lines = new List<short> { newLineIndex };

            var previous = startIndex;
            var current = endIndex;
            var incomingLine = newLineIndex;

            while (current != startIndex)
            {
                if (corners.Count > MAXIMUM_VERTICES_PER_POLYGON || current < 0 || current >= endpointLines.Length)
                {
                    return null;
                }

                var here = Location(data, current);
                var incoming = here - Location(data, previous);

                var bestLine = cstypes.NONE;
                var bestNext = cstypes.NONE;
                var bestTurn = 0d;

                foreach (var lineIndex in endpointLines[current])
                {
                    if (lineIndex == incomingLine)
                    {
                        continue;
                    }

                    var line = data.LineList[lineIndex];
                    var next = line.endpoint_indexes[0] == current ? line.endpoint_indexes[1] : line.endpoint_indexes[0];
                    var outgoing = Location(data, next) - here;

                    // How far it turns from the way the loop came (from straight back around to straight back)
                    var turn = System.Math.Atan2(incoming.x * outgoing.y - incoming.y * outgoing.x, incoming.x * outgoing.x + incoming.y * outgoing.y);
                    if (bestLine == cstypes.NONE || (turnsLeft ? turn > bestTurn : turn < bestTurn))
                    {
                        bestLine = lineIndex;
                        bestNext = next;
                        bestTurn = turn;
                    }
                }

                // A dead end, or back on itself (other than at the start)
                if (bestLine == cstypes.NONE || (bestNext != startIndex && corners.Contains(bestNext)))
                {
                    return null;
                }

                lines.Add(bestLine);
                if (bestNext != startIndex)
                {
                    corners.Add(bestNext);
                }

                previous = current;
                current = bestNext;
                incomingLine = bestLine;
            }

            return AsPolygon(data, corners, lines, endIndex, requiresSource);
        }

        // As a polygon's corners (clockwise, as the level's polygons go), if it can be one
        private static Loop AsPolygon(MapLevel data, List<short> corners, List<short> lines, short endIndex, bool requiresSource)
        {
            var count = corners.Count;
            if (count < 3 || count > MAXIMUM_VERTICES_PER_POLYGON)
            {
                return null;
            }

            // Clockwise (as the map shows them) is a positive area, as in the level's polygons
            var clockwiseSign = 1L;
            foreach (var polygon in data.PolygonList)
            {
                var polygonArea = GeometryConstruction.SignedArea(data, polygon);
                if (polygonArea != 0)
                {
                    clockwiseSign = System.Math.Sign(polygonArea);
                    break;
                }
            }

            var loop = new Loop();
            var area = SignedArea(data, corners);
            if (area == 0)
            {
                return null;
            }

            if (System.Math.Sign(area) == clockwiseSign)
            {
                loop.Corners.AddRange(corners);
                loop.Lines.AddRange(lines);
            }
            else
            {
                // The other way around: the line from each corner to the next is the one that was before it
                for (var i = 0; i < count; i++)
                {
                    loop.Corners.Add(corners[count - 1 - i]);
                    loop.Lines.Add(i < count - 1 ? lines[count - 2 - i] : lines[count - 1]);
                }
            }

            // Convex (straight corners are allowed)
            for (var i = 0; i < count; i++)
            {
                var a = GeometryConstruction.ToVector(data.EndpointList[loop.Corners[i]].vertex);
                var b = GeometryConstruction.ToVector(data.EndpointList[loop.Corners[(i + 1) % count]].vertex);
                var c = GeometryConstruction.ToVector(data.EndpointList[loop.Corners[(i + 2) % count]].vertex);

                if (GeometryConstruction.Cross(a, b, c) * clockwiseSign < 0)
                {
                    return null;
                }
            }

            // No line already has a polygon on its side
            var neighbors = new List<short>();
            for (var i = 0; i < count; i++)
            {
                var lineIndex = loop.Lines[i];
                if (lineIndex == cstypes.NONE)
                {
                    continue;
                }

                var line = data.LineList[lineIndex];
                var isClockwise = line.endpoint_indexes[0] == loop.Corners[i];
                if (line.GetPolygonOwner(isClockwise) != cstypes.NONE)
                {
                    return null;
                }

                var neighbor = line.GetPolygonOwner(!isClockwise);
                if (neighbor != cstypes.NONE)
                {
                    neighbors.Add(neighbor);
                }
            }

            // Nothing inside it
            var shape = new polygon_data { vertex_count = (ushort) count };
            for (var i = 0; i < count; i++)
            {
                shape.endpoint_indexes[i] = loop.Corners[i];
            }

            for (short pointIndex = 0; pointIndex < data.EndpointList.Count; pointIndex++)
            {
                var vertex = data.EndpointList[pointIndex].vertex;
                if (!loop.Corners.Contains(pointIndex) && GeometryConstruction.IsInsidePolygon(data, shape, new Vector2(vertex.x, vertex.y)))
                {
                    return null;
                }
            }

            loop.Source = FindSource(data, endIndex, loop.Corners, neighbors);

            return loop.Source == cstypes.NONE && requiresSource ? null : loop;
        }

        // A polygon at the point the line was drawn to (beside the loop, if any is, or else the one with the lowest
        // floor), or else at any of its corners
        private static short FindSource(MapLevel data, short endIndex, List<short> corners, List<short> neighbors)
        {
            var best = cstypes.NONE;
            var bestIsBeside = false;

            foreach (var pointIndex in new[] { endIndex }.Concat(corners))
            {
                for (short polygonIndex = 0; polygonIndex < data.PolygonList.Count; polygonIndex++)
                {
                    var polygon = data.PolygonList[polygonIndex];
                    if (System.Array.IndexOf(polygon.endpoint_indexes, pointIndex, 0, polygon.vertex_count) < 0)
                    {
                        continue;
                    }

                    var isBeside = neighbors.Contains(polygonIndex);
                    if (best == cstypes.NONE ||
                        (isBeside && !bestIsBeside) ||
                        (isBeside == bestIsBeside && polygon.floor_height < data.PolygonList[best].floor_height))
                    {
                        best = polygonIndex;
                        bestIsBeside = isBeside;
                    }
                }

                if (best != cstypes.NONE)
                {
                    return best;
                }
            }

            return best;
        }

        // Adds the loop's polygon (its new line is added by now, and in its lines). Returns it. With no source, it's a plain
        // one (PlainPolygon), and so are its walls' textures.
        public static polygon_data AddPolygon(MapLevel data, Loop loop, short drawnToIndex)
        {
            var hasSource = loop.Source != cstypes.NONE;
            var source = hasSource ? data.PolygonList[loop.Source] : PlainPolygon(data);
            var sourceSide = hasSource ? GeometryConstruction.FindSideToCopy(data, loop.Source, loop.Lines, new[] { drawnToIndex }) : PlainSide(source);

            // The polygons beside it, whose sides it may hide or reveal
            var neighbors = new List<polygon_data>();
            var exposure = new SideExposure();
            var polygonIndex = (short) data.PolygonList.Count;

            for (var i = 0; i < loop.Corners.Count; i++)
            {
                var line = data.LineList[loop.Lines[i]];
                var isClockwise = line.endpoint_indexes[0] == loop.Corners[i];
                var neighbor = line.GetPolygonOwner(!isClockwise);

                if (neighbor != cstypes.NONE)
                {
                    neighbors.Add(data.PolygonList[neighbor]);
                    exposure.RecordPolygon(data, data.PolygonList[neighbor]);
                }
            }

            for (var i = 0; i < loop.Corners.Count; i++)
            {
                var line = data.LineList[loop.Lines[i]];
                if (line.endpoint_indexes[0] == loop.Corners[i])
                {
                    line.clockwise_polygon_owner = polygonIndex;
                }
                else
                {
                    line.counterclockwise_polygon_owner = polygonIndex;
                }
            }

            var polygon = GeometryConstruction.NewPolygonLike(source, loop.Corners, loop.Lines);
            data.PolygonList.Add(polygon);

            // Its walls where they're seen (all of those with nothing beyond them)
            foreach (var lineIndex in loop.Lines)
            {
                var line = data.LineList[lineIndex];
                var isClockwise = line.clockwise_polygon_owner == polygonIndex;

                if (line.GetSideIndex(isClockwise) == cstypes.NONE && LevelEntity_Side.IsFaceExposed(data, lineIndex, isClockwise))
                {
                    GeometryConstruction.AddSide(data, polygonIndex, lineIndex, sourceSide);
                }
            }

            // Sides beside it that it hides (or reveals)
            exposure.ApplyToSides(data);

            // Lines it shares open between it and the polygon beyond (unless their heights close them), and the rest are
            // solid
            foreach (var lineIndex in loop.Lines)
            {
                var line = data.LineList[lineIndex];
                var isShared = line.clockwise_polygon_owner != cstypes.NONE && line.counterclockwise_polygon_owner != cstypes.NONE;
                var isClosed = !isShared || LineFlagsEditing.IsClosedByHeights(data, lineIndex);

                LineFlagsEditing.SetSolidity(data, lineIndex, isClosed);
                SET_LINE_TRANSPARENCY(line, !isClosed);
                map_constructors.recalculate_redundant_line_data(data, lineIndex);
            }

            neighbors.Add(polygon);
            GeometryConstruction.RecalculateAfterAdding(data, neighbors, loop.Lines);

            return polygon;
        }

        // The floor height (in world units) a loop's polygon would have
        public static short FloorHeight(MapLevel data, Loop loop)
        {
            return loop.Source != cstypes.NONE ? data.PolygonList[loop.Source].floor_height : (short) 0;
        }

        // What a polygon with nothing to copy is like: its floor at 0 and ceiling a world unit above, textured with the
        // first plain texture of the level's environment's wall collection (PlainWallTexture), and lit by the first light
        // (one is added if the level has none, as every surface needs one)
        private static polygon_data PlainPolygon(MapLevel data)
        {
            if (data.LightList.Count == 0)
            {
                lightsource.new_light(data, lightsource.get_defaults_for_light_type(lightsource._normal_light));
            }

            var texture = PlainWallTexture(ForgePlus.Inspection.CollectionChoices.WallCollectionOf(data.static_world.environment_code));

            return new polygon_data
            {
                floor_height = 0,
                ceiling_height = world.WORLD_ONE,
                floor_texture = texture,
                ceiling_texture = texture,
                floor_transfer_mode = _xfer_normal,
                ceiling_transfer_mode = _xfer_normal,
                floor_lightsource_index = 0,
                ceiling_lightsource_index = 0,
                media_lightsource_index = 0,
            };
        }

        // The collection's first texture that isn't a control panel's (active or inactive) or a liquid's (any of its
        // frames), as those are meant for their own surfaces. The definitions are as MML has them, if it changes them.
        private static ushort PlainWallTexture(int collection)
        {
            var reservedShapes = new HashSet<int>();

            for (short controlPanelType = 0; controlPanelType < devices.NUMBER_OF_CONTROL_PANEL_DEFINITIONS; controlPanelType++)
            {
                var definition = devices.get_control_panel_definition(controlPanelType);
                if (definition != null && shape_descriptors.GET_COLLECTION(definition.collection) == collection)
                {
                    reservedShapes.Add(definition.active_shape);
                    reservedShapes.Add(definition.inactive_shape);
                }
            }

            for (short mediaType = 0; mediaType < media.NUMBER_OF_MEDIA_TYPES; mediaType++)
            {
                var definition = media.get_media_definition(mediaType);
                if (definition != null && shape_descriptors.GET_COLLECTION(definition.collection) == collection)
                {
                    for (var frame = 0; frame < Mathf.Max(1, (int) definition.shape_count); frame++)
                    {
                        reservedShapes.Add(definition.shape + frame);
                    }
                }
            }

            var shape = 0;
            while (reservedShapes.Contains(shape))
            {
                shape++;
            }

            return shape_descriptors.BUILD_DESCRIPTOR(shape_descriptors.BUILD_COLLECTION(collection, 0), shape);
        }

        // Its walls, with the polygon's floor texture and light
        private static side_data PlainSide(polygon_data polygon)
        {
            var side = new side_data();
            side.primary_texture.texture = polygon.floor_texture;
            side.primary_lightsource_index = polygon.floor_lightsource_index;
            side.primary_transfer_mode = _xfer_normal;
            side.secondary_texture.texture = cstypes.UNONE;
            side.secondary_lightsource_index = polygon.floor_lightsource_index;
            side.transparent_texture.texture = cstypes.UNONE;
            side.transparent_lightsource_index = polygon.floor_lightsource_index;

            return side;
        }

        private static Vector2 Location(MapLevel data, short pointIndex)
        {
            var vertex = data.EndpointList[pointIndex].vertex;
            return new Vector2(vertex.x, vertex.y);
        }

        private static long SignedArea(MapLevel data, List<short> corners)
        {
            long area = 0;
            for (var i = 0; i < corners.Count; i++)
            {
                var current = data.EndpointList[corners[i]].vertex;
                var next = data.EndpointList[corners[(i + 1) % corners.Count]].vertex;
                area += (long) current.x * next.y - (long) next.x * current.y;
            }

            return area;
        }
    }
}
#endif
