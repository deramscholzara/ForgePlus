#if !NO_EDITING
using AlephOne;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.LevelManipulation
{
    // The Heights palette's floor and ceiling heights: those in use, and those listed since the level was opened, which
    // stay (unused) until it's opened again
    [AutoStaticsCleanup]
    public static partial class HeightSwatches
    {
        public static event Action OnChanged;

        // Opening a level loads its data again, which starts new lists (rebuilding it for an edit keeps them)
        private static MapLevel sessionLevel;
        private static readonly SortedSet<short> sessionFloorHeights = new SortedSet<short>();
        private static readonly SortedSet<short> sessionCeilingHeights = new SortedSet<short>();

        public static IReadOnlyList<short> Heights(LevelEntity_Polygon.DataSources dataSource)
        {
            var sessionHeights = SessionHeights(dataSource);
            sessionHeights.UnionWith(UsageCounts(dataSource).Keys);

            return sessionHeights.ToList();
        }

        // By height (a platform's faces aren't counted, as their heights are its platform's)
        public static Dictionary<short, int> UsageCounts(LevelEntity_Polygon.DataSources dataSource)
        {
            var counts = new Dictionary<short, int>();

            foreach (var polygon in EditablePolygons())
            {
                var height = HeightsEditing.GetHeight(polygon, dataSource);
                counts[height] = counts.TryGetValue(height, out var count) ? count + 1 : 1;
            }

            return counts;
        }

        public static List<LevelEntity_Polygon> PolygonsAt(LevelEntity_Polygon.DataSources dataSource, short height)
        {
            return EditablePolygons().Where(polygon => HeightsEditing.GetHeight(polygon, dataSource) == height).ToList();
        }

        public static void Add(LevelEntity_Polygon.DataSources dataSource, short height)
        {
            if (SessionHeights(dataSource).Add(height))
            {
                OnChanged?.Invoke();
            }
        }

        // An unused height that's changed is listed at its new height instead (a used one stays while it's used)
        public static void Replace(LevelEntity_Polygon.DataSources dataSource, short oldHeight, short newHeight)
        {
            var sessionHeights = SessionHeights(dataSource);
            sessionHeights.Remove(oldHeight);
            sessionHeights.Add(newHeight);

            OnChanged?.Invoke();
        }

        private static SortedSet<short> SessionHeights(LevelEntity_Polygon.DataSources dataSource)
        {
            var level = LevelEntity_Level.Instance.Level;
            if (level != sessionLevel)
            {
                sessionLevel = level;
                sessionFloorHeights.Clear();
                sessionCeilingHeights.Clear();
            }

            return dataSource == LevelEntity_Polygon.DataSources.Ceiling ? sessionCeilingHeights : sessionFloorHeights;
        }

        private static IEnumerable<LevelEntity_Polygon> EditablePolygons()
        {
            return LevelEntity_Level.Instance.Polygons.Values.Where(HeightsEditing.CanEdit);
        }
    }
}
#endif
