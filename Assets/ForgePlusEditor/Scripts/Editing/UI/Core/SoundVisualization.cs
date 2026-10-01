using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A volume bar's fill (from silent, 0, to full volume, MAXIMUM_SOUND_VOLUME)
    public static class VolumeBars
    {
        public static float Percent(float volume)
        {
            return Mathf.Clamp01(volume / SoundManagerEnums.MAXIMUM_SOUND_VOLUME) * 100f;
        }
    }

    // What the level's sounds look like in the level:
    // - volume bars (screen-space, behind the panels): each polygon's ambient and random sounds' side by side (empty for
    //   none, and a random sound's showing how much higher it can be), and each sound source's (following its light, if
    //   its volume does);
    // - direction arrows (on the floor at a polygon's center): where its random sound comes from, over the range it
    //   varies by;
    // - the radii of a selected sound source: where its sound is at full volume, and where it fades to silent (from its
    //   sound's behavior, quiet, normal or loud, which the sounds file says; without it, the 10 world units it's heard
    //   from at most).
    // Volumes and directions show everywhere while their display settings are on (always, in Sounds mode); otherwise
    // only for what's selected.
    public class SoundVisualization
    {
        private const float BarWidth = 8f;
        private const float BarHeight = 32f;
        private const float BarAnchorHeight = 0.5f;
        private const float ArrowLength = 0.75f;
        private const float LineWidth = 0.03f;
        private const float FloorOffset = 0.02f;
        private const int CircleSegments = 64;

        // Where an unobstructed sound is at full volume, and where it's silent, in world units (sound_definitions.h,
        // sound_behavior_definitions: quiet, normal and loud), and the farthest a sound source is heard from at all
        // (map_constructors.cpp, ZERO_VOLUME_DISTANCE)
        private static readonly float[] FullVolumeDistances = { 0f, 1f, 2f };
        private static readonly float[] SilentDistances = { 5f, 10f, 15f };
        private const float MaximumSourceDistance = 10f;

        private readonly VisualElement layer;

        private readonly Dictionary<short, VisualElement> polygonBars = new Dictionary<short, VisualElement>();
        private readonly Dictionary<short, VisualElement> sourceBars = new Dictionary<short, VisualElement>();

        private readonly Dictionary<short, (GameObject Arrow, short Direction, short Delta)> arrows = new Dictionary<short, (GameObject, short, short)>();
        private readonly Dictionary<short, (GameObject Radii, short Behavior)> radii = new Dictionary<short, (GameObject, short)>();

        private readonly HashSet<short> selectedPolygons = new HashSet<short>();
        private readonly HashSet<short> selectedSources = new HashSet<short>();

        private GameObject root;
        private Material material;
        private LevelEntity_Level shownLevel;

        public SoundVisualization(VisualElement uiRoot)
        {
            layer = new VisualElement { name = "sound-volumes", pickingMode = PickingMode.Ignore };
            layer.AddToClassList("fp-world-labels");

            // First, so every panel is drawn over it
            uiRoot.Insert(0, layer);
        }

        // After the camera has moved for the frame
        public void Update()
        {
            var level = LevelEntity_Level.Instance;
            if (level != shownLevel)
            {
                Clear();
                shownLevel = level;
            }

            if (!level)
            {
                return;
            }

            var settings = SettingsManager.Instance;
            var showsAllVolumes = settings.SoundDisplaysForcedOn || settings.SoundVolumeEnabled;
            var showsAllDirections = settings.SoundDisplaysForcedOn || settings.SoundDirectionEnabled;

            selectedPolygons.Clear();
            selectedSources.Clear();
            foreach (var selected in SelectionManager.Instance.Selection)
            {
                if (selected is LevelEntity_Polygon polygon)
                {
                    selectedPolygons.Add(polygon.NativeIndex);
                }
                else if (selected is LevelEntity_MapObject mapObject && mapObject.NativeObject.type == map._saved_sound_source)
                {
                    selectedSources.Add(mapObject.NativeIndex);
                }
            }

            var camera = Camera.main;
            var panel = layer.panel;

            foreach (var polygon in level.Polygons.Values)
            {
                var index = polygon.NativeIndex;
                var data = polygon.NativeObject;
                var anchor = PolygonAnchor(data, BarAnchorHeight);

                ShowPolygonBars(level.Level, index, data, (showsAllVolumes || selectedPolygons.Contains(index)) ? (Vector3?) anchor : null, camera, panel);
                ShowArrow(level.Level, index, data, showsAllDirections || selectedPolygons.Contains(index));
            }

            foreach (var mapObject in level.MapObjects.Values)
            {
                if (mapObject.NativeObject.type != map._saved_sound_source)
                {
                    continue;
                }

                var index = mapObject.NativeIndex;
                ShowSourceBar(level, mapObject, showsAllVolumes || selectedSources.Contains(index), camera, panel);
                ShowRadii(mapObject, selectedSources.Contains(index));
            }
        }

        public void Clear()
        {
            layer.Clear();
            polygonBars.Clear();
            sourceBars.Clear();
            arrows.Clear();
            radii.Clear();

            if (root)
            {
                Object.Destroy(root);
            }

            root = null;
        }

        // ---------- Volume bars

        private void ShowPolygonBars(MapLevel level, short index, polygon_data polygon, Vector3? anchor, Camera camera, IPanel panel)
        {
            polygonBars.TryGetValue(index, out var bars);

            if (!anchor.HasValue || !TryGetPanelPosition(anchor.Value, camera, panel, out var position))
            {
                if (bars != null)
                {
                    bars.style.display = DisplayStyle.None;
                }

                return;
            }

            if (bars == null)
            {
                bars = new VisualElement { pickingMode = PickingMode.Ignore };
                bars.AddToClassList("fp-volume-bars");
                bars.Add(CreateBar());
                bars.Add(CreateBar());
                layer.Add(bars);
                polygonBars[index] = bars;
            }

            // Ambient, then random (each empty for none)
            var ambientIndex = polygon.ambient_sound_image_index;
            var ambientVolume = ambientIndex >= 0 && ambientIndex < level.AmbientSoundImageList.Count ? level.AmbientSoundImageList[ambientIndex].volume : 0;
            SetBar(bars[0], ambientVolume, ambientVolume);

            var randomIndex = polygon.random_sound_image_index;
            if (randomIndex >= 0 && randomIndex < level.RandomSoundImageList.Count)
            {
                var random = level.RandomSoundImageList[randomIndex];
                SetBar(bars[1], random.volume, random.delta_volume > 1 ? random.volume + random.delta_volume - 1 : random.volume);
            }
            else
            {
                SetBar(bars[1], 0, 0);
            }

            Place(bars, position);
        }

        private void ShowSourceBar(LevelEntity_Level level, LevelEntity_MapObject mapObject, bool isShown, Camera camera, IPanel panel)
        {
            var index = mapObject.NativeIndex;
            sourceBars.TryGetValue(index, out var bars);

            if (!isShown || !TryGetPanelPosition(mapObject.transform.position, camera, panel, out var position))
            {
                if (bars != null)
                {
                    bars.style.display = DisplayStyle.None;
                }

                return;
            }

            if (bars == null)
            {
                bars = new VisualElement { pickingMode = PickingMode.Ignore };
                bars.AddToClassList("fp-volume-bars");
                bars.Add(CreateBar());
                layer.Add(bars);
                sourceBars[index] = bars;
            }

            // Its volume, or the intensity of the light it follows (map.cpp: get_light_intensity(-volume) >> 8)
            var facing = mapObject.NativeObject.facing;
            float volume = facing;
            if (facing < 0)
            {
                volume = level.Lights.TryGetValue((short) -facing, out var light) ? light.CurrentLinearIntensity * SoundManagerEnums.MAXIMUM_SOUND_VOLUME : 0f;
            }

            SetBar(bars[0], volume, volume);
            Place(bars, position);
        }

        private static VisualElement CreateBar()
        {
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("fp-volume-bar");
            bar.style.width = BarWidth;
            bar.style.height = BarHeight;

            var range = new VisualElement { name = "range", pickingMode = PickingMode.Ignore };
            range.AddToClassList("fp-volume-bar__range");
            bar.Add(range);

            var fill = new VisualElement { name = "fill", pickingMode = PickingMode.Ignore };
            fill.AddToClassList("fp-volume-bar__fill");
            bar.Add(fill);

            return bar;
        }

        // Filled to the volume, with how much higher it can be (up to the maximum) above it
        private static void SetBar(VisualElement bar, float volume, float maximum)
        {
            bar.Q("fill").style.height = Length.Percent(VolumeBars.Percent(volume));
            bar.Q("range").style.height = Length.Percent(VolumeBars.Percent(maximum));
        }

        private static void Place(VisualElement bars, Vector2 position)
        {
            bars.style.left = position.x;
            bars.style.top = position.y;
            bars.style.display = DisplayStyle.Flex;
        }

        // Behind the camera, or outside its view, isn't shown
        private static bool TryGetPanelPosition(Vector3 worldPosition, Camera camera, IPanel panel, out Vector2 position)
        {
            position = default;

            if (!camera || panel == null)
            {
                return false;
            }

            var screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f || !camera.pixelRect.Contains(screenPosition))
            {
                return false;
            }

            position = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            return true;
        }

        // ---------- Direction arrows and radii

        private void ShowArrow(MapLevel level, short index, polygon_data polygon, bool isShown)
        {
            var randomIndex = polygon.random_sound_image_index;
            random_sound_image_data random = randomIndex >= 0 && randomIndex < level.RandomSoundImageList.Count ? level.RandomSoundImageList[randomIndex] : null;
            var isDirectional = random != null && !csmacros.TEST_FLAG(random.flags, map._sound_image_is_non_directional);

            arrows.TryGetValue(index, out var arrow);

            if (!isShown || !isDirectional)
            {
                if (arrow.Arrow)
                {
                    arrow.Arrow.SetActive(false);
                }

                return;
            }

            if (!arrow.Arrow || arrow.Direction != random.direction || arrow.Delta != random.delta_direction)
            {
                if (!arrow.Arrow)
                {
                    arrow.Arrow = CreateIndicator($"Sound Direction - Polygon {index}");
                }

                SetMesh(arrow.Arrow, BuildArrowMesh(random.direction, random.delta_direction));
                arrow = (arrow.Arrow, random.direction, random.delta_direction);
                arrows[index] = arrow;
            }

            arrow.Arrow.transform.position = PolygonAnchor(polygon, FloorOffset);
            arrow.Arrow.SetActive(true);
        }

        private void ShowRadii(LevelEntity_MapObject mapObject, bool isShown)
        {
            var index = mapObject.NativeIndex;
            radii.TryGetValue(index, out var shown);

            if (!isShown)
            {
                if (shown.Radii)
                {
                    shown.Radii.SetActive(false);
                }

                return;
            }

            var behavior = SoundsLoading.Instance.AmbientSoundBehavior(mapObject.NativeObject.index);
            if (!shown.Radii || shown.Behavior != behavior)
            {
                if (!shown.Radii)
                {
                    shown.Radii = CreateIndicator($"Sound Radii - Object {index}");
                }

                SetMesh(shown.Radii, BuildRadiiMesh(behavior));
                shown = (shown.Radii, behavior);
                radii[index] = shown;
            }

            shown.Radii.transform.position = mapObject.transform.position;
            shown.Radii.SetActive(true);
        }

        // At the polygon's center, above its floor
        private static Vector3 PolygonAnchor(polygon_data polygon, float heightAboveFloor)
        {
            var center = polygon.center;

            return new Vector3(center.x, polygon.floor_height, -center.y) / GeometryUtilities.WorldUnitIncrementsPerMeter + Vector3.up * heightAboveFloor;
        }

        private GameObject CreateIndicator(string name)
        {
            if (!root)
            {
                root = new GameObject("Sound Visualization");
            }

            if (!material)
            {
                material = new Material(Shader.Find("ForgePlus/GeometrySelectionIndicator"));
            }

            var indicator = new GameObject(name);
            indicator.transform.SetParent(root.transform, worldPositionStays: false);
            indicator.layer = SelectionManager.SelectionIndicatorLayer;
            indicator.AddComponent<MeshFilter>();
            indicator.AddComponent<MeshRenderer>().sharedMaterial = material;

            return indicator;
        }

        private static void SetMesh(GameObject indicator, Mesh mesh)
        {
            var meshFilter = indicator.GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh)
            {
                Object.Destroy(meshFilter.sharedMesh);
            }

            meshFilter.sharedMesh = mesh;
        }

        // The direction a Marathon angle points (its x to Unity's x, and its y to Unity's -z)
        private static Vector3 AngleDirection(float angle)
        {
            var radians = angle * 2f * Mathf.PI / world.NUMBER_OF_ANGLES;

            return new Vector3(Mathf.Cos(radians), 0f, -Mathf.Sin(radians));
        }

        // An arrow pointing where the sound comes from (its direction), and, for a direction that varies, an arc over
        // the range it varies by (from its direction to the highest it reaches)
        private static Mesh BuildArrowMesh(short direction, short delta)
        {
            var builder = new LineMeshBuilder();
            var forward = AngleDirection(direction);
            var tip = forward * ArrowLength;
            var side = Vector3.Cross(Vector3.up, forward);

            builder.AddLine(Vector3.zero, tip);
            builder.AddLine(tip, tip - forward * 0.15f + side * 0.1f);
            builder.AddLine(tip, tip - forward * 0.15f - side * 0.1f);

            if (delta > 1)
            {
                var highest = direction + delta - 1;
                var segments = Mathf.Max(2, Mathf.CeilToInt((delta - 1) * CircleSegments / (float) world.NUMBER_OF_ANGLES));
                var radius = ArrowLength * 0.8f;

                builder.AddLine(Vector3.zero, AngleDirection(highest) * ArrowLength);
                for (var i = 0; i < segments; i++)
                {
                    var from = Mathf.Lerp(direction, highest, i / (float) segments);
                    var to = Mathf.Lerp(direction, highest, (i + 1) / (float) segments);
                    builder.AddLine(AngleDirection(from) * radius, AngleDirection(to) * radius);
                }
            }

            return builder.Build("Sound Direction");
        }

        // A circle where it's at full volume (if it's at full volume anywhere but at its center), and one where it's silent
        private static Mesh BuildRadiiMesh(short behavior)
        {
            var builder = new LineMeshBuilder();
            var isKnown = behavior >= 0 && behavior < SilentDistances.Length;

            var fullVolumeDistance = isKnown ? FullVolumeDistances[behavior] : 0f;
            var silentDistance = isKnown ? SilentDistances[behavior] : MaximumSourceDistance;

            if (fullVolumeDistance > 0f)
            {
                builder.AddCircle(fullVolumeDistance, CircleSegments);
            }

            builder.AddCircle(silentDistance, CircleSegments);

            return builder.Build("Sound Radii");
        }

        // Thin flat strips (on the XZ plane) for lines, as one mesh
        private class LineMeshBuilder
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<int> triangles = new List<int>();

            public void AddLine(Vector3 from, Vector3 to)
            {
                var along = to - from;
                if (along.sqrMagnitude < 1e-8f)
                {
                    return;
                }

                var side = Vector3.Cross(Vector3.up, along.normalized) * (LineWidth * 0.5f);
                var first = vertices.Count;

                vertices.Add(from - side);
                vertices.Add(from + side);
                vertices.Add(to + side);
                vertices.Add(to - side);

                // Both faces, so it shows from above and below
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
                triangles.AddRange(new[] { first, first + 2, first + 1, first, first + 3, first + 2 });
            }

            public void AddCircle(float radius, int segments)
            {
                for (var i = 0; i < segments; i++)
                {
                    var from = i * world.NUMBER_OF_ANGLES / (float) segments;
                    var to = (i + 1) * world.NUMBER_OF_ANGLES / (float) segments;
                    AddLine(AngleDirection(from) * radius, AngleDirection(to) * radius);
                }
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();

                return mesh;
            }
        }
    }
}
