using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using UnityEngine;

namespace ForgePlus.UI
{
    // Shows something in the level (for an error's Show button): switches to the mode that selects it, selects it, and
    // frames it in the given part of the view (viewport coordinates, 0 to 1 from the bottom left)
    public static class LevelFocus
    {
        public static void ShowPoint(short pointIndex, Rect viewportArea)
        {
            SwitchMode(ModeManager.PrimaryModes.Geometry);

            // Points are only selectable while they're shown
            SettingsManager.Instance.PointsEnabled = true;

            var level = LevelEntity_Level.Instance;
            if (level && level.Points.TryGetValue(pointIndex, out var point))
            {
                SelectAndFrame(point, viewportArea);
            }
        }

        public static void ShowPolygon(short polygonIndex, Rect viewportArea)
        {
            SwitchMode(ModeManager.PrimaryModes.Geometry);

            var level = LevelEntity_Level.Instance;
            if (level && level.Polygons.TryGetValue(polygonIndex, out var polygon))
            {
                SelectAndFrame(polygon, viewportArea);
            }
        }

        // Lines aren't selected themselves, so their first side is (if they have one), and the line is framed
        public static void ShowLine(short lineIndex, Rect viewportArea)
        {
            SwitchMode(ModeManager.PrimaryModes.Geometry);

            var level = LevelEntity_Level.Instance;
            if (!level || !level.Lines.TryGetValue(lineIndex, out var line))
            {
                return;
            }

            SelectionManager.Instance.DeselectAll();

            var side = line.ClockwiseSide ? line.ClockwiseSide : line.CounterclockwiseSide;
            if (side)
            {
                SelectionManager.Instance.SelectObject(side, multiSelect: false);
            }

            ForgePlusUI.Instance.EditorCamera.Frame(new ISelectable[] { line }, viewportArea);
        }

        public static void ShowObject(short objectIndex, Rect viewportArea)
        {
            SwitchMode(ModeManager.PrimaryModes.Objects);

            var level = LevelEntity_Level.Instance;
            if (level && level.MapObjects.TryGetValue(objectIndex, out var mapObject))
            {
                SelectAndFrame(mapObject, viewportArea);
            }
        }

        public static void ShowAnnotation(short annotationIndex, Rect viewportArea)
        {
            SwitchMode(ModeManager.PrimaryModes.Annotations);

            var level = LevelEntity_Level.Instance;
            if (level && level.Annotations.TryGetValue(annotationIndex, out var annotation))
            {
                SelectAndFrame(annotation, viewportArea);
            }
        }

        // The level itself (its inspector), for what's about the whole level
        public static void ShowLevel()
        {
            SwitchMode(ModeManager.PrimaryModes.Level);
        }

        // With nothing carried over from the last mode's selection
        private static void SwitchMode(ModeManager.PrimaryModes mode)
        {
            SelectionManager.Instance.DeselectAll();
            ModeManager.Instance.PrimaryMode = mode;
        }

        private static void SelectAndFrame(ISelectable selectable, Rect viewportArea)
        {
            SelectionManager.Instance.SelectObject(selectable, multiSelect: false);
            ForgePlusUI.Instance.EditorCamera.Frame(new[] { selectable }, viewportArea);
        }
    }
}
