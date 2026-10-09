// Port of Aleph One: Source_Files/GameWorld/map_constructors.cpp
//
// The redundant-data recalculation (recalculate_redundant_*_data, precalculate_map_indexes and their
// helpers) and the pack/unpack routines for the map's data types, working on a MapLevel.
//
// Not ported: pack/unpack of dynamic_data and object_data (saved games only).
// The #ifdef NEW_AND_BROKEN / WITH_ORIGINAL_DATA_STRUCTURES versions of intersecting_flood_proc are
// commented out in Aleph One and aren't ported.
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csalerts;
using static AlephOne.csmacros;
using static AlephOne.cstypes;
using static AlephOne.editor;
using static AlephOne.FilmProfileGlobals;
using static AlephOne.map;
using static AlephOne.Packing;
using static AlephOne.platforms;
using static AlephOne.world;

namespace AlephOne
{
    [NoAutoStaticsCleanup]
    public static class map_constructors
    {
        /*
        maps of one polygon don't have their impassability information computed

        //detached polygons (i.e., shadows) and their twins will not have their neighbor polygon lists correctly computed
        //adjacent polygons should be precalculated in the polygon structure
        //intersecting_flood_proc can't store side information (using sign) with line index zero
        //keep_line_segment_out_of_walls() can't use precalculated height information and should do weird things next to elevators and doors
        */

        /* ---------- structures */

        private const int MAXIMUM_INTERSECTING_INDEXES = 64;

        private class intersecting_flood_data
        {
            // This stuff now global:
            /*
            short *line_indexes;
            short line_count;

            short *endpoint_indexes;
            short endpoint_count;

            short *polygon_indexes;
            short polygon_count;
            */

            public short original_polygon_index;
            public world_point2d center;

            public int minimum_separation_squared;
        }

        /* ---------- globals */

        // LP: Temporary areas for nearby endpoint/line/polygon finding;
        // OK for this to be global since they replace only single instances.
        private static readonly List<short> LineIndices = new List<short>(MAXIMUM_INTERSECTING_INDEXES);
        private static readonly List<short> EndpointIndices = new List<short>(MAXIMUM_INTERSECTING_INDEXES);
        private static readonly List<short> PolygonIndices = new List<short>(MAXIMUM_INTERSECTING_INDEXES);

        /* ---------- code */

        public static void recalculate_side_type(MapLevel level, short side_index)
        {
            side_data side = get_side_data(level, side_index);
            short opposite_index = find_adjacent_polygon(level, side.polygon_index, side.line_index);
            polygon_data polygon = get_polygon_data(level, side.polygon_index);
            if (opposite_index != NONE)
            {
                polygon_data opposite = get_polygon_data(level, opposite_index);
                int ceiling_height, floor_height, opposite_ceiling_height, opposite_floor_height;
                if (polygon.type == _polygon_is_platform)
                {
                    platform_data platform = get_platform_data(level, polygon.permutation);
                    ceiling_height = platform.maximum_ceiling_height;
                    floor_height = platform.minimum_floor_height;
                }
                else
                {
                    ceiling_height = polygon.ceiling_height;
                    floor_height = polygon.floor_height;
                }

                if (opposite.type == _polygon_is_platform)
                {
                    platform_data platform = get_platform_data(level, opposite.permutation);
                    opposite_ceiling_height = platform.minimum_ceiling_height;
                    opposite_floor_height = platform.maximum_floor_height;
                }
                else
                {
                    opposite_ceiling_height = opposite.ceiling_height;
                    opposite_floor_height = opposite.floor_height;
                }

                if (opposite_ceiling_height < ceiling_height && opposite_floor_height > floor_height)
                {
                    side.type = _split_side;
                }
                else if (opposite_floor_height > floor_height)
                {
                    side.type = _low_side;
                }
                else if (opposite_ceiling_height < ceiling_height)
                {
                    side.type = _high_side;
                }
                else
                    side.type = _full_side;

            }
            else
            {
                side.type = _full_side;
            }
        }

        public static short new_side(MapLevel level, short polygon_index, short line_index)
        {
            line_data line = get_line_data(level, line_index);
            polygon_data polygon = get_polygon_data(level, polygon_index);

            assert((line.clockwise_polygon_owner == polygon_index && line.clockwise_polygon_side_index == NONE) || (line.counterclockwise_polygon_owner == polygon_index && line.counterclockwise_polygon_side_index == NONE));

            side_data side = new side_data(); // obj_clear(side);
            side.primary_texture.texture = UNONE;
            side.secondary_texture.texture = UNONE;
            side.transparent_texture.texture = UNONE;

            short side_index = (short) level.SideList.Count;
            level.SideList.Add(side);
            // dynamic_world->side_count++;

            if (line.clockwise_polygon_owner == polygon_index)
                line.clockwise_polygon_side_index = side_index;
            else
                line.counterclockwise_polygon_side_index = side_index;
            recalculate_redundant_side_data(level, side_index, line_index);
            calculate_adjacent_sides(level, polygon_index, polygon.side_indexes);

            recalculate_side_type(level, side_index);
            return side_index;
        }

        /* calculates area, clockwise endpoint list, adjacent polygons */
        public static void recalculate_redundant_polygon_data(MapLevel level, short polygon_index)
        {
            polygon_data polygon = get_polygon_data(level, polygon_index);

            if (!POLYGON_IS_DETACHED(polygon))
            {
                calculate_clockwise_endpoints(level, polygon_index, polygon.endpoint_indexes);
                calculate_adjacent_polygons(level, polygon_index, polygon.adjacent_polygon_indexes);
                polygon.area = calculate_polygon_area(level, polygon_index);

                find_center_of_polygon(level, polygon_index, out polygon.center);
                calculate_adjacent_sides(level, polygon_index, polygon.side_indexes);
            }

            // TEMPORARY UNTIL THE EDITOR SETS THESE FIELDS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // polygon->media_lightsource_index= polygon->floor_lightsource_index;
            // polygon->ambient_sound_image_index= NONE;
            // polygon->random_sound_image_index= NONE;
        }

        /* calculates solidity, highest adjacent floor and lowest adjacent ceiling; not to be called
            at runtime. */
        public static void recalculate_redundant_endpoint_data(MapLevel level, short endpoint_index)
        {
            endpoint_data endpoint = get_endpoint_data(level, endpoint_index);
            short highest_adjacent_floor_height = INT16_MIN;
            short lowest_adjacent_ceiling_height = INT16_MAX;
            short supporting_polygon_index = NONE;
            line_data line;
            short line_index;
            bool solid = false;
            bool elevation = false;
            bool transparent = true;

            for (line_index = 0; line_index < level.LineList.Count; ++line_index)
            {
                line = level.LineList[line_index];

                /* does this line contain our endpoint? */
                if (line.endpoint_indexes[0] == endpoint_index || line.endpoint_indexes[1] == endpoint_index)
                {
                    short polygon_index;
                    polygon_data polygon;

                    /* if this line is solid, so is the endpoint */
                    if (LINE_IS_SOLID(line)) solid = true;
                    if (!LINE_IS_TRANSPARENT(line)) transparent = false;
                    if (LINE_IS_ELEVATION(line)) elevation = true;

                    /* look at adjacent polygons to determine highest floor and lowest ceiling */
                    polygon_index = line.clockwise_polygon_owner;
                    if (polygon_index != NONE)
                    {
                        polygon = get_polygon_data(level, polygon_index);
                        if (highest_adjacent_floor_height < polygon.floor_height)
                        {
                            highest_adjacent_floor_height = polygon.floor_height;
                            supporting_polygon_index = polygon_index;
                        }
                        if (lowest_adjacent_ceiling_height > polygon.ceiling_height) lowest_adjacent_ceiling_height = polygon.ceiling_height;
                    }
                    polygon_index = line.counterclockwise_polygon_owner;
                    if (polygon_index != NONE)
                    {
                        polygon = get_polygon_data(level, polygon_index);
                        if (highest_adjacent_floor_height < polygon.floor_height)
                        {
                            highest_adjacent_floor_height = polygon.floor_height;
                            supporting_polygon_index = polygon_index;
                        }
                        if (lowest_adjacent_ceiling_height > polygon.ceiling_height) lowest_adjacent_ceiling_height = polygon.ceiling_height;
                    }
                }
            }

            SET_ENDPOINT_SOLIDITY(endpoint, solid);
            SET_ENDPOINT_TRANSPARENCY(endpoint, transparent);
            SET_ENDPOINT_ELEVATION(endpoint, elevation);
            endpoint.highest_adjacent_floor_height = highest_adjacent_floor_height;
            endpoint.lowest_adjacent_ceiling_height = lowest_adjacent_ceiling_height;
            endpoint.supporting_polygon_index = supporting_polygon_index;
        }

        /* calculates line length, highest adjacent floor and lowest adjacent ceiling and calls
            recalculate_redundant_side_data() on the line's sides */
        public static void recalculate_redundant_line_data(MapLevel level, short line_index)
        {
            line_data line = get_line_data(level, line_index);
            side_data clockwise_side = null, counterclockwise_side = null;
            bool elevation = false;
            bool landscaped = false;
            bool variable_elevation = false;
            bool transparent_texture = false;

            /* recalculate line length */
            line.length = distance2d(get_endpoint_data(level, line.endpoint_indexes[0]).vertex,
                get_endpoint_data(level, line.endpoint_indexes[1]).vertex);

            /* find highest adjacent floor and lowest adjacent ceiling */
            {
                polygon_data polygon1, polygon2;

                polygon1 = (line.clockwise_polygon_owner == NONE) ? null : get_polygon_data(level, line.clockwise_polygon_owner);
                polygon2 = (line.counterclockwise_polygon_owner == NONE) ? null : get_polygon_data(level, line.counterclockwise_polygon_owner);

                if ((polygon1 != null && polygon1.type == _polygon_is_platform) || (polygon2 != null && polygon2.type == _polygon_is_platform)) variable_elevation = true;

                if (polygon1 != null && polygon2 != null)
                {
                    line.highest_adjacent_floor = (short) MAX(polygon1.floor_height, polygon2.floor_height);
                    line.lowest_adjacent_ceiling = (short) MIN(polygon1.ceiling_height, polygon2.ceiling_height);
                    if (polygon1.floor_height != polygon2.floor_height) elevation = true;
                }
                else
                {
                    elevation = true;

                    if (polygon1 != null)
                    {
                        line.highest_adjacent_floor = polygon1.floor_height;
                        line.lowest_adjacent_ceiling = polygon1.ceiling_height;
                    }
                    else
                    {
                        if (polygon2 != null)
                        {
                            line.highest_adjacent_floor = polygon2.floor_height;
                            line.lowest_adjacent_ceiling = polygon2.ceiling_height;
                        }
                        else
                        {
                            line.highest_adjacent_floor = line.lowest_adjacent_ceiling = 0;
                        }
                    }
                }
            }

            if (line.clockwise_polygon_side_index != NONE)
            {
                recalculate_redundant_side_data(level, line.clockwise_polygon_side_index, line_index);
                clockwise_side = get_side_data(level, line.clockwise_polygon_side_index);
            }
            if (line.counterclockwise_polygon_side_index != NONE)
            {
                recalculate_redundant_side_data(level, line.counterclockwise_polygon_side_index, line_index);
                counterclockwise_side = get_side_data(level, line.counterclockwise_polygon_side_index);
            }

            if ((clockwise_side != null && clockwise_side.primary_transfer_mode == _xfer_landscape) ||
                (counterclockwise_side != null && counterclockwise_side.primary_transfer_mode == _xfer_landscape))
            {
                landscaped = true;
            }

            if ((clockwise_side != null && clockwise_side.transparent_texture.texture != UNONE) ||
                (counterclockwise_side != null && counterclockwise_side.transparent_texture.texture != UNONE))
            {
                transparent_texture = true;
            }

            SET_LINE_ELEVATION(line, elevation);
            SET_LINE_VARIABLE_ELEVATION(line, variable_elevation && !LINE_IS_SOLID(line));
            SET_LINE_LANDSCAPE_STATUS(line, landscaped);
            SET_LINE_HAS_TRANSPARENT_SIDE(line, transparent_texture);
        }

        public static void recalculate_redundant_side_data(MapLevel level, short side_index, short line_index)
        {
            side_data side = get_side_data(level, side_index);
            line_data line = get_line_data(level, line_index);
            world_point2d e0, e1;

            // TEMPORARY UNTIL THE EDITOR SETS THESE FIELDS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // side->transparent_texture.texture= NONE; // no transparent texture
            // side->ambient_delta= 0;

            if (line.clockwise_polygon_side_index == side_index)
            {
                e0 = get_endpoint_data(level, line.endpoint_indexes[0]).vertex;
                e1 = get_endpoint_data(level, line.endpoint_indexes[1]).vertex;
                side.polygon_index = line.clockwise_polygon_owner;
            }
            else
            {
                assert(side_index == line.counterclockwise_polygon_side_index);

                e0 = get_endpoint_data(level, line.endpoint_indexes[1]).vertex;
                e1 = get_endpoint_data(level, line.endpoint_indexes[0]).vertex;
                side.polygon_index = line.counterclockwise_polygon_owner;
            }

            side.exclusion_zone.e0 = side.exclusion_zone.e2 = e0;
            side.exclusion_zone.e1 = side.exclusion_zone.e3 = e1;
            push_out_line(ref side.exclusion_zone.e0, ref side.exclusion_zone.e1, MINIMUM_SEPARATION_FROM_WALL, line.length);

            side.line_index = line_index;
            // side->direction= arctangent(e0->x - e1->x, e0->y - e1->y);

            // TEMPORARY UNTIL THE EDITOR SETS THESE FIELDS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // guess_side_lightsource_indexes(side_index);
        }

        public static void calculate_endpoint_polygon_owners(MapLevel level, short endpoint_index, out short first_index, out short index_count)
        {
            short polygon_index = 0;

            first_index = (short) level.MapIndexList.Count; // dynamic_world->map_index_count
            index_count = 0;

            for (; polygon_index < level.PolygonList.Count; ++polygon_index)
            {
                polygon_data polygon = level.PolygonList[polygon_index];
                for (ushort i = 0; i < polygon.vertex_count; ++i)
                {
                    if (endpoint_index == polygon.endpoint_indexes[i])
                    {
                        add_map_index(level, polygon_index, ref index_count);
                    }
                }
            }
        }

        public static void calculate_endpoint_line_owners(MapLevel level, short endpoint_index, out short first_index, out short index_count)
        {
            short line_index = 0;

            first_index = (short) level.MapIndexList.Count; // dynamic_world->map_index_count
            index_count = 0;

            for (; line_index < level.LineList.Count; ++line_index)
            {
                line_data line = level.LineList[line_index];
                if (line.endpoint_indexes[0] == endpoint_index || line.endpoint_indexes[1] == endpoint_index)
                {
                    add_map_index(level, line_index, ref index_count);
                }
            }
        }

        private const short CONTINUOUS_SPLIT_SIDE_HEIGHT = WORLD_ONE;

        public static void guess_side_lightsource_indexes(MapLevel level, short side_index)
        {
            side_data side = get_side_data(level, side_index);
            if (side.line_index < 0 ||
                side.line_index >= level.LineList.Count ||
                side.polygon_index < 0 ||
                side.polygon_index >= level.PolygonList.Count)
            {
                // apparently some M1 net maps have orphan sides
                return;
            }

            line_data line = get_line_data(level, side.line_index);
            polygon_data polygon = get_polygon_data(level, side.polygon_index);

            short ceiling_index = polygon.ceiling_lightsource_index;
            short floor_index = polygon.floor_lightsource_index;
            // change floor lighting if poly is a flooded platform
            if (polygon.type == _polygon_is_platform)
            {
                platform_data platform = get_platform_data(level, polygon.permutation);
                if (platform != null && PLATFORM_IS_FLOODED(platform))
                {
                    short adj_index = find_flooding_polygon(level, side.polygon_index);
                    if (adj_index != NONE)
                    {
                        polygon_data adj_polygon = get_polygon_data(level, adj_index);
                        floor_index = adj_polygon.floor_lightsource_index;
                    }
                }
            }

            switch (side.type)
            {
                case _full_side:
                    side.primary_lightsource_index = ceiling_index;
                    break;
                case _split_side:
                    side.secondary_lightsource_index = (line.lowest_adjacent_ceiling - line.highest_adjacent_floor > CONTINUOUS_SPLIT_SIDE_HEIGHT) ?
                        floor_index : ceiling_index;
                    /* fall through to high side */
                    goto case _high_side;
                case _high_side:
                    side.primary_lightsource_index = ceiling_index;
                    break;
                case _low_side:
                    side.primary_lightsource_index = floor_index;
                    break;

                default:
                    assert(false);
                    break;
            }

            side.transparent_lightsource_index = ceiling_index;
        }

        /* ---------- private code */

        /* given a polygon, return its endpoints in clockwise order; always returns polygon->vertex_count */
        private static short calculate_clockwise_endpoints(MapLevel level, short polygon_index, short[] buffer)
        {
            polygon_data polygon = get_polygon_data(level, polygon_index);

            for (ushort i = 0; i < polygon.vertex_count; ++i)
            {
                buffer[i] = clockwise_endpoint_in_line(level, polygon_index, polygon.line_indexes[i], 0);
            }

            return (short) polygon.vertex_count;
        }

        public static void calculate_adjacent_sides(MapLevel level, short polygon_index, short[] side_indexes)
        {
            polygon_data polygon = get_polygon_data(level, polygon_index);

            for (ushort i = 0; i < polygon.vertex_count; ++i)
            {
                line_data line = get_line_data(level, polygon.line_indexes[i]);
                short side_index;

                if (line.clockwise_polygon_owner == polygon_index)
                {
                    side_index = line.clockwise_polygon_side_index;
                }
                else
                {
                    // LP change: get around some Pfhorte bugs
                    side_index = line.counterclockwise_polygon_side_index;
                }

                side_indexes[i] = side_index;
            }
        }

        private static void calculate_adjacent_polygons(MapLevel level, short polygon_index, short[] polygon_indexes)
        {
            polygon_data polygon = get_polygon_data(level, polygon_index);

            for (ushort i = 0; i < polygon.vertex_count; ++i)
            {
                line_data line = get_line_data(level, polygon.line_indexes[i]);
                short adjacent_polygon_index = NONE;

                if (polygon_index == line.clockwise_polygon_owner)
                {
                    adjacent_polygon_index = line.counterclockwise_polygon_owner;
                }
                else
                {
                    // LP change: get around some Pfhorte bugs
                    adjacent_polygon_index = line.clockwise_polygon_owner;
                }

                polygon_indexes[i] = adjacent_polygon_index;
            }
        }

        /* returns area of the given polygon */
        private static int calculate_polygon_area(MapLevel level, short polygon_index)
        {
            int area = 0;
            world_point2d first_point, point, next_point;
            polygon_data polygon = get_polygon_data(level, polygon_index);

            first_point = get_endpoint_data(level, polygon.endpoint_indexes[0]).vertex;
            for (ushort vertex = 1; vertex < polygon.vertex_count - 1; ++vertex)
            {
                point = get_endpoint_data(level, polygon.endpoint_indexes[vertex]).vertex;
                next_point = get_endpoint_data(level, polygon.endpoint_indexes[vertex + 1]).vertex;

                area += unchecked((((first_point.x) * (point.y)) - ((point.x) * (first_point.y))) +
                    (((point.x) * (next_point.y)) - ((next_point.x) * (point.y))) +
                    (((next_point.x) * (first_point.y)) - ((first_point.x) * (next_point.y))));
            }

            /* real area is absolute value of calculated area divided by two */
            area = (Math.Abs(area) >> 1);

            return area;
        }

        /* ---------- precalculate map indexes */

        public static void precalculate_map_indexes(MapLevel level)
        {
            short polygon_index = 0;

            for (; polygon_index < level.PolygonList.Count; ++polygon_index)
            {
                polygon_data polygon = level.PolygonList[polygon_index];

                if (!POLYGON_IS_DETACHED(polygon)) /* we'll handle detached polygons during the second pass */
                {
                    polygon.first_exclusion_zone_index = (short) level.MapIndexList.Count; // dynamic_world->map_index_count
                    polygon.line_exclusion_zone_count = polygon.point_exclusion_zone_count = 0;
                    find_intersecting_endpoints_and_lines(level, polygon_index, MINIMUM_SEPARATION_FROM_WALL);

                    int line_count = LineIndices.Count;
                    int endpoint_count = EndpointIndices.Count;

                    for (int i = 0; i < line_count; ++i)
                    {
                        add_map_index(level, LineIndices[i], ref polygon.line_exclusion_zone_count);
                    }

                    for (int i = 0; i < endpoint_count; ++i)
                    {
                        add_map_index(level, EndpointIndices[i], ref polygon.point_exclusion_zone_count);
                    }

                    polygon.first_neighbor_index = (short) level.MapIndexList.Count; // dynamic_world->map_index_count
                    polygon.neighbor_count = 0;
                    find_intersecting_endpoints_and_lines(level, polygon_index, MINIMUM_SEPARATION_FROM_PROJECTILE);

                    int polygon_count = PolygonIndices.Count;

                    for (int i = 0; i < polygon_count; ++i)
                    {
                        add_map_index(level, PolygonIndices[i], ref polygon.neighbor_count);
                    }
                }
            }

            precalculate_polygon_sound_sources(level);
        }

        private static void find_intersecting_endpoints_and_lines(MapLevel level, short polygon_index, short minimum_separation)
        {
            intersecting_flood_data data = new intersecting_flood_data();

            data.original_polygon_index = polygon_index;
            LineIndices.Clear();
            EndpointIndices.Clear();
            PolygonIndices.Clear();

            data.minimum_separation_squared = minimum_separation * minimum_separation;
            find_center_of_polygon(level, polygon_index, out data.center);

            if (film_profile.adjacent_polygons_always_intersect)
            {
                polygon_data polygon = get_polygon_data(level, polygon_index);
                for (int i = 0; i < polygon.vertex_count; ++i)
                {
                    short adjacent_polygon_index = find_adjacent_polygon(level, polygon_index, polygon.line_indexes[i]);
                    if (adjacent_polygon_index != NONE)
                    {
                        PolygonIndices.Add(adjacent_polygon_index);
                    }
                }
            }

            polygon_index = flood_map.flood_map_(level, polygon_index, INT32_MAX, intersecting_flood_proc, flood_map._breadth_first, data);
            while (polygon_index != NONE)
            {
                polygon_index = flood_map.flood_map_(level, NONE, INT32_MAX, intersecting_flood_proc, flood_map._breadth_first, data);
            }
        }

        private static int intersecting_flood_proc(MapLevel level, short source_polygon_index, short line_index, short destination_polygon_index, object vdata)
        {
            intersecting_flood_data data = (intersecting_flood_data) vdata;
            polygon_data polygon = get_polygon_data(level, source_polygon_index);
            polygon_data original_polygon = get_polygon_data(level, data.original_polygon_index);
            bool keep_searching = false; /* don't flood any deeper unless we find something close enough */
            int i, j;
            // (void) (line_index); (void) (destination_polygon_index);

            /* we only care about this polygon if it intersects us in z */
            if ((polygon.floor_height <= original_polygon.ceiling_height) && (polygon.ceiling_height >= original_polygon.floor_height))
            {
                /* update our running line and endpoint lists */
                for (i = 0; i < polygon.vertex_count; ++i)
                {
                    /* add this line if it isn't already in the intersecting line list */
                    for (j = 0; j < LineIndices.Count; ++j)
                    {
                        if (LineIndices[j] == polygon.line_indexes[i] ||
                            -LineIndices[j] - 1 == polygon.line_indexes[i])
                        {
                            keep_searching = true;
                            break; /* found duplicate, stop */
                        }
                    }
                    if (j == LineIndices.Count)
                    {
                        short line_index2 = polygon.line_indexes[i];
                        line_data line = get_line_data(level, line_index2);

                        if (LINE_IS_SOLID(line) ||
                            line_has_variable_height(level, line_index2) ||
                            line.lowest_adjacent_ceiling < original_polygon.ceiling_height ||
                            line.highest_adjacent_floor > original_polygon.floor_height)
                        {
                            world_point2d a = get_endpoint_data(level, line.endpoint_indexes[0]).vertex;
                            world_point2d b = get_endpoint_data(level, line.endpoint_indexes[1]).vertex;

                            /* check and see if this line is close enough to any point in our original polygon
                                to care about; if it is, add it to our list */
                            for (j = 0; j < original_polygon.vertex_count; ++j)
                            {
                                world_point2d p = get_endpoint_data(level, original_polygon.endpoint_indexes[j]).vertex;

                                if (point_to_line_segment_distance_squared(p, a, b) < data.minimum_separation_squared)
                                {
                                    bool clockwise = unchecked((((b.x - a.x) * (data.center.y - b.y)) - ((b.y - a.y) * (data.center.x - b.x))) > 0);

                                    LineIndices.Add(clockwise ? polygon.line_indexes[i] : (short) (-polygon.line_indexes[i] - 1));
                                    keep_searching = true;
                                    break;
                                }
                            }
                        }
                    }

                    /* add this endpoint if it isn't already in the intersecting endpoint list */
                    for (j = 0; j < EndpointIndices.Count; ++j)
                    {
                        if (EndpointIndices[j] == polygon.endpoint_indexes[i])
                        {
                            keep_searching = true;
                            break; /* found duplicate, ignore (but keep looking for others) */
                        }
                    }
                    if (j == EndpointIndices.Count)
                    {
                        world_point2d p = get_endpoint_data(level, polygon.endpoint_indexes[i]).vertex;

                        /* check and see if this endpoint is close enough to any line in our original polygon
                            to care about; if it is, add it to our list */
                        for (j = 0; j < original_polygon.vertex_count; ++j)
                        {
                            line_data line = get_line_data(level, original_polygon.line_indexes[j]);
                            world_point2d a = get_endpoint_data(level, line.endpoint_indexes[0]).vertex;
                            world_point2d b = get_endpoint_data(level, line.endpoint_indexes[1]).vertex;

                            if (point_to_line_segment_distance_squared(p, a, b) < data.minimum_separation_squared)
                            {
                                EndpointIndices.Add(polygon.endpoint_indexes[i]);
                                break;
                            }
                        }
                    }
                }
            }

            /* if any part of this polygon is close enough to our original polygon, remember it's index */
            if (keep_searching)
            {
                for (j = 0; j < PolygonIndices.Count; ++j)
                {
                    if (PolygonIndices[j] == source_polygon_index)
                    {
                        break; /* found duplicate, ignore */
                    }
                }
                if (j == PolygonIndices.Count)
                {
                    short detached_twin_index = NONE; //find_undetached_polygons_twin(source_polygon_index);

                    PolygonIndices.Add(source_polygon_index);

                    /* if this polygon has a detached twin, add it too */
                    if (detached_twin_index != NONE)
                    {
                        PolygonIndices.Add(detached_twin_index);
                    }
                }
            }

            /* return area of source polygon as cost */
            return keep_searching ? 1 : -1;
        }

        private static void add_map_index(MapLevel level, short index, ref short count)
        {
            assert(level.MapIndexList.Count < UINT16_MAX);
            level.MapIndexList.Add(index);
            // dynamic_world->map_index_count++;
            count += 1;
        }

        private const int ZERO_VOLUME_DISTANCE = (10 * WORLD_ONE);

        private static void precalculate_polygon_sound_sources(MapLevel level)
        {
            short polygon_index;

            for (polygon_index = 0; polygon_index < level.PolygonList.Count; ++polygon_index)
            {
                polygon_data polygon = level.PolygonList[polygon_index];
                short object_index;
                short sound_sources = 0;

                polygon.sound_source_indexes = (short) level.MapIndexList.Count; // dynamic_world->map_index_count

                for (object_index = 0; object_index < level.SavedObjectList.Count; ++object_index)
                {
                    map_object obj = level.SavedObjectList[object_index];
                    if (obj.type == _saved_sound_source)
                    {
                        short i;
                        bool close = false;

                        for (i = 0; i < polygon.vertex_count; ++i)
                        {
                            endpoint_data endpoint = get_endpoint_data(level, polygon.endpoint_indexes[i]);
                            line_data line = get_line_data(level, polygon.line_indexes[i]);

                            if (guess_distance2d(obj.location.xy(), endpoint.vertex) < ZERO_VOLUME_DISTANCE ||
                                point_to_line_segment_distance_squared(obj.location.xy(),
                                    get_endpoint_data(level, line.endpoint_indexes[0]).vertex,
                                    get_endpoint_data(level, line.endpoint_indexes[1]).vertex) < ZERO_VOLUME_DISTANCE)
                            {
                                close = true;
                                break;
                            }
                        }

                        if (close) add_map_index(level, object_index, ref sound_sources);
                    }
                }

                add_map_index(level, NONE, ref sound_sources);
            }
        }

        /* ---------- packing */

        public static void unpack_endpoint_data(StreamPointer S, IList<endpoint_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                endpoint_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.flags);
                StreamToValue(S, out ObjPtr.highest_adjacent_floor_height);
                StreamToValue(S, out ObjPtr.lowest_adjacent_ceiling_height);

                StreamToValue(S, out ObjPtr.vertex.x);
                StreamToValue(S, out ObjPtr.vertex.y);
                StreamToValue(S, out ObjPtr.transformed.x);
                StreamToValue(S, out ObjPtr.transformed.y);

                StreamToValue(S, out ObjPtr.supporting_polygon_index);
            }

            assert((S.Position - Stream) == Count * SIZEOF_endpoint_data);
        }

        public static void pack_endpoint_data(StreamPointer S, IList<endpoint_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                endpoint_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.flags);
                ValueToStream(S, ObjPtr.highest_adjacent_floor_height);
                ValueToStream(S, ObjPtr.lowest_adjacent_ceiling_height);

                ValueToStream(S, ObjPtr.vertex.x);
                ValueToStream(S, ObjPtr.vertex.y);
                ValueToStream(S, ObjPtr.transformed.x);
                ValueToStream(S, ObjPtr.transformed.y);

                ValueToStream(S, ObjPtr.supporting_polygon_index);
            }

            assert((S.Position - Stream) == Count * SIZEOF_endpoint_data);
        }

        public static void unpack_line_data(StreamPointer S, IList<line_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                line_data ObjPtr = Objects[k];
                StreamToList(S, ObjPtr.endpoint_indexes, 2);
                StreamToValue(S, out ObjPtr.flags);

                StreamToValue(S, out ObjPtr.length);
                StreamToValue(S, out ObjPtr.highest_adjacent_floor);
                StreamToValue(S, out ObjPtr.lowest_adjacent_ceiling);

                StreamToValue(S, out ObjPtr.clockwise_polygon_side_index);
                StreamToValue(S, out ObjPtr.counterclockwise_polygon_side_index);

                StreamToValue(S, out ObjPtr.clockwise_polygon_owner);
                StreamToValue(S, out ObjPtr.counterclockwise_polygon_owner);

                S.Skip(6 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_line_data);
        }

        public static void pack_line_data(StreamPointer S, IList<line_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                line_data ObjPtr = Objects[k];
                ListToStream(S, ObjPtr.endpoint_indexes, 2);
                ValueToStream(S, ObjPtr.flags);

                ValueToStream(S, ObjPtr.length);
                ValueToStream(S, ObjPtr.highest_adjacent_floor);
                ValueToStream(S, ObjPtr.lowest_adjacent_ceiling);

                ValueToStream(S, ObjPtr.clockwise_polygon_side_index);
                ValueToStream(S, ObjPtr.counterclockwise_polygon_side_index);

                ValueToStream(S, ObjPtr.clockwise_polygon_owner);
                ValueToStream(S, ObjPtr.counterclockwise_polygon_owner);

                S.Skip(6 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_line_data);
        }

        private static void StreamToSideTxtr(StreamPointer S, side_texture_definition Object)
        {
            StreamToValue(S, out Object.x0);
            StreamToValue(S, out Object.y0);
            StreamToValue(S, out Object.texture);
        }

        private static void SideTxtrToStream(StreamPointer S, side_texture_definition Object)
        {
            ValueToStream(S, Object.x0);
            ValueToStream(S, Object.y0);
            ValueToStream(S, Object.texture);
        }

        private static void StreamToSideExclZone(StreamPointer S, side_exclusion_zone Object)
        {
            StreamToValue(S, out Object.e0.x);
            StreamToValue(S, out Object.e0.y);
            StreamToValue(S, out Object.e1.x);
            StreamToValue(S, out Object.e1.y);
            StreamToValue(S, out Object.e2.x);
            StreamToValue(S, out Object.e2.y);
            StreamToValue(S, out Object.e3.x);
            StreamToValue(S, out Object.e3.y);
        }

        private static void SideExclZoneToStream(StreamPointer S, side_exclusion_zone Object)
        {
            ValueToStream(S, Object.e0.x);
            ValueToStream(S, Object.e0.y);
            ValueToStream(S, Object.e1.x);
            ValueToStream(S, Object.e1.y);
            ValueToStream(S, Object.e2.x);
            ValueToStream(S, Object.e2.y);
            ValueToStream(S, Object.e3.x);
            ValueToStream(S, Object.e3.y);
        }

        public static void unpack_side_data(StreamPointer S, IList<side_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                side_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.type);
                StreamToValue(S, out ObjPtr.flags);

                StreamToSideTxtr(S, ObjPtr.primary_texture);
                StreamToSideTxtr(S, ObjPtr.secondary_texture);
                StreamToSideTxtr(S, ObjPtr.transparent_texture);

                StreamToSideExclZone(S, ObjPtr.exclusion_zone);

                StreamToValue(S, out ObjPtr.control_panel_type);
                StreamToValue(S, out ObjPtr.control_panel_permutation);

                StreamToValue(S, out ObjPtr.primary_transfer_mode);
                StreamToValue(S, out ObjPtr.secondary_transfer_mode);
                StreamToValue(S, out ObjPtr.transparent_transfer_mode);

                StreamToValue(S, out ObjPtr.polygon_index);
                StreamToValue(S, out ObjPtr.line_index);

                StreamToValue(S, out ObjPtr.primary_lightsource_index);
                StreamToValue(S, out ObjPtr.secondary_lightsource_index);
                StreamToValue(S, out ObjPtr.transparent_lightsource_index);

                StreamToValue(S, out ObjPtr.ambient_delta);

                S.Skip(1 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_side_data);
        }

        public static void pack_side_data(StreamPointer S, IList<side_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                side_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.type);
                ValueToStream(S, ObjPtr.flags);

                SideTxtrToStream(S, ObjPtr.primary_texture);
                SideTxtrToStream(S, ObjPtr.secondary_texture);
                SideTxtrToStream(S, ObjPtr.transparent_texture);

                SideExclZoneToStream(S, ObjPtr.exclusion_zone);

                ValueToStream(S, ObjPtr.control_panel_type);
                ValueToStream(S, ObjPtr.control_panel_permutation);

                ValueToStream(S, ObjPtr.primary_transfer_mode);
                ValueToStream(S, ObjPtr.secondary_transfer_mode);
                ValueToStream(S, ObjPtr.transparent_transfer_mode);

                ValueToStream(S, ObjPtr.polygon_index);
                ValueToStream(S, ObjPtr.line_index);

                ValueToStream(S, ObjPtr.primary_lightsource_index);
                ValueToStream(S, ObjPtr.secondary_lightsource_index);
                ValueToStream(S, ObjPtr.transparent_lightsource_index);

                ValueToStream(S, ObjPtr.ambient_delta);

                S.Skip(1 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_side_data);
        }

        public static void unpack_polygon_data(StreamPointer S, IList<polygon_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                polygon_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.type);
                StreamToValue(S, out ObjPtr.flags);
                StreamToValue(S, out ObjPtr.permutation);

                StreamToValue(S, out ObjPtr.vertex_count);
                StreamToList(S, ObjPtr.endpoint_indexes, MAXIMUM_VERTICES_PER_POLYGON);
                StreamToList(S, ObjPtr.line_indexes, MAXIMUM_VERTICES_PER_POLYGON);

                StreamToValue(S, out ObjPtr.floor_texture);
                StreamToValue(S, out ObjPtr.ceiling_texture);
                StreamToValue(S, out ObjPtr.floor_height);
                StreamToValue(S, out ObjPtr.ceiling_height);
                StreamToValue(S, out ObjPtr.floor_lightsource_index);
                StreamToValue(S, out ObjPtr.ceiling_lightsource_index);

                StreamToValue(S, out ObjPtr.area);

                StreamToValue(S, out ObjPtr.first_object);

                StreamToValue(S, out ObjPtr.first_exclusion_zone_index);
                StreamToValue(S, out ObjPtr.line_exclusion_zone_count);
                StreamToValue(S, out ObjPtr.point_exclusion_zone_count);

                StreamToValue(S, out ObjPtr.floor_transfer_mode);
                StreamToValue(S, out ObjPtr.ceiling_transfer_mode);

                StreamToList(S, ObjPtr.adjacent_polygon_indexes, MAXIMUM_VERTICES_PER_POLYGON);

                StreamToValue(S, out ObjPtr.first_neighbor_index);
                StreamToValue(S, out ObjPtr.neighbor_count);

                StreamToValue(S, out ObjPtr.center.x);
                StreamToValue(S, out ObjPtr.center.y);

                StreamToList(S, ObjPtr.side_indexes, MAXIMUM_VERTICES_PER_POLYGON);

                StreamToValue(S, out ObjPtr.floor_origin.x);
                StreamToValue(S, out ObjPtr.floor_origin.y);
                StreamToValue(S, out ObjPtr.ceiling_origin.x);
                StreamToValue(S, out ObjPtr.ceiling_origin.y);

                StreamToValue(S, out ObjPtr.media_index);
                StreamToValue(S, out ObjPtr.media_lightsource_index);

                StreamToValue(S, out ObjPtr.sound_source_indexes);

                StreamToValue(S, out ObjPtr.ambient_sound_image_index);
                StreamToValue(S, out ObjPtr.random_sound_image_index);

                S.Skip(1 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_polygon_data);
        }

        public static void pack_polygon_data(StreamPointer S, IList<polygon_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                polygon_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.type);
                ValueToStream(S, ObjPtr.flags);
                ValueToStream(S, ObjPtr.permutation);

                ValueToStream(S, ObjPtr.vertex_count);
                ListToStream(S, ObjPtr.endpoint_indexes, MAXIMUM_VERTICES_PER_POLYGON);
                ListToStream(S, ObjPtr.line_indexes, MAXIMUM_VERTICES_PER_POLYGON);

                ValueToStream(S, ObjPtr.floor_texture);
                ValueToStream(S, ObjPtr.ceiling_texture);
                ValueToStream(S, ObjPtr.floor_height);
                ValueToStream(S, ObjPtr.ceiling_height);
                ValueToStream(S, ObjPtr.floor_lightsource_index);
                ValueToStream(S, ObjPtr.ceiling_lightsource_index);

                ValueToStream(S, ObjPtr.area);

                ValueToStream(S, ObjPtr.first_object);

                ValueToStream(S, ObjPtr.first_exclusion_zone_index);
                ValueToStream(S, ObjPtr.line_exclusion_zone_count);
                ValueToStream(S, ObjPtr.point_exclusion_zone_count);

                ValueToStream(S, ObjPtr.floor_transfer_mode);
                ValueToStream(S, ObjPtr.ceiling_transfer_mode);

                ListToStream(S, ObjPtr.adjacent_polygon_indexes, MAXIMUM_VERTICES_PER_POLYGON);

                ValueToStream(S, ObjPtr.first_neighbor_index);
                ValueToStream(S, ObjPtr.neighbor_count);

                ValueToStream(S, ObjPtr.center.x);
                ValueToStream(S, ObjPtr.center.y);

                ListToStream(S, ObjPtr.side_indexes, MAXIMUM_VERTICES_PER_POLYGON);

                ValueToStream(S, ObjPtr.floor_origin.x);
                ValueToStream(S, ObjPtr.floor_origin.y);
                ValueToStream(S, ObjPtr.ceiling_origin.x);
                ValueToStream(S, ObjPtr.ceiling_origin.y);

                ValueToStream(S, ObjPtr.media_index);
                ValueToStream(S, ObjPtr.media_lightsource_index);

                ValueToStream(S, ObjPtr.sound_source_indexes);

                ValueToStream(S, ObjPtr.ambient_sound_image_index);
                ValueToStream(S, ObjPtr.random_sound_image_index);

                S.Skip(1 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_polygon_data);
        }

        public static void unpack_map_annotation(StreamPointer S, IList<map_annotation> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                map_annotation ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.type);

                StreamToValue(S, out ObjPtr.location.x);
                StreamToValue(S, out ObjPtr.location.y);
                StreamToValue(S, out ObjPtr.polygon_index);

                StreamToBytes(S, ObjPtr.text, MAXIMUM_ANNOTATION_TEXT_LENGTH);
            }

            assert((S.Position - Stream) == Count * SIZEOF_map_annotation);
        }

        public static void pack_map_annotation(StreamPointer S, IList<map_annotation> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                map_annotation ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.type);

                ValueToStream(S, ObjPtr.location.x);
                ValueToStream(S, ObjPtr.location.y);
                ValueToStream(S, ObjPtr.polygon_index);

                BytesToStream(S, ObjPtr.text, MAXIMUM_ANNOTATION_TEXT_LENGTH);
            }

            assert((S.Position - Stream) == Count * SIZEOF_map_annotation);
        }

        public static void unpack_map_object(StreamPointer S, IList<map_object> Objects, int Count, int version)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                map_object ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.type);
                StreamToValue(S, out ObjPtr.index);
                StreamToValue(S, out ObjPtr.facing);
                StreamToValue(S, out ObjPtr.polygon_index);
                StreamToValue(S, out ObjPtr.location.x);
                StreamToValue(S, out ObjPtr.location.y);
                if (version == MARATHON_ONE_DATA_VERSION &&
                    film_profile.m1_object_unused)
                {
                    ObjPtr.location.z = 0;
                    ObjPtr.flags = 0;
                    S.Skip(2 * 2); // short unused[2]
                }
                else
                {
                    StreamToValue(S, out ObjPtr.location.z);
                    StreamToValue(S, out ObjPtr.flags);
                }
            }

            assert((S.Position - Stream) == Count * SIZEOF_map_object);
        }

        public static void pack_map_object(StreamPointer S, IList<map_object> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                map_object ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.type);
                ValueToStream(S, ObjPtr.index);
                ValueToStream(S, ObjPtr.facing);
                ValueToStream(S, ObjPtr.polygon_index);
                ValueToStream(S, ObjPtr.location.x);
                ValueToStream(S, ObjPtr.location.y);
                ValueToStream(S, ObjPtr.location.z);

                ValueToStream(S, ObjPtr.flags);
            }

            assert((S.Position - Stream) == Count * SIZEOF_map_object);
        }

        public static void unpack_object_frequency_definition(StreamPointer S, IList<object_frequency_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                object_frequency_definition ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.flags);

                StreamToValue(S, out ObjPtr.initial_count);
                StreamToValue(S, out ObjPtr.minimum_count);
                StreamToValue(S, out ObjPtr.maximum_count);

                StreamToValue(S, out ObjPtr.random_count);
                StreamToValue(S, out ObjPtr.random_chance);
            }

            assert((S.Position - Stream) == Count * SIZEOF_object_frequency_definition);
        }

        public static void pack_object_frequency_definition(StreamPointer S, IList<object_frequency_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                object_frequency_definition ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.flags);

                ValueToStream(S, ObjPtr.initial_count);
                ValueToStream(S, ObjPtr.minimum_count);
                ValueToStream(S, ObjPtr.maximum_count);

                ValueToStream(S, ObjPtr.random_count);
                ValueToStream(S, ObjPtr.random_chance);
            }

            assert((S.Position - Stream) == Count * SIZEOF_object_frequency_definition);
        }

        public static void unpack_static_data(StreamPointer S, IList<static_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                static_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.environment_code);

                StreamToValue(S, out ObjPtr.physics_model);
                StreamToValue(S, out ObjPtr.song_index);
                StreamToValue(S, out ObjPtr.mission_flags);
                StreamToValue(S, out ObjPtr.environment_flags);

                S.Skip(4 * 2);

                StreamToBytes(S, ObjPtr.level_name, LEVEL_NAME_LENGTH);
                StreamToValue(S, out ObjPtr.entry_point_flags);
            }

            assert((S.Position - Stream) == Count * SIZEOF_static_data);
        }

        public static void pack_static_data(StreamPointer S, IList<static_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                static_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.environment_code);

                ValueToStream(S, ObjPtr.physics_model);
                ValueToStream(S, ObjPtr.song_index);
                ValueToStream(S, ObjPtr.mission_flags);
                ValueToStream(S, ObjPtr.environment_flags);

                S.Skip(4 * 2);

                BytesToStream(S, ObjPtr.level_name, LEVEL_NAME_LENGTH);
                ValueToStream(S, ObjPtr.entry_point_flags);
            }

            assert((S.Position - Stream) == Count * SIZEOF_static_data);
        }

        public static void unpack_ambient_sound_image_data(StreamPointer S, IList<ambient_sound_image_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                ambient_sound_image_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.flags);

                StreamToValue(S, out ObjPtr.sound_index);
                StreamToValue(S, out ObjPtr.volume);

                S.Skip(5 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_ambient_sound_image_data);
        }

        public static void pack_ambient_sound_image_data(StreamPointer S, IList<ambient_sound_image_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                ambient_sound_image_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.flags);

                ValueToStream(S, ObjPtr.sound_index);
                ValueToStream(S, ObjPtr.volume);

                S.Skip(5 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_ambient_sound_image_data);
        }

        public static void unpack_random_sound_image_data(StreamPointer S, IList<random_sound_image_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                random_sound_image_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.flags);

                StreamToValue(S, out ObjPtr.sound_index);

                StreamToValue(S, out ObjPtr.volume);
                StreamToValue(S, out ObjPtr.delta_volume);
                StreamToValue(S, out ObjPtr.period);
                StreamToValue(S, out ObjPtr.delta_period);
                StreamToValue(S, out ObjPtr.direction);
                StreamToValue(S, out ObjPtr.delta_direction);
                StreamToValue(S, out ObjPtr.pitch);
                StreamToValue(S, out ObjPtr.delta_pitch);

                StreamToValue(S, out ObjPtr.phase);

                S.Skip(3 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_random_sound_image_data);
        }

        public static void pack_random_sound_image_data(StreamPointer S, IList<random_sound_image_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                random_sound_image_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.flags);

                ValueToStream(S, ObjPtr.sound_index);

                ValueToStream(S, ObjPtr.volume);
                ValueToStream(S, ObjPtr.delta_volume);
                ValueToStream(S, ObjPtr.period);
                ValueToStream(S, ObjPtr.delta_period);
                ValueToStream(S, ObjPtr.direction);
                ValueToStream(S, ObjPtr.delta_direction);
                ValueToStream(S, ObjPtr.pitch);
                ValueToStream(S, ObjPtr.delta_pitch);

                ValueToStream(S, ObjPtr.phase);

                S.Skip(3 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_random_sound_image_data);
        }

        // unpack_dynamic_data/pack_dynamic_data and unpack_object_data/pack_object_data: not ported

        public static void unpack_damage_definition(StreamPointer S, IList<damage_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                damage_definition ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.type);
                StreamToValue(S, out ObjPtr.flags);

                StreamToValue(S, out ObjPtr.@base);
                StreamToValue(S, out ObjPtr.random);
                StreamToValue(S, out ObjPtr.scale);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_damage_definition));
        }

        public static void pack_damage_definition(StreamPointer S, IList<damage_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                damage_definition ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.type);
                ValueToStream(S, ObjPtr.flags);

                ValueToStream(S, ObjPtr.@base);
                ValueToStream(S, ObjPtr.random);
                ValueToStream(S, ObjPtr.scale);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_damage_definition));
        }
    }
}
