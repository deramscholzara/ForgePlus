using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.History;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = UnityEngine.Rect;

namespace ForgePlus.UI
{
    // Selecting by dragging a rectangle over the level (WorldPointer starts it, from anywhere not already selected, with the
    // Select tool), drawn as a faint outline. As it's dragged (shown live, and recorded as one change once it's let go,
    // or put back as it was if it's cancelled), what the mode selects that's wholly in it is selected (a polygon's whole
    // floor or ceiling, a line's both ends, a point anywhere up its vertical axis, and an object's or annotation's
    // center), or while ctrl is held, what it touches any of: in Geometry mode, points and lines (as drawn, while shown)
    // and polygons; in Heights mode, polygons; in Objects mode, objects; in Sounds mode, sound sources; and in
    // Annotations mode, annotations. In perspective, only what's within the distance points and lines fade out at counts
    // (looking straight down, anything in view does). Holding shift adds what it covers to the selection; otherwise, it's
    // selected instead.
    public class RectangleSelection
    {
        // Pixels
        private const float OutlineWidth = 2f;

        // For annotations, whose labels are drawn around their places
        private const float AnnotationMargin = 8f;

        private static readonly Color OutlineColor = new Color(1f, 1f, 1f, 0.15f);

        private readonly VisualElement layer;
        private readonly VisualElement box;

        private Vector2 startScreenPosition;

        // The selection before the drag (which shift adds to)
        private readonly List<ISelectable> previousSelection = new List<ISelectable>();

        public RectangleSelection(VisualElement root)
        {
            layer = WorldLayer.Create(root, "selection-rectangle");

            box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.style.position = Position.Absolute;
            box.style.borderLeftWidth = OutlineWidth;
            box.style.borderRightWidth = OutlineWidth;
            box.style.borderTopWidth = OutlineWidth;
            box.style.borderBottomWidth = OutlineWidth;
            box.style.borderLeftColor = OutlineColor;
            box.style.borderRightColor = OutlineColor;
            box.style.borderTopColor = OutlineColor;
            box.style.borderBottomColor = OutlineColor;
            box.style.display = DisplayStyle.None;
            layer.Add(box);
        }

        // In the Select tool, in the modes it selects in
        public static bool IsAvailable
        {
            get
            {
                if (ModeManager.Instance.SecondaryMode != ModeManager.SecondaryModes.Selection)
                {
                    return false;
                }

                switch (ModeManager.Instance.PrimaryMode)
                {
                    case ModeManager.PrimaryModes.Geometry:
                    case ModeManager.PrimaryModes.Heights:
                    case ModeManager.PrimaryModes.Objects:
                    case ModeManager.PrimaryModes.Sounds:
                    case ModeManager.PrimaryModes.Annotations:
                        return true;
                    default:
                        return false;
                }
            }
        }

        // Screen positions are in pixels, from the bottom left
        public void Begin(Vector2 screenPosition)
        {
            startScreenPosition = screenPosition;

            previousSelection.Clear();
            previousSelection.AddRange(SelectionManager.Instance.Selection);
            SelectionHistory.IsPaused = true;

            box.style.display = DisplayStyle.Flex;
        }

        // Add to Selection (Shift, held now) adds to the selection as it was before the drag, rather than replacing it
        public void Drag(Vector2 screenPosition, bool addsToSelection)
        {
            var panelRectangle = PanelRectangle(screenPosition);
            if (panelRectangle != null)
            {
                box.style.left = panelRectangle.Value.xMin;
                box.style.top = panelRectangle.Value.yMin;
                box.style.width = panelRectangle.Value.width;
                box.style.height = panelRectangle.Value.height;
            }

            // Select Touching (Ctrl, held now) selects what it touches any of, rather than only what's wholly in it
            var touches = Hotkeys.IsPressed(ForgePlusInput.Editing.SelectTouching);

            var hits = new List<ISelectable>();
            Collect(ScreenRectangle(screenPosition), panelRectangle, touches, hits);

            var selection = addsToSelection ? previousSelection.Concat(hits).Distinct().ToList() : hits;
            ApplySelection(selection);
        }

        public void End(Vector2 screenPosition, bool addsToSelection)
        {
            Drag(screenPosition, addsToSelection);
            Finish();
        }

        // The selection is put back as it was
        public void Cancel()
        {
            ApplySelection(previousSelection);
            Finish();
        }

        private void Finish()
        {
            box.style.display = DisplayStyle.None;
            previousSelection.Clear();
            SelectionHistory.IsPaused = false;
        }

        // Only what changes is selected or deselected
        private static void ApplySelection(List<ISelectable> selection)
        {
            var selectionManager = SelectionManager.Instance;
            var wanted = new HashSet<ISelectable>(selection);

            foreach (var selected in selectionManager.Selection.Where(selected => !wanted.Contains(selected)).ToList())
            {
                selectionManager.DeselectObject(selected, multiSelect: true);
            }

            foreach (var selectable in selection)
            {
                if (!selectionManager.GetIsSelected(selectable))
                {
                    selectionManager.SelectObject(selectable, multiSelect: true);
                }
            }
        }

        private Rect ScreenRectangle(Vector2 screenPosition)
        {
            return Rect.MinMaxRect(Mathf.Min(startScreenPosition.x, screenPosition.x), Mathf.Min(startScreenPosition.y, screenPosition.y),
                                   Mathf.Max(startScreenPosition.x, screenPosition.x), Mathf.Max(startScreenPosition.y, screenPosition.y));
        }

        private Rect? PanelRectangle(Vector2 screenPosition)
        {
            var panel = layer.panel;
            if (panel == null)
            {
                return null;
            }

            var start = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(startScreenPosition.x, Screen.height - startScreenPosition.y));
            var end = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));

            return Rect.MinMaxRect(Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y), Mathf.Max(start.x, end.x), Mathf.Max(start.y, end.y));
        }

        private static void Collect(Rect screenRectangle, Rect? panelRectangle, bool touches, List<ISelectable> hits)
        {
            var level = LevelEntity_Level.Instance;
            var camera = Camera.main;
            if (!level || !camera)
            {
                return;
            }

            var editorCamera = ForgePlusUI.Instance.EditorCamera;
            var limitsDistance = !(editorCamera && editorCamera.IsOrthographic);
            var found = new HashSet<ISelectable>();

            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    if (panelRectangle != null)
                    {
                        ForgePlusUI.Instance.PointHandles.CollectInRectangle(panelRectangle.Value, requiresWhole: !touches, found);
                    }

                    CollectPolygons(level, camera, screenRectangle, limitsDistance, touches, found);
                    break;
                case ModeManager.PrimaryModes.Heights:
                    CollectPolygons(level, camera, screenRectangle, limitsDistance, touches, found);
                    break;
                case ModeManager.PrimaryModes.Objects:
                case ModeManager.PrimaryModes.Sounds:
                    var soundSourcesOnly = ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Sounds;
                    foreach (var mapObject in level.MapObjects.Values)
                    {
                        if (mapObject && (!soundSourcesOnly || mapObject.NativeObject.type == map._saved_sound_source) &&
                            (touches ?
                                BoundsCover(camera, mapObject, screenRectangle, limitsDistance) :
                                PointCovers(camera, ObjectCenter(mapObject), 0f, screenRectangle, limitsDistance)))
                        {
                            found.Add(mapObject);
                        }
                    }

                    break;
                case ModeManager.PrimaryModes.Annotations:
                    foreach (var annotation in level.Annotations.Values)
                    {
                        if (annotation && PointCovers(camera, annotation.transform.position, touches ? AnnotationMargin : 0f, screenRectangle, limitsDistance))
                        {
                            found.Add(annotation);
                        }
                    }

                    break;
            }

            hits.AddRange(found);
        }

        // A polygon's floor or ceiling any of which (within the distance) is in the rectangle: an edge crosses it, or the
        // rectangle's corners or middle look onto it. Or, unless it touches, all of which is: every corner, in front of the
        // camera (and within the distance)
        private static void CollectPolygons(LevelEntity_Level level, Camera camera, Rect screenRectangle, bool limitsDistance, bool touches, HashSet<ISelectable> found)
        {
            var data = level.Level;
            var cameraPosition = camera.transform.position;
            var distance = PointHandles.FadeEndDistance;

            var rays = new[]
            {
                camera.ScreenPointToRay(new Vector2(screenRectangle.xMin, screenRectangle.yMin)),
                camera.ScreenPointToRay(new Vector2(screenRectangle.xMax, screenRectangle.yMin)),
                camera.ScreenPointToRay(new Vector2(screenRectangle.xMax, screenRectangle.yMax)),
                camera.ScreenPointToRay(new Vector2(screenRectangle.xMin, screenRectangle.yMax)),
                camera.ScreenPointToRay(screenRectangle.center),
            };

            foreach (var polygon in level.Polygons.Values)
            {
                if (!polygon)
                {
                    continue;
                }

                var nativePolygon = polygon.NativeObject;
                var corners = new Vector3[nativePolygon.vertex_count];
                for (var i = 0; i < corners.Length; i++)
                {
                    corners[i] = GeometryUtilities.GetMeshVertex(data, nativePolygon.endpoint_indexes[i]);
                }

                foreach (var height in new[] { SurfaceHeight(polygon, isFloor: true), SurfaceHeight(polygon, isFloor: false) })
                {
                    var isInside = touches ?
                                   FaceCovers(level, polygon, corners, height, camera, cameraPosition, rays, screenRectangle, limitsDistance, distance) :
                                   FaceIsInside(corners, height, camera, cameraPosition, screenRectangle, limitsDistance, distance);

                    if (isInside)
                    {
                        found.Add(polygon);
                        break;
                    }
                }
            }
        }

        private static bool FaceCovers(LevelEntity_Level level, LevelEntity_Polygon polygon, Vector3[] corners, float height, Camera camera, Vector3 cameraPosition, Ray[] rays,
                                       Rect screenRectangle, bool limitsDistance, float distance)
        {
            for (var i = 0; i < corners.Length; i++)
            {
                var start = corners[i];
                var end = corners[(i + 1) % corners.Length];
                start.y = height;
                end.y = height;

                if (limitsDistance && !PointHandles.ClipToSphere(cameraPosition, distance, ref start, ref end))
                {
                    continue;
                }

                if (!PointHandles.ClipToFront(camera, ref start, ref end))
                {
                    continue;
                }

                if (PointHandles.SegmentIntersectsRectangle(camera.WorldToScreenPoint(start), camera.WorldToScreenPoint(end), screenRectangle))
                {
                    return true;
                }
            }

            foreach (var ray in rays)
            {
                if (Mathf.Abs(ray.direction.y) < 0.0001f)
                {
                    continue;
                }

                var along = (height - ray.origin.y) / ray.direction.y;
                if (along <= 0f)
                {
                    continue;
                }

                var hit = ray.origin + ray.direction * along;
                if (limitsDistance && Vector3.Distance(cameraPosition, hit) > distance)
                {
                    continue;
                }

                var location = new world_point2d((short) Mathf.Clamp(Mathf.RoundToInt(hit.x * GeometryUtilities.WorldUnitIncrementsPerMeter), short.MinValue, short.MaxValue),
                                                 (short) Mathf.Clamp(Mathf.RoundToInt(-hit.z * GeometryUtilities.WorldUnitIncrementsPerMeter), short.MinValue, short.MaxValue));
                if (map.point_in_polygon(level.Level, polygon.NativeIndex, location))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FaceIsInside(Vector3[] corners, float height, Camera camera, Vector3 cameraPosition, Rect screenRectangle, bool limitsDistance, float distance)
        {
            foreach (var corner in corners)
            {
                var position = new Vector3(corner.x, height, corner.z);
                if (limitsDistance && Vector3.Distance(cameraPosition, position) > distance)
                {
                    return false;
                }

                var screen = camera.WorldToScreenPoint(position);
                if (screen.z <= 0f || !screenRectangle.Contains(screen))
                {
                    return false;
                }
            }

            return corners.Length > 0;
        }

        // The middle of what's drawn of it (or its place, if nothing is)
        private static Vector3 ObjectCenter(LevelEntity_MapObject mapObject)
        {
            var renderers = mapObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return mapObject.transform.position;
            }

            var bounds = renderers[0].bounds;
            foreach (var objectRenderer in renderers)
            {
                bounds.Encapsulate(objectRenderer.bounds);
            }

            return bounds.center;
        }

        private static float SurfaceHeight(LevelEntity_Polygon polygon, bool isFloor)
        {
            Component surface = isFloor ? polygon.FloorSurface : polygon.CeilingSurface;
            if (surface)
            {
                return surface.transform.position.y;
            }

            return (isFloor ? polygon.NativeObject.floor_height : polygon.NativeObject.ceiling_height) / GeometryUtilities.WorldUnitIncrementsPerMeter;
        }

        // Its drawn extent on screen overlaps the rectangle (and it's near enough)
        private static bool BoundsCover(Camera camera, LevelEntity_MapObject mapObject, Rect screenRectangle, bool limitsDistance)
        {
            var renderers = mapObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return PointCovers(camera, mapObject.transform.position, AnnotationMargin, screenRectangle, limitsDistance);
            }

            var bounds = renderers[0].bounds;
            foreach (var objectRenderer in renderers)
            {
                bounds.Encapsulate(objectRenderer.bounds);
            }

            if (limitsDistance && Mathf.Sqrt(bounds.SqrDistance(camera.transform.position)) > PointHandles.FadeEndDistance)
            {
                return false;
            }

            var screenBounds = new Rect();
            var hasScreenBounds = false;
            var min = bounds.min;
            var max = bounds.max;

            for (var corner = 0; corner < 8; corner++)
            {
                var position = new Vector3((corner & 1) == 0 ? min.x : max.x, (corner & 2) == 0 ? min.y : max.y, (corner & 4) == 0 ? min.z : max.z);
                var screen = camera.WorldToScreenPoint(position);
                if (screen.z <= 0f)
                {
                    continue;
                }

                if (!hasScreenBounds)
                {
                    screenBounds = new Rect(screen.x, screen.y, 0f, 0f);
                    hasScreenBounds = true;
                }
                else
                {
                    screenBounds = Rect.MinMaxRect(Mathf.Min(screenBounds.xMin, screen.x), Mathf.Min(screenBounds.yMin, screen.y),
                                                   Mathf.Max(screenBounds.xMax, screen.x), Mathf.Max(screenBounds.yMax, screen.y));
                }
            }

            return hasScreenBounds && screenBounds.Overlaps(screenRectangle);
        }

        private static bool PointCovers(Camera camera, Vector3 position, float margin, Rect screenRectangle, bool limitsDistance)
        {
            if (limitsDistance && Vector3.Distance(camera.transform.position, position) > PointHandles.FadeEndDistance)
            {
                return false;
            }

            var screen = camera.WorldToScreenPoint(position);
            if (screen.z <= 0f)
            {
                return false;
            }

            return new Rect(screen.x - margin, screen.y - margin, margin * 2f, margin * 2f).Overlaps(screenRectangle);
        }
    }
}
