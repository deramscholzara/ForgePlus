#if !NO_EDITING
using System.Collections.Generic;
using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.ApplicationGeneral;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;

namespace ForgePlus.LevelManipulation
{
    // While any part of a platform is selected (the platform, its polygon, a line or side around it, or an object on it), in
    // any mode: Activate Platform (R) starts or stops its simulation, and Obstruct Platform (T) simulates something in its way
    // as it extends, as its inspector's Active and Obstruct Extension do. Neither cares whether a player could do it.
    public static class PlatformSimulationHotkeys
    {
        // Once a frame, from ForgePlusUI
        public static void Update()
        {
            // Not as a shortcut's key (with Ctrl or Command)
            if (Hotkeys.IsShortcutModifierPressed)
            {
                return;
            }

            var activates = Hotkeys.WasPressed(ForgePlusInput.Editing.ActivatePlatform);
            var obstructs = Hotkeys.WasPressed(ForgePlusInput.Editing.ObstructPlatform);
            if (!activates && !obstructs)
            {
                return;
            }

            var level = LevelEntity_Level.Instance;
            if (!level || LevelEditing.IsRebuildPending)
            {
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
