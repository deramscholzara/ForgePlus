using AlephOne;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using static AlephOne.cstypes;

namespace ForgePlus.LevelManipulation
{
    // What a player's action key would use (find_action_key_target in devices.cpp): along its facing across the map from
    // the polygon it's in, the first platform polygon entered through a line in reach whose platform a player could use,
    // or the first control panel on a line in reach (on its side facing the player) that can be toggled. Through no solid
    // wall. Shared by Play mode's player and the Use Platform hotkey (PlatformSimulationHotkeys).
    public static class ActionKeyTargeting
    {
        public enum TargetTypes
        {
            None,
            Platform,
            ControlPanel,
        }

        public struct Target
        {
            public TargetTypes Type;
            public LevelEntity_Platform Platform;
            public short SideIndex;

            // A control panel passed over on the way, which couldn't be toggled (or NONE)
            public short UnusableSideIndex;

            // The same thing, found the same way
            public bool Matches(Target other)
            {
                return Type == other.Type && Platform == other.Platform && SideIndex == other.SideIndex;
            }
        }

        // MAXIMUM_ACTIVATION_RANGE (devices.cpp): how far along its facing the action key looks
        private const short MaximumActivationRange = 3 * world.WORLD_ONE;

        // How far Find looks while ignoring reach (as far as a ray across a map can go)
        private const short LookRange = short.MaxValue;

        // control_panel_settings' defaults (devices.cpp): how far a panel can be reached, and how much more height counts
        // than distance across the map, reaching for a panel or a platform
        private const int ControlPanelReachDistance = devices.MAXIMUM_CONTROL_ACTIVATION_RANGE;
        private const int ControlPanelReachHorizontal = 2;

        // map is the level as it is now (Play mode's has its platforms' current heights), and reachOrigin where reach is
        // measured from (a player's middle: its feet plus half its height). canTogglePanel is null to look for platforms
        // only; otherwise it says whether a control panel's side can be toggled (the search goes on past one that can't).
        // Ignoring reach, it finds what the player is looking at, however far: the first of them that a solid line (a
        // wall, or a closed door) isn't in front of.
        public static Target Find(LevelEntity_Level level, MapLevel map, short polygonIndex, world_point2d location, short facing,
            world_point3d reachOrigin, Func<short, bool> canTogglePanel, bool ignoresReach = false)
        {
            var target = new Target { Type = TargetTypes.None, SideIndex = NONE, UnusableSideIndex = NONE };
            if (polygonIndex == NONE)
            {
                return target;
            }

            AlephOne.map.ray_to_line_segment(location, out var destination, world.NORMALIZE_ANGLE(facing),
                ignoresReach ? LookRange : MaximumActivationRange);

            var currentPolygon = polygonIndex;
            while (true)
            {
                var lineIndex = AlephOne.map.find_line_crossed_leaving_polygon(map, currentPolygon, location, destination);
                if (lineIndex == NONE)
                {
                    return target;
                }

                var originalPolygon = currentPolygon;
                currentPolygon = AlephOne.map.find_adjacent_polygon(map, currentPolygon, lineIndex);

                var done = false;

                if (currentPolygon != NONE)
                {
                    var polygon = map.PolygonList[currentPolygon];

                    /* We hit a platform */
                    if (polygon.type == AlephOne.map._polygon_is_platform &&
                        (ignoresReach || IsLineWithinRange(map, lineIndex, reachOrigin, devices.MAXIMUM_PLATFORM_ACTIVATION_RANGE)))
                    {
                        var platform = LevelEntity_Platform.GetSelectablePlatform(level, polygon.permutation);
                        if (platform && platform.IsLegalPlayerTarget)
                        {
                            target = new Target { Type = TargetTypes.Platform, Platform = platform, SideIndex = NONE, UnusableSideIndex = target.UnusableSideIndex };
                            done = true;
                        }
                    }
                }
                else
                {
                    done = true;
                }

                /* Slammed a wall */
                if (canTogglePanel != null &&
                    (ignoresReach || IsLineWithinRange(map, lineIndex, reachOrigin, ControlPanelReachDistance)) &&
                    devices.line_side_has_control_panel(map, lineIndex, originalPolygon, out var sideIndex))
                {
                    if (canTogglePanel(sideIndex))
                    {
                        return new Target { Type = TargetTypes.ControlPanel, SideIndex = sideIndex, UnusableSideIndex = target.UnusableSideIndex };
                    }

                    if (target.UnusableSideIndex == NONE)
                    {
                        target.UnusableSideIndex = sideIndex;
                    }
                }

                // Looking, nothing is seen through a wall
                if (done || ignoresReach && AlephOne.map.LINE_IS_SOLID(map.LineList[lineIndex]))
                {
                    return target;
                }
            }
        }

        // line_is_within_range (devices.cpp): from the reach origin to the line's midpoint, with height counting more
        // (ReachHorizontal times)
        private static bool IsLineWithinRange(MapLevel map, short lineIndex, world_point3d reachOrigin, int range)
        {
            AlephOne.map.calculate_line_midpoint(map, lineIndex, out var midpoint);

            long dx = reachOrigin.x - midpoint.x;
            long dy = reachOrigin.y - midpoint.y;
            long dz = ControlPanelReachHorizontal * (reachOrigin.z - midpoint.z);

            return Math.Sqrt(dx * dx + dy * dy + dz * dz) < range;
        }
    }
}
