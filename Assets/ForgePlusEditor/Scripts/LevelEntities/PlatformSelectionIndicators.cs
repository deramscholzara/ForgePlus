#if !NO_EDITING
using ForgePlus.LevelManipulation.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    // Selection indicators on the surfaces that move with a platform, refitted after PlatformSideClipping each frame
    [DefaultExecutionOrder(1001)]
    public class PlatformSelectionIndicators : MonoBehaviour
    {
        private class SurfaceIndicators
        {
            public MeshFilter SurfaceMeshFilter;
            public PlatformSideClipping SideClipping;
            public bool IsCeiling;

            public GameObject[] Corners;
            public Bounds LastBounds;
            public bool IsShown;
        }

        private readonly List<SurfaceIndicators> surfaces = new List<SurfaceIndicators>();

        public void Initialize(LevelEntity_Platform platform)
        {
            platform.GetMovingSurfaces(out var floorSurface, out var ceilingSurface, out var sides);

            if (floorSurface)
            {
                AddSurface(floorSurface, sideClipping: null, isCeiling: false);
            }

            if (ceilingSurface)
            {
                AddSurface(ceilingSurface, sideClipping: null, isCeiling: true);
            }

            foreach (var sideClipping in sides)
            {
                AddSurface(sideClipping.GetComponent<MeshFilter>(), sideClipping, isCeiling: false);
            }

            UpdateIndicators();
        }

        private void LateUpdate()
        {
            UpdateIndicators();
        }

        private void OnDestroy()
        {
            foreach (var surface in surfaces)
            {
                if (surface.Corners == null)
                {
                    continue;
                }

                foreach (var corner in surface.Corners)
                {
                    GeometryUtilities.DestroySurfaceSelectionIndicator(corner);
                }
            }

            surfaces.Clear();
        }

        private void AddSurface(MeshFilter surfaceMeshFilter, PlatformSideClipping sideClipping, bool isCeiling)
        {
            surfaces.Add(new SurfaceIndicators
            {
                SurfaceMeshFilter = surfaceMeshFilter,
                SideClipping = sideClipping,
                IsCeiling = isCeiling,
            });
        }

        private void UpdateIndicators()
        {
            foreach (var surface in surfaces)
            {
                var isShown = !surface.SideClipping || surface.SideClipping.Module.IsShownByPlatformClipping;

                // Clipping only changes a side's height, so its bounds change whenever its vertices do
                var bounds = surface.SurfaceMeshFilter.sharedMesh.bounds;

                if (isShown == surface.IsShown &&
                    surface.Corners != null &&
                    (!isShown || bounds == surface.LastBounds))
                {
                    continue;
                }

                surface.IsShown = isShown;
                surface.LastBounds = bounds;

                if (surface.Corners != null)
                {
                    foreach (var corner in surface.Corners)
                    {
                        corner.SetActive(isShown);
                    }
                }

                if (isShown)
                {
                    surface.Corners = GeometryUtilities.FitSurfaceSelectionIndicators("Platform Vertex",
                                                                                      surface.SurfaceMeshFilter.transform,
                                                                                      surface.SurfaceMeshFilter.sharedMesh.vertices,
                                                                                      surface.IsCeiling,
                                                                                      surface.Corners);
                }
            }
        }
    }
}
#endif
