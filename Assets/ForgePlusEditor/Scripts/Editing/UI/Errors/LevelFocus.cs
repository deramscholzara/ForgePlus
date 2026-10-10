using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation;
using RuntimeCore.Entities;

namespace ForgePlus.UI
{
    // Shows something in the level (for an error's Show button): switches to the mode that selects it, selects it, and
    // focuses on it as Frame Selected does (framing it, and flashing it)
    public static class LevelFocus
    {
        public static void ShowPoint(short pointIndex)
        {
            SwitchMode(ModeManager.PrimaryModes.Geometry);

            // Points are only selectable while they're shown
            SettingsManager.Instance.PointsEnabled = true;

            var level = LevelEntity_Level.Instance;
            if (level && level.Points.TryGetValue(pointIndex, out var point))
            {
                SelectAndFocus(point);
            }
        }

        public static void ShowPolygon(short polygonIndex)
        {
            SwitchMode(ModeManager.PrimaryModes.Geometry);

            var level = LevelEntity_Level.Instance;
            if (level && level.Polygons.TryGetValue(polygonIndex, out var polygon))
            {
                SelectAndFocus(polygon);
            }
        }

        // Lines aren't selected themselves, so their first side is (or, if they have none, the polygon on either side)
        public static void ShowLine(short lineIndex)
        {
            SwitchMode(ModeManager.PrimaryModes.Geometry);

            var level = LevelEntity_Level.Instance;
            if (!level || !level.Lines.TryGetValue(lineIndex, out var line))
            {
                return;
            }

            var side = line.ClockwiseSide ? line.ClockwiseSide : line.CounterclockwiseSide;
            if (side)
            {
                SelectAndFocus(side);
                return;
            }

            var owner = line.NativeObject.clockwise_polygon_owner >= 0 ? line.NativeObject.clockwise_polygon_owner : line.NativeObject.counterclockwise_polygon_owner;
            if (level.Polygons.TryGetValue(owner, out var polygon))
            {
                SelectAndFocus(polygon);
            }
        }

        public static void ShowObject(short objectIndex)
        {
            SwitchMode(ModeManager.PrimaryModes.Objects);

            var level = LevelEntity_Level.Instance;
            if (level && level.MapObjects.TryGetValue(objectIndex, out var mapObject))
            {
                SelectAndFocus(mapObject);
            }
        }

        public static void ShowAnnotation(short annotationIndex)
        {
            SwitchMode(ModeManager.PrimaryModes.Annotations);

            var level = LevelEntity_Level.Instance;
            if (level && level.Annotations.TryGetValue(annotationIndex, out var annotation))
            {
                SelectAndFocus(annotation);
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

        private static void SelectAndFocus(ISelectable selectable)
        {
            SelectionManager.Instance.SelectObject(selectable, multiSelect: false);
            ForgePlusUI.Instance.EditorCamera.FrameSelected();
        }
    }
}
