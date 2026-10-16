#if !NO_EDITING
using System.Collections.Generic;
using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.ApplicationGeneral;
using ForgePlus.CameraNavigation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // In any mode: Use Platform (Tab) uses the platform in front of the camera as a player's action key would. And while
    // any part of a platform is selected (the platform, its polygon, a line or side around it, or an object on it),
    // Activate Platform (Shift+Tab) starts or stops its simulation, and Obstruct Platform (Ctrl+Tab) simulates something in
    // its way as it extends, as its inspector's Active and Obstruct Extension do (neither cares whether a player could do
    // it). Each acts on both halves of a split platform.
    public static class PlatformSimulationHotkeys
    {
        // Once a frame, from ForgePlusUI
        public static void Update()
        {
            // Obstruct's own combination is a shortcut (Ctrl+Tab), so only Use and Activate are kept from being a
            // shortcut's key (with Ctrl or Command)
            var obstructs = Hotkeys.WasPressed(ForgePlusInput.Editing.ObstructPlatform);
            var isShortcut = Hotkeys.IsShortcutModifierPressed;
            var activates = !isShortcut && Hotkeys.WasPressed(ForgePlusInput.Editing.ActivatePlatform);

            // Not as part of Activate's or Obstruct's combination (Shift+Tab, Ctrl+Tab), whose last key it is by default
            var uses = !isShortcut && !activates && !obstructs && Hotkeys.WasPressed(ForgePlusInput.Editing.UsePlatform);
            if (!activates && !obstructs && !uses)
            {
                return;
            }

            var level = LevelEntity_Level.Instance;
            if (!level || LevelEditing.IsRebuildPending)
            {
                return;
            }

            if (uses)
            {
                var target = FindPlayerTarget(level);
                if (target)
                {
                    target.PlayerTouchRuntimeBehavior();
                }

                return;
            }

            foreach (var platform in GetSelectedPlatforms(level))
            {
                if (activates)
                {
                    // Its other half too, for a platform that comes from both the floor and the ceiling
                    platform.SetRuntimeActive(!platform.IsRuntimeActive);
                }

                if (obstructs)
                {
                    platform.ObstructRuntimeBehavior();
                }
            }
        }

        // As a player's action key would (find_action_key_target in devices.cpp), from the camera: along its facing
        // (ignoring its pitch) across the map from the polygon it's in, the first platform polygon entered through a line
        // whose midpoint is in range, if a player could use that platform. Through no solid wall, and none in the
        // top-down view (which faces no direction across the map).
        private static LevelEntity_Platform FindPlayerTarget(LevelEntity_Level level)
        {
            var camera = Camera.main;
            var editorCamera = camera ? camera.GetComponent<EditorCamera>() : null;
            if (!camera || editorCamera && editorCamera.IsOrthographic)
            {
                return null;
            }

            var data = level.Level;
            var position = camera.transform.position * GeometryUtilities.WorldUnitIncrementsPerMeter;
            var eye = new world_point3d { x = (short) position.x, y = (short) -position.z, z = (short) position.y };
            var origin = new world_point2d(eye.x, eye.y);

            var forward = camera.transform.forward;
            var facing = new Vector2(forward.x, -forward.z);
            if (facing.sqrMagnitude < 0.0001f)
            {
                return null;
            }

            facing = facing.normalized * MaximumPlatformActivationRange;
            var destination = new world_point2d((short) (origin.x + facing.x), (short) (origin.y + facing.y));

            var polygonIndex = map.world_point_to_polygon_index(data, origin);
            while (polygonIndex != cstypes.NONE)
            {
                var lineIndex = map.find_line_crossed_leaving_polygon(data, polygonIndex, origin, destination);
                if (lineIndex == cstypes.NONE)
                {
                    return null;
                }

                polygonIndex = map.find_adjacent_polygon(data, polygonIndex, lineIndex);
                if (polygonIndex == cstypes.NONE)
                {
                    return null;
                }

                var polygon = data.PolygonList[polygonIndex];
                if (polygon.type == map._polygon_is_platform && IsLineWithinRange(data, lineIndex, eye))
                {
                    var platform = LevelEntity_Platform.GetSelectablePlatform(level, polygon.permutation);
                    if (platform && platform.IsLegalPlayerTarget)
                    {
                        return platform;
                    }
                }
            }

            return null;
        }

        // MAXIMUM_PLATFORM_ACTIVATION_RANGE (devices.cpp)
        private const float MaximumPlatformActivationRange = 3 * world.WORLD_ONE;

        // line_is_within_range (devices.cpp), from the camera (in place of the player's middle), with height counting
        // double (control_panel_settings.ReachHorizontal)
        private static bool IsLineWithinRange(MapLevel data, short lineIndex, world_point3d eye)
        {
            map.calculate_line_midpoint(data, lineIndex, out var midpoint);

            float dx = eye.x - midpoint.x;
            float dy = eye.y - midpoint.y;
            float dz = 2f * (eye.z - midpoint.z);

            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz) < MaximumPlatformActivationRange;
        }

        // Each once, as its inspector inspects it (the floor's part of a platform that comes from both)
        private static List<LevelEntity_Platform> GetSelectedPlatforms(LevelEntity_Level level)
        {
            var platformIndexes = new HashSet<short>();
            var data = level.Level;

            void AddPolygon(short polygonIndex)
            {
                if (polygonIndex < 0 || polygonIndex >= data.PolygonList.Count)
                {
                    return;
                }

                var polygon = data.PolygonList[polygonIndex];
                if (polygon.GetPlatform(data) != null)
                {
                    platformIndexes.Add(polygon.permutation);
                }
            }

            // A line's sides move with the platform on either side of it
            void AddLine(short lineIndex)
            {
                if (lineIndex < 0 || lineIndex >= data.LineList.Count)
                {
                    return;
                }

                var line = data.LineList[lineIndex];
                AddPolygon(line.clockwise_polygon_owner);
                AddPolygon(line.counterclockwise_polygon_owner);
            }

            foreach (var selected in SelectionManager.Instance.Selection)
            {
                switch (selected)
                {
                    case LevelEntity_Platform platform:
                        platformIndexes.Add(platform.NativeIndex);
                        break;
                    case LevelEntity_Polygon polygon:
                        AddPolygon(polygon.NativeIndex);
                        break;
                    case LevelEntity_Side side:
                        AddLine(side.ParentLineIndex);
                        break;
                    case LevelEntity_Line line:
                        AddLine(line.NativeIndex);
                        break;
                    case LevelEntity_MapObject mapObject:
                        AddPolygon(mapObject.NativeObject.polygon_index);
                        break;
                }
            }

            var platforms = new List<LevelEntity_Platform>();
            foreach (var platformIndex in platformIndexes)
            {
                var platform = LevelEntity_Platform.GetSelectablePlatform(level, platformIndex);
                if (platform)
                {
                    platforms.Add(platform);
                }
            }

            return platforms;
        }
    }
}
#endif
