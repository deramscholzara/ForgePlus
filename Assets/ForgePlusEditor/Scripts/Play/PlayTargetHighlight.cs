#if !NO_EDITING
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.Play
{
    // In Play mode, what the player is looking at that the action key can use (ActionKeyTargeting) is overlaid: green
    // while it's within the game's reach, or yellow while it isn't (which Play mode still lets the player use). A control
    // panel's overlay covers its primary surface, and a platform's its moving surfaces. Overlays share their surfaces'
    // meshes and follow their transforms, so they keep up with a platform as it moves.
    [AutoStaticsCleanup]
    public static partial class PlayTargetHighlight
    {
        public const string OverlayName = "Play Target";

        private static readonly Color InReachColor = new Color(0f, 1f, 0f, 0.05f);
        private static readonly Color OutOfReachColor = new Color(1f, 1f, 0f, 0.05f);

        private static readonly int colorPropertyId = Shader.PropertyToID("_Color");

        private static readonly List<GameObject> overlays = new List<GameObject>();

        private static Material material;
        private static ActionKeyTargeting.Target shown;
        private static bool isShown;

        // The target, or none, and whether it's in reach. Overlays are made again only as the target changes.
        public static void Show(ActionKeyTargeting.Target target, bool isInReach)
        {
            if (target.Type == ActionKeyTargeting.TargetTypes.None)
            {
                Clear();
                return;
            }

            if (!isShown || !target.Matches(shown))
            {
                Clear();
                CreateOverlays(target);
                shown = target;
                isShown = true;
            }

            Material.SetColor(colorPropertyId, isInReach ? InReachColor : OutOfReachColor);
        }

        public static void Clear()
        {
            foreach (var overlay in overlays)
            {
                if (overlay)
                {
                    Object.Destroy(overlay);
                }
            }

            overlays.Clear();
            isShown = false;
        }

        private static Material Material
        {
            get
            {
                if (!material)
                {
                    material = new Material(Shader.Find("ForgePlus/DiagnosticOverlay")) { name = "Play Target" };
                }

                return material;
            }
        }

        private static void CreateOverlays(ActionKeyTargeting.Target target)
        {
            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                return;
            }

            switch (target.Type)
            {
                case ActionKeyTargeting.TargetTypes.Platform:
                    foreach (var meshFilter in FocusFlash.MeshesOf(target.Platform))
                    {
                        CreateOverlay(meshFilter);
                    }

                    break;
                case ActionKeyTargeting.TargetTypes.ControlPanel:
                    if (level.Sides.TryGetValue(target.SideIndex, out var side) && side.PrimarySurface &&
                        side.PrimarySurface.TryGetComponent<MeshFilter>(out var panelMesh))
                    {
                        CreateOverlay(panelMesh);
                    }

                    break;
            }
        }

        private static void CreateOverlay(MeshFilter meshFilter)
        {
            if (!meshFilter || !meshFilter.sharedMesh)
            {
                return;
            }

            var overlay = new GameObject(OverlayName);
            overlay.layer = meshFilter.gameObject.layer;
            overlay.transform.SetParent(meshFilter.transform, worldPositionStays: false);
            overlay.AddComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;

            var overlayRenderer = overlay.AddComponent<MeshRenderer>();
            RuntimeSurfaceGeometry.ConfigureRenderer(overlayRenderer);
            overlayRenderer.sharedMaterial = Material;
            GeometryUtilities.ExcludeFromGpuResidentDrawer(overlayRenderer);
            SurfaceCacheRenderingLayers.Exclude(overlayRenderer);

            overlays.Add(overlay);
        }
    }
}
#endif
