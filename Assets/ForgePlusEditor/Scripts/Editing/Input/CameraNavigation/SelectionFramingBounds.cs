using ForgePlus.LevelManipulation;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.CameraNavigation
{
    // The world-space bounds that framing fits into the camera's view
    public static class SelectionFramingBounds
    {
        // Selections with no size (such as a point) are framed as if they were this big (in meters)
        private static readonly Vector3 SizeOfSizelessSelection = Vector3.one;

        private static readonly List<MeshFilter> meshFilters = new List<MeshFilter>();

        public static bool TryGetBounds(IReadOnlyList<ISelectable> selection, out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;

            foreach (var selectable in selection)
            {
                if (TryGetBounds(selectable, out var selectableBounds))
                {
                    Encapsulate(selectableBounds, ref bounds, ref hasBounds);
                }
            }

            return hasBounds;
        }

        private static bool TryGetBounds(ISelectable selectable, out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;

            switch (selectable)
            {
                case LevelEntity_Platform platform:
                    platform.GetMovingSurfaces(out var floorSurface, out var ceilingSurface, out var sides);

                    if (floorSurface)
                    {
                        EncapsulateMeshes(floorSurface.gameObject, includeChildren: false, ref bounds, ref hasBounds);
                    }

                    if (ceilingSurface)
                    {
                        EncapsulateMeshes(ceilingSurface.gameObject, includeChildren: false, ref bounds, ref hasBounds);
                    }

                    // Sides as they are now: clipped, or covering the platform's whole travel while Clip Platform Sides is off
                    foreach (var sideClipping in sides)
                    {
                        if (sideClipping.Module.IsShownByPlatformClipping)
                        {
                            EncapsulateMeshes(sideClipping.gameObject, includeChildren: false, ref bounds, ref hasBounds);
                        }
                    }

                    break;

                case LevelEntity_Light light:
                    // Lights have no place of their own, so frame everything they light
                    EncapsulateSurfaces(LevelEntity_Level.Instance.EditableSurface_Polygons, surface => surface.RuntimeLight == light, ref bounds, ref hasBounds);
                    EncapsulateSurfaces(LevelEntity_Level.Instance.EditableSurface_Sides, surface => surface.RuntimeLight == light, ref bounds, ref hasBounds);
                    EncapsulateSurfaces(LevelEntity_Level.Instance.EditableSurface_Medias, surface => surface.RuntimeLight == light, ref bounds, ref hasBounds);

                    break;

                case LevelEntity_Media media:
                    // Media have no place of their own, so frame everywhere they are
                    EncapsulateSurfaces(LevelEntity_Level.Instance.EditableSurface_Medias, surface => surface.Media == media, ref bounds, ref hasBounds);

                    break;

                case LevelEntity_Level level:
                    foreach (var surface in level.EditableSurface_Polygons)
                    {
                        EncapsulateMeshes(surface.gameObject, includeChildren: false, ref bounds, ref hasBounds);
                    }

                    break;

                case Component component:
                    // Polygons, lines, sides, map objects, annotations, etc. - their surfaces are their children
                    EncapsulateMeshes(component.gameObject, includeChildren: true, ref bounds, ref hasBounds);

                    if (!hasBounds)
                    {
                        bounds = new Bounds(component.transform.position, Vector3.zero);
                        hasBounds = true;
                    }

                    break;
            }

            if (hasBounds && bounds.size.sqrMagnitude < Mathf.Epsilon)
            {
                bounds.size = SizeOfSizelessSelection;
            }

            return hasBounds;
        }

        private static void EncapsulateSurfaces<T>(List<T> surfaces, Func<T, bool> isFramed, ref Bounds bounds, ref bool hasBounds) where T : Component
        {
            foreach (var surface in surfaces)
            {
                if (isFramed(surface))
                {
                    EncapsulateMeshes(surface.gameObject, includeChildren: false, ref bounds, ref hasBounds);
                }
            }
        }

        private static void EncapsulateMeshes(GameObject gameObject, bool includeChildren, ref Bounds bounds, ref bool hasBounds)
        {
            meshFilters.Clear();

            if (includeChildren)
            {
                gameObject.GetComponentsInChildren(includeInactive: false, meshFilters);
            }
            else
            {
                gameObject.GetComponents(meshFilters);
            }

            foreach (var meshFilter in meshFilters)
            {
                // Selection indicators aren't part of what's selected
                if (!meshFilter.sharedMesh || meshFilter.gameObject.layer == SelectionManager.SelectionIndicatorLayer)
                {
                    continue;
                }

                Encapsulate(TransformBounds(meshFilter.transform.localToWorldMatrix, meshFilter.sharedMesh.bounds), ref bounds, ref hasBounds);
            }

            meshFilters.Clear();
        }

        private static void Encapsulate(Bounds addedBounds, ref Bounds bounds, ref bool hasBounds)
        {
            if (hasBounds)
            {
                bounds.Encapsulate(addedBounds);
            }
            else
            {
                bounds = addedBounds;
                hasBounds = true;
            }
        }

        private static Bounds TransformBounds(Matrix4x4 localToWorldMatrix, Bounds localBounds)
        {
            var worldBounds = new Bounds(localToWorldMatrix.MultiplyPoint3x4(localBounds.center), Vector3.zero);

            for (var i = 0; i < 8; i++)
            {
                var corner = localBounds.center + Vector3.Scale(localBounds.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                worldBounds.Encapsulate(localToWorldMatrix.MultiplyPoint3x4(corner));
            }

            return worldBounds;
        }
    }
}
