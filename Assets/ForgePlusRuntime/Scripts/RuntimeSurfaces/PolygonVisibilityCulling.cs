using RuntimeCore.Entities.MapObjects;
using System;
using Unity.Scripting.LifecycleManagement;

namespace RuntimeCore.Entities.Geometry
{
    // Which of the level's polygons are drawn, as Aleph One would draw them from a point (the Culling Simulation view
    // option): the surfaces and objects of the others are hidden, and clicks pass through them. While it's cleared,
    // nothing is culled.
    [AutoStaticsCleanup]
    public static partial class PolygonVisibilityCulling
    {
        // Indexed by polygon, or null while nothing is culled
        // Not readonly, so AutoStaticsCleanup resets it each Play session
        private static bool[] visiblePolygons = null;

        public static bool IsActive
        {
            get
            {
                return visiblePolygons != null;
            }
        }

        // A polygon beyond those it was given (such as one added since) isn't culled until it's given them again
        public static bool IsCulled(short polygonIndex)
        {
            return visiblePolygons != null &&
                   polygonIndex >= 0 &&
                   polygonIndex < visiblePolygons.Length &&
                   !visiblePolygons[polygonIndex];
        }

        // Only those visible are drawn; what's shown is only applied again where it differs from before
        public static void SetVisiblePolygons(bool[] visible)
        {
            if (visible == null)
            {
                Clear();
                return;
            }

            if (visiblePolygons != null && visiblePolygons.AsSpan().SequenceEqual(visible))
            {
                return;
            }

            if (visiblePolygons == null || visiblePolygons.Length != visible.Length)
            {
                visiblePolygons = new bool[visible.Length];
            }

            Array.Copy(visible, visiblePolygons, visible.Length);

            Apply();
        }

        public static void Clear()
        {
            if (visiblePolygons == null)
            {
                return;
            }

            visiblePolygons = null;

            Apply();
        }

        private static void Apply()
        {
            RuntimeSurfaceGeometry.ApplyVisibilityToAll();
            LevelEntity_MapObject.ApplyVisibilityToAllObjects();
        }
    }
}
