using AlephOne;
using ForgePlus.Localization;
using ForgePlus.UI;
using RuntimeCore.Entities;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.computer_interface;

namespace ForgePlus.Inspection
{
    // What a permutation is used as (by a polygon's type, a control panel's class or a terminal group's type), as Aleph
    // One uses it, with what's wrong with what it refers to (such as a light that doesn't exist)
    [NoAutoStaticsCleanup]
    public static class PermutationCaptions
    {
        // map.h's polygon types; player.cpp: update_player_teleport for teleporters and automatic exits; map.cpp:
        // changed_polygon for triggers
        public static string ForPolygon(polygon_data polygon)
        {
            var permutation = polygon.permutation;

            switch (polygon.type)
            {
                case map._polygon_is_base:
                    return Strings.Get(Strings.Common, "Permutation.Polygon.Base");
                case map._polygon_is_platform:
                    return Strings.Get(Strings.Common, "Permutation.Polygon.Platform");
                case map._polygon_is_light_on_trigger:
                    return WithProblem(Strings.Get(Strings.Common, "Permutation.Polygon.LightOnTrigger"), LightProblem(permutation));
                case map._polygon_is_light_off_trigger:
                    return WithProblem(Strings.Get(Strings.Common, "Permutation.Polygon.LightOffTrigger"), LightProblem(permutation));
                case map._polygon_is_platform_on_trigger:
                    return WithProblem(Strings.Get(Strings.Common, "Permutation.Polygon.PlatformOnTrigger"), PlatformPolygonProblem(permutation));
                case map._polygon_is_platform_off_trigger:
                    return WithProblem(Strings.Get(Strings.Common, "Permutation.Polygon.PlatformOffTrigger"), PlatformPolygonProblem(permutation));
                case map._polygon_is_teleporter:
                    // A negative destination is another level's (the level is -permutation - 1)
                    return permutation < 0 ?
                           Strings.Get(Strings.Common, "Permutation.Polygon.TeleporterToLevel", -permutation - 1) :
                           WithProblem(Strings.Get(Strings.Common, "Permutation.Polygon.Teleporter"), PolygonProblem(permutation));
                case map._polygon_is_automatic_exit:
                    return Strings.Get(Strings.Common, "Permutation.Polygon.AutomaticExit");
                default:
                    return Strings.Get(Strings.Common, "Permutation.Polygon.Unused");
            }
        }

        // devices.cpp: change_panel_state and initialize_control_panels_for_level
        public static string ForControlPanel(side_data side)
        {
            if (side == null || !map.SIDE_IS_CONTROL_PANEL(side) || devices.get_control_panel_definition(side.control_panel_type) == null)
            {
                return string.Empty;
            }

            var permutation = side.control_panel_permutation;

            switch (devices.get_panel_class(side.control_panel_type))
            {
                case map._panel_is_light_switch:
                    return WithProblem(Strings.Get(Strings.Geometry, "Permutation.ControlPanel.LightSwitch"), LightProblem(permutation));
                case map._panel_is_platform_switch:
                    // A negative one stops being a control panel as the level starts
                    return permutation < 0 ?
                           Strings.Get(Strings.Geometry, "Permutation.ControlPanel.PlatformSwitchDisabled") :
                           WithProblem(Strings.Get(Strings.Geometry, "Permutation.ControlPanel.PlatformSwitch"), PlatformPolygonProblem(permutation));
                case map._panel_is_tag_switch:
                    return Strings.Get(Strings.Geometry, permutation == 0 ? "Permutation.ControlPanel.TagSwitchZero" : "Permutation.ControlPanel.TagSwitch");
                case map._panel_is_computer_terminal:
                    return WithProblem(Strings.Get(Strings.Geometry, "Permutation.ControlPanel.Terminal"), TerminalProblem(permutation));
                default:
                    return Strings.Get(Strings.Geometry, "Permutation.ControlPanel.Unused");
            }
        }

        // computer_interface.cpp: what each group type does with its permutation
        public static string ForTerminalGroup(terminal_groupings group)
        {
            if (group == null)
            {
                return string.Empty;
            }

            switch (group.type)
            {
                case _logon_group:
                    return Strings.Get(Strings.Terminals, "Permutation.Group.Logon");
                case _logoff_group:
                    return Strings.Get(Strings.Terminals, "Permutation.Group.Logoff");
                case _pict_group:
                    return Strings.Get(Strings.Terminals, "Permutation.Group.Pict");
                case _interlevel_teleport_group:
                    return WithProblem(Strings.Get(Strings.Terminals, "Permutation.Group.InterlevelTeleport"), TerminalGroupProblem(group, "Permutation.Problem.NoLevel"));
                case _intralevel_teleport_group:
                    return WithProblem(Strings.Get(Strings.Terminals, "Permutation.Group.IntralevelTeleport"), TerminalGroupProblem(group, "Permutation.Problem.NoPolygon"));
                case _checkpoint_group:
                    return WithProblem(Strings.Get(Strings.Terminals, "Permutation.Group.Checkpoint"), TerminalGroupProblem(group, "Permutation.Problem.NoGoal"));
                case _sound_group:
                    return Strings.Get(Strings.Terminals, "Permutation.Group.Sound");
                case _static_group:
                    return Strings.Get(Strings.Terminals, "Permutation.Group.Static");
                case _tag_group:
                    return Strings.Get(Strings.Terminals, "Permutation.Group.Tag");
                case _movie_group:
                    return Strings.Get(Strings.Terminals, "Permutation.Group.Movie");
                case _track_group:
                    return Strings.Get(Strings.Terminals, "Permutation.Group.Track");
                default:
                    return Strings.Get(Strings.Terminals, "Permutation.Group.Unused");
            }
        }

        private static string WithProblem(string caption, string problem)
        {
            return string.IsNullOrEmpty(problem) ? caption : $"{caption} {problem}";
        }

        private static string TerminalGroupProblem(terminal_groupings group, string problemKey)
        {
            return TerminalRules.IsPermutationValid(group.type, group.permutation) ?
                   null :
                   Strings.Get(Strings.Common, problemKey, group.permutation);
        }

        private static string LightProblem(short lightIndex)
        {
            var level = LevelEntity_Level.Instance;

            return level && !level.Lights.ContainsKey(lightIndex) ?
                   Strings.Get(Strings.Common, "Permutation.Problem.NoLight", lightIndex) :
                   null;
        }

        private static string PolygonProblem(short polygonIndex)
        {
            var level = LevelEntity_Level.Instance;

            return level && !level.Polygons.ContainsKey(polygonIndex) ?
                   Strings.Get(Strings.Common, "Permutation.Problem.NoPolygon", polygonIndex) :
                   null;
        }

        // Platform triggers and switches name a polygon, whose own permutation is its platform
        private static string PlatformPolygonProblem(short polygonIndex)
        {
            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                return null;
            }

            if (!level.Polygons.TryGetValue(polygonIndex, out var polygon))
            {
                return Strings.Get(Strings.Common, "Permutation.Problem.NoPolygon", polygonIndex);
            }

            return polygon.NativeObject.type != map._polygon_is_platform ?
                   Strings.Get(Strings.Common, "Permutation.Problem.NotPlatform", polygonIndex) :
                   null;
        }

        private static string TerminalProblem(short terminalIndex)
        {
            var level = LevelEntity_Level.Instance;

            return level && (terminalIndex < 0 || terminalIndex >= number_of_terminal_texts(level.Level)) ?
                   Strings.Get(Strings.Common, "Permutation.Problem.NoTerminal", terminalIndex) :
                   null;
        }
    }
}
