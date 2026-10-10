#if !NO_EDITING
using ForgePlus.ApplicationGeneral;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // What's focused on (framed, as Frame Selected frames it) flashes green three times, so it stands out from what else is
    // in view: its surfaces (a polygon's floor and ceiling, a side's faces, a platform's moving surfaces, what a light lights
    // or a media covers), an object's icon, and a point's handles or a line's lines drawn over the level (PointHandles
    // reads PointIntensity and LineIntensity). Surfaces and icons
    // are overlaid with their own meshes, so the flash follows them.
    [AutoStaticsCleanup]
    public static partial class FocusFlash
    {
        private const float Duration = 0.84f;
        private const int Pulses = 3;
        private const float MaximumOverlayAlpha = 0.75f;

        public static readonly Color FlashColor = new Color(0f, 1f, 0.2f);

        private static readonly int colorPropertyId = Shader.PropertyToID("_Color");

        // Recreated when destroyed, such as at the end of a Play session
        private static Material material;

        private static List<GameObject> overlays = new List<GameObject>();
        private static HashSet<LevelEntity_Point> points = new HashSet<LevelEntity_Point>();
        private static HashSet<LevelEntity_Line> lines = new HashSet<LevelEntity_Line>();
        private static float intensity;

        // Each flash's, so a flash left from an earlier one stops
        private static int flashNumber;

        // How far toward the flash color a point's handle is now (0 while it isn't flashing)
        public static float PointIntensity(LevelEntity_Point point)
        {
            return points.Contains(point) ? intensity : 0f;
        }

        // After the delay (such as while the camera moves to frame them, so all three pulses are seen)
        // How far toward the flash color a line's lines are now (0 while it isn't flashing)
        public static float LineIntensity(LevelEntity_Line line)
        {
            return lines.Contains(line) ? intensity : 0f;
        }

        public static void Flash(IEnumerable<ISelectable> targets, float delay = 0f)
        {
            Stop();

            foreach (var target in targets)
            {
                if (target is LevelEntity_Point point)
                {
                    points.Add(point);
                    continue;
                }

                if (target is LevelEntity_Line line)
                {
                    lines.Add(line);
                    continue;
                }

                foreach (var meshFilter in MeshesOf(target))
                {
                    overlays.Add(CreateOverlay(meshFilter));
                }
            }

            if (overlays.Count > 0 || points.Count > 0 || lines.Count > 0)
            {
                Animate(++flashNumber, delay);
            }
        }

        private static async void Animate(int flash, float delay)
        {
            var startTime = Time.unscaledTime + delay;

            while (flash == flashNumber && Application.isPlaying)
            {
                var progress = (Time.unscaledTime - startTime) / Duration;
                if (progress >= 1f)
                {
                    Stop();
                    return;
                }

                // Three rises and falls, from nothing and back to nothing
                intensity = progress > 0f ? 0.5f - 0.5f * Mathf.Cos(progress * Pulses * 2f * Mathf.PI) : 0f;

                if (material)
                {
                    material.SetColor(colorPropertyId, new Color(FlashColor.r, FlashColor.g, FlashColor.b, intensity * MaximumOverlayAlpha));
                }

                await Awaitable.NextFrameAsync();
            }
        }

        private static void Stop()
        {
            flashNumber++;

            foreach (var overlay in overlays.Where(overlay => overlay))
            {
                overlay.SetActive(false);
                Object.Destroy(overlay);
            }

            overlays.Clear();
            points.Clear();
            lines.Clear();
            intensity = 0f;
        }

        private static GameObject CreateOverlay(MeshFilter meshFilter)
        {
            if (!material)
            {
                material = new Material(Shader.Find("ForgePlus/DiagnosticOverlay")) { name = "Focus Flash" };

                // Over diagnostic overlays
                material.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent + 1;
            }

            var overlay = new GameObject("Focus Flash");
            overlay.layer = meshFilter.gameObject.layer;
            overlay.transform.SetParent(meshFilter.transform, worldPositionStays: false);
            overlay.AddComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;

            var overlayRenderer = overlay.AddComponent<MeshRenderer>();
            RuntimeSurfaceGeometry.ConfigureRenderer(overlayRenderer);
            overlayRenderer.sharedMaterial = material;

            return overlay;
        }

        // The meshes that show it (not selection indicators, or other overlays)
        private static IEnumerable<MeshFilter> MeshesOf(ISelectable target)
        {
            var level = LevelEntity_Level.Instance;

            switch (target)
            {
                case LevelEntity_MapObject mapObject:
                    // Its icon (the placeholder geometry on its root), not its sprite
                    return mapObject.TryGetComponent<MeshFilter>(out var icon) ? new[] { icon } : new MeshFilter[0];
                case LevelEntity_Platform platform:
                    platform.GetMovingSurfaces(out var floorSurface, out var ceilingSurface, out var sides);
                    return new Component[] { floorSurface, ceilingSurface }.Concat(sides.Select(side => (Component) side))
                                                                          .Where(surface => surface)
                                                                          .Select(surface => surface.GetComponent<MeshFilter>());
                case LevelEntity_Light light:
                    return SurfacesOf(level).Where(surface => surface.RuntimeLight == light).Select(surface => surface.GetComponent<MeshFilter>());
                case LevelEntity_Media media:
                    return level.EditableSurface_Medias.Where(surface => surface && surface.Media == media).Select(surface => surface.GetComponent<MeshFilter>());
                case LevelEntity_Level _:
                    return new MeshFilter[0];
                case Component component when component:
                    // Polygons, sides and lines: their surfaces are their children
                    return component.GetComponentsInChildren<MeshFilter>()
                                    .Where(meshFilter => meshFilter.sharedMesh &&
                                                         meshFilter.gameObject.layer != SelectionManager.SelectionIndicatorLayer &&
                                                         meshFilter.name != "Focus Flash" && meshFilter.name != "Diagnostic Overlay")
                                    .ToList();
                default:
                    return new MeshFilter[0];
            }
        }

        private static IEnumerable<EditableSurface_Base> SurfacesOf(LevelEntity_Level level)
        {
            return level.EditableSurface_Polygons.Cast<EditableSurface_Base>()
                                                  .Concat(level.EditableSurface_Sides)
                                                  .Concat(level.EditableSurface_Medias)
                                                  .Where(surface => surface);
        }
    }
}
#endif
