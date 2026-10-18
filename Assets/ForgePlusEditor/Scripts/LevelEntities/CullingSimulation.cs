using AlephOne;
using ForgePlus.History;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.PolygonContainment;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // The Culling Simulation view option: while it's on, the level shows only what Aleph One would draw from a point (the
    // camera, or a reference point's cube dragged around the level), in every direction: the polygon the point is in,
    // and those visible from it (PolygonContainmentMap.FindVisiblePolygons). While the point isn't in a polygon, nothing
    // is culled. Which polygon it's in is only looked for again as it (or the level's polygons) move
    // (PolygonContainmentTracker). Updated once a frame (ForgePlusUI), after the camera and what's dragged have moved.
    [AutoStaticsCleanup]
    public static partial class CullingSimulation
    {
        public enum Modes
        {
            Off,
            Camera,
            ReferencePoint,
        }

        // How far in front of the camera the reference point starts, and its cube's size (meters)
        private const float ReferencePointStartDistance = 3f;
        private const float ReferencePointSize = 0.25f;

        private static readonly Color ReferencePointColor = Color.white;
        private static readonly int colorPropertyId = Shader.PropertyToID("_Color");

        private static Modes mode = Modes.Off;

        // What's culled from: a child of the camera, or the reference point's cube
        private static GameObject point = null;
        private static PolygonContainmentTracker tracker = null;
        private static Material referencePointMaterial = null;

        // What visibility was last found from (the map, its polygon and where in it), or whether it must be found again
        // (as an edit can change which lines can be seen through, without moving anything)
        private static int computedVersion;
        private static short computedPolygonIndex = cstypes.NONE;
        private static int2 computedLocation;
        private static bool isEdited;
        private static bool[] visiblePolygons = null;

        public static Modes Mode
        {
            get
            {
                return mode;
            }
            set
            {
                if (value == mode)
                {
                    return;
                }

                // Leaving a mode clears what it culled (and removes its point); entering Reference Point puts the cube back
                // in front of the camera
                DestroyPoint();
                PolygonVisibilityCulling.Clear();

                mode = value;

                if (mode != Modes.Off)
                {
                    CreatePoint();
                    UndoHistory.OnChanged -= OnHistoryChanged;
                    UndoHistory.OnChanged += OnHistoryChanged;
                }
                else
                {
                    UndoHistory.OnChanged -= OnHistoryChanged;
                }
            }
        }

        public static void Update()
        {
            if (mode == Modes.Off)
            {
                return;
            }

            if (!point)
            {
                // Destroyed from outside (such as with its camera), so it's made again
                CreatePoint();
            }

            if (isEdited)
            {
                // Lines' transparency (which the map keeps) may have changed
                PolygonContainmentMap.MarkChanged();
            }

            tracker.Evaluate();

            var map = PolygonContainmentMap.Current;
            var polygonIndex = tracker.PolygonIndex;
            if (map == null || polygonIndex == cstypes.NONE)
            {
                computedPolygonIndex = cstypes.NONE;
                isEdited = false;
                PolygonVisibilityCulling.Clear();
                return;
            }

            var location = tracker.Location;
            var location2D = new int2(location.x, location.y);

            // Doors and other platforms opening (or closing) as they're simulated show (or hide) what's past them
            var platformsChanged = map.ApplyPlatformHeights(LevelEntity_Level.Instance);

            if (!isEdited && !platformsChanged && map.Version == computedVersion && polygonIndex == computedPolygonIndex && location2D.Equals(computedLocation))
            {
                return;
            }

            if (visiblePolygons == null || visiblePolygons.Length != map.PolygonCount)
            {
                visiblePolygons = new bool[map.PolygonCount];
            }

            map.FindVisiblePolygons(polygonIndex, location2D, visiblePolygons);

            computedVersion = map.Version;
            computedPolygonIndex = polygonIndex;
            computedLocation = location2D;
            isEdited = false;

            PolygonVisibilityCulling.SetVisiblePolygons(visiblePolygons);
        }

        private static void OnHistoryChanged()
        {
            isEdited = true;
        }

        private static void CreatePoint()
        {
            var cameraTransform = ForgePlusUI.Instance.EditorCamera.transform;

            if (mode == Modes.Camera)
            {
                point = new GameObject("Culling Simulation Point");
                point.transform.SetParent(cameraTransform, worldPositionStays: false);
            }
            else
            {
                point = CreateReferencePoint(cameraTransform.position + cameraTransform.forward * ReferencePointStartDistance);
            }

            // Only from floor to ceiling
            tracker = point.AddComponent<PolygonContainmentTracker>();
            tracker.WithHeight = true;

            computedPolygonIndex = cstypes.NONE;
            isEdited = true;
        }

        // A plain white cube, which takes presses before the level (ReferencePointHandle), but isn't hit by the level's
        // own raycasts
        private static GameObject CreateReferencePoint(Vector3 position)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Culling Simulation Reference Point";
            cube.layer = 2;
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * ReferencePointSize;

            if (!referencePointMaterial)
            {
                referencePointMaterial = new Material(Shader.Find("ForgePlus/DiagnosticOverlay")) { name = "Culling Simulation Reference Point" };
                referencePointMaterial.SetColor(colorPropertyId, ReferencePointColor);
            }

            var cubeRenderer = cube.GetComponent<MeshRenderer>();
            cubeRenderer.sharedMaterial = referencePointMaterial;
            RuntimeSurfaceGeometry.ConfigureRenderer(cubeRenderer);
            SurfaceCacheRenderingLayers.Exclude(cubeRenderer);

            var handle = cube.AddComponent<ReferencePointHandle>();
            WorldPointer.Instance.OverlayPicker = handle;

            return cube;
        }

        private static void DestroyPoint()
        {
            if (point)
            {
                if (point.TryGetComponent<ReferencePointHandle>(out var handle) && WorldPointer.Instance && WorldPointer.Instance.OverlayPicker == handle)
                {
                    WorldPointer.Instance.OverlayPicker = null;
                }

                UnityEngine.Object.Destroy(point);
            }

            point = null;
            tracker = null;
            computedPolygonIndex = cstypes.NONE;
        }

        // Dragged around in the plane facing the camera, at the depth (from the camera) it was at as the drag began
        private class ReferencePointHandle : MonoBehaviour, IWorldPointerHandler, IScreenPointerPicker
        {
            private float dragDepth;
            private Vector3 dragOffset;

            public bool TryPick(Vector2 screenPosition, out IWorldPointerHandler handler, out Vector3 worldPosition)
            {
                var viewCamera = ViewCamera;
                if (viewCamera && TryGetComponent<Collider>(out var cubeCollider) &&
                    cubeCollider.Raycast(viewCamera.ScreenPointToRay(screenPosition), out var hit, viewCamera.farClipPlane))
                {
                    handler = this;
                    worldPosition = hit.point;
                    return true;
                }

                handler = null;
                worldPosition = default;
                return false;
            }

            public void OnWorldPointerClick(WorldPointerEventData eventData)
            {
                // Intentionally blank
            }

            public void OnWorldPointerBeginDrag(WorldPointerEventData eventData)
            {
                var cameraTransform = ViewCamera.transform;
                dragDepth = Vector3.Dot(transform.position - cameraTransform.position, cameraTransform.forward);

                if (TryGetPlanePoint(eventData.PressPosition, out var pressPoint))
                {
                    dragOffset = transform.position - pressPoint;
                }
                else
                {
                    dragOffset = Vector3.zero;
                }
            }

            public void OnWorldPointerDrag(WorldPointerEventData eventData)
            {
                if (TryGetPlanePoint(eventData.Position, out var planePoint))
                {
                    transform.position = planePoint + dragOffset;
                }
            }

            public void OnWorldPointerEndDrag(WorldPointerEventData eventData)
            {
                OnWorldPointerDrag(eventData);
            }

            private static Camera ViewCamera
            {
                get
                {
                    var ui = ForgePlusUI.Instance;

                    return ui && ui.EditorCamera ? ui.EditorCamera.GetComponent<Camera>() : null;
                }
            }

            // Where the screen position's ray meets the plane facing the camera at the drag's depth (as the camera is now)
            private bool TryGetPlanePoint(Vector2 screenPosition, out Vector3 planePoint)
            {
                var viewCamera = ViewCamera;
                var cameraTransform = viewCamera.transform;
                var plane = new Plane(cameraTransform.forward, cameraTransform.position + cameraTransform.forward * dragDepth);
                var ray = viewCamera.ScreenPointToRay(screenPosition);

                if (plane.Raycast(ray, out var distance))
                {
                    planePoint = ray.GetPoint(distance);
                    return true;
                }

                planePoint = default;
                return false;
            }
        }
    }
}
