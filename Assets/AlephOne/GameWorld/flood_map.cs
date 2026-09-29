// Port of Aleph One: Source_Files/GameWorld/flood_map.h, flood_map.cpp
//
// Not ported: choose_random_flood_node() and the pathfinding.cpp prototypes.
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csalerts;
using static AlephOne.cstypes;
using static AlephOne.map;

namespace AlephOne
{
    /* ---------- typedefs */

    // typedef int32 (*cost_proc_ptr)(short source_polygon_index, short line_index, short destination_polygon_index, void *caller_data);
    // with _flagged_breadth_first, caller_data is an int[1]
    public delegate int cost_proc_ptr(MapLevel level, short source_polygon_index, short line_index, short destination_polygon_index, object caller_data);

    [NoAutoStaticsCleanup]
    public static class flood_map
    {
        /* ---------- constants */

        /* flood modes */
        public const short _depth_first = 0; /* unsupported */
        public const short _breadth_first = 1; /* significantly faster than _best_first for large domains */
        public const short _flagged_breadth_first = 2; /* user data is interpreted as an int32 * to 4 bytes of flags */
        public const short _best_first = 3;

        private const int MAXIMUM_FLOOD_NODES = 255;
        private const short UNVISITED = NONE;

        /* ---------- structures */

        private static bool NODE_IS_EXPANDED(node_data n) { return ((n).flags & (ushort) 0x8000) != 0; }
        private static bool NODE_IS_UNEXPANDED(node_data n) { return (!NODE_IS_EXPANDED(n)); }
        private static void MARK_NODE_AS_EXPANDED(node_data n) { (n).flags |= (ushort) 0x8000; }

        private class node_data /* 16 bytes */
        {
            public ushort flags;
            public short parent_node_index; /* node index of the node we came from to get here; only used for backtracking */
            public short polygon_index; /* index of this polygon */
            public int cost; /* the cost to evaluate this entry */
            public short depth;
            public int user_flags;
        }

        /* ---------- globals */

        private static short node_count = 0, last_node_index_expanded = NONE;
        private static node_data[] nodes = null;
        private static short[] visited_polygons = null;

        /* ---------- code */

        public static void allocate_flood_map_memory(MapLevel level)
        {
            // Made reentrant because this must be called every time a map is loaded
            nodes = new node_data[MAXIMUM_FLOOD_NODES];
            for (int i = 0; i < MAXIMUM_FLOOD_NODES; i++) nodes[i] = new node_data();
            visited_polygons = new short[level.PolygonList.Count];
        }

        /* returns next polygon index or NONE if there are no more polygons left cheaper than maximum_cost */
        public static short flood_map_(MapLevel level, short first_polygon_index, int maximum_cost, cost_proc_ptr cost_proc, short flood_mode, object caller_data)
        {
            short lowest_cost_node_index = NONE, node_index;
            node_data node;
            short polygon_index;
            int lowest_cost = 0;

            /* initialize ourselves if first_polygon_index!=NONE */
            if (first_polygon_index != NONE)
            {
                /* clear the visited polygon array */
                if (visited_polygons == null || visited_polygons.Length != level.PolygonList.Count) allocate_flood_map_memory(level);
                for (int i = 0; i < visited_polygons.Length; i++) visited_polygons[i] = NONE;
                node_count = 0;
                last_node_index_expanded = NONE;
                add_node(level, NONE, first_polygon_index, 0, 0, (flood_mode == _flagged_breadth_first) ? ((int[]) caller_data)[0] : 0);
            }

            switch (flood_mode)
            {
                case _best_first:
                    /* find the unexpanded node with the lowest cost */
                    lowest_cost = maximum_cost; lowest_cost_node_index = NONE;
                    for (node_index = 0; node_index < node_count; ++node_index)
                    {
                        node = nodes[node_index];
                        if (NODE_IS_UNEXPANDED(node) && node.cost < lowest_cost)
                        {
                            lowest_cost_node_index = node_index;
                            lowest_cost = node.cost;
                        }
                    }
                    break;

                case _breadth_first:
                case _flagged_breadth_first:
                    /* find the next unexpanded node in the list under maximum_cost */
                    node_index = (short) ((last_node_index_expanded == NONE) ? 0 : (last_node_index_expanded + 1));
                    for (; node_index < node_count; ++node_index)
                    {
                        if (nodes[node_index].cost < maximum_cost) break;
                    }
                    if (node_index == node_count)
                    {
                        lowest_cost_node_index = NONE;
                        lowest_cost = maximum_cost;
                    }
                    else
                    {
                        lowest_cost_node_index = node_index;
                        lowest_cost = nodes[node_index].cost;
                    }
                    break;

                case _depth_first:
                    /* implementation left to the caller (c.f., zen() in fareast.c) */
                    assert(false);
                    break;

                default:
                    assert(false);
                    break;
            }

            /* if we found a node, mark it as expanded and add it's adjacent non-solid polygons to the search tree */
            if (lowest_cost_node_index != NONE)
            {
                polygon_data polygon;
                short i;

                /* for flood_depth() and reverse_flood_map(), remember which node we successfully expanded last */
                last_node_index_expanded = lowest_cost_node_index;

                /* get pointer to lowest cost node */
                assert(lowest_cost_node_index >= 0 && lowest_cost_node_index < node_count);
                node = nodes[lowest_cost_node_index];
                polygon = get_polygon_data(level, node.polygon_index);
                assert(!POLYGON_IS_DETACHED(polygon));

                /* mark node as expanded */
                MARK_NODE_AS_EXPANDED(node);

                // int32 new_user_flags, whose address is passed
                int[] new_user_flags = new int[1];

                for (i = 0; i < polygon.vertex_count; ++i)
                {
                    short destination_polygon_index = polygon.adjacent_polygon_indexes[i];

                    if (destination_polygon_index != NONE &&
                        (maximum_cost != INT32_MAX || visited_polygons[destination_polygon_index] == UNVISITED))
                    {
                        new_user_flags[0] = node.user_flags;
                        int cost = cost_proc != null ? cost_proc(level, node.polygon_index, polygon.line_indexes[i], destination_polygon_index, (flood_mode == _flagged_breadth_first) ? new_user_flags : caller_data) : polygon.area;

                        /* polygons with zero or negative costs are not added to the node list */
                        if (cost > 0) add_node(level, lowest_cost_node_index, destination_polygon_index, (short) (node.depth + 1), lowest_cost + cost, new_user_flags[0]);
                    }
                }

                polygon_index = node.polygon_index;
                if (flood_mode == _flagged_breadth_first) ((int[]) caller_data)[0] = node.user_flags;
            }
            else
            {
                polygon_index = NONE;
            }

            return polygon_index;
        }

        /* walks backwards from the last node expanded, returning polygons as it goes; returns NONE
            when there are no more polygons to return.  this is useful for pathfinding: when
            flood_map() returns the destination polygon index, calling reverse_flood_map() will return
            the polygons traversed to reach the destination) */
        public static short reverse_flood_map()
        {
            short polygon_index = NONE;

            if (last_node_index_expanded != NONE)
            {
                node_data node;

                assert(last_node_index_expanded >= 0 && last_node_index_expanded < node_count);
                node = nodes[last_node_index_expanded];
                last_node_index_expanded = node.parent_node_index;
                polygon_index = node.polygon_index;
            }

            return polygon_index;
        }

        /* returns depth (in polygons) at last_node_index_expanded */
        public static short flood_depth()
        {
            assert(last_node_index_expanded >= 0 && last_node_index_expanded < node_count);
            return last_node_index_expanded == NONE ? (short) 0 : nodes[last_node_index_expanded].depth;
        }

        /* ---------- private code */

        /* checks to see if the given node is already in the node list */
        private static void add_node(MapLevel level, short parent_node_index, short polygon_index, short depth, int cost, int user_flags)
        {
            if (node_count < MAXIMUM_FLOOD_NODES)
            {
                node_data node;
                short node_index;

                /* see if this polygon already exists in the node list anywhere */
                assert(polygon_index >= 0 && polygon_index < level.PolygonList.Count);
                if ((node_index = visited_polygons[polygon_index]) != UNVISITED)
                {
                    /* there is already a node referencing this polygon; if it has a higher cost
                        than the cost we are attempting to add, replace it (because we are doing
                        a best-first search, we are guarenteed never to find a better path to an
                        expanded node, and in fact if we find a path to a node we have already
                        expanded we're backtracking and can ignore the node) */
                    assert(node_index >= 0 && node_index < node_count);
                    node = nodes[node_index];
                    if (NODE_IS_EXPANDED(node) || node.cost <= cost) node = null;
                }
                else
                {
                    node_index = node_count;
                    node = nodes[node_index];
                }

                if (node != null)
                {
                    if (node_index == node_count)
                    {
                        node_count += 1;
                    }

                    node.flags = 0;
                    node.parent_node_index = parent_node_index;
                    node.polygon_index = polygon_index;
                    node.depth = depth;
                    node.cost = cost;
                    node.user_flags = user_flags;

                    assert(polygon_index >= 0 && polygon_index < level.PolygonList.Count);
                    visited_polygons[polygon_index] = node_index;
                }
            }
        }
    }
}
