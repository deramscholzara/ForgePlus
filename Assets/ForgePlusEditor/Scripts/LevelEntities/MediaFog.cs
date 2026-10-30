using AlephOne;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgePlus.LevelManipulation
{
    // The Media Fog view option: fog filling the space under each polygon's media (from its floor up to its media's
    // height, or its ceiling where that's lower), seen from inside or outside it, and from above through the media's
    // surface. Each polygon's is a prism (as its polygon is convex), drawn by its inside faces (so it's drawn while the
    // camera is in it), whose shader (ForgePlus/MediaFog) finds how far each pixel's view ray passes through it, up to
    // what's drawn behind it. Each media type's fog has its own color and density (settings, which start as Aleph One's).
    // Display-only: nothing of it is kept in the level. Updated once a frame (ForgePlusUI), after what moves surfaces.
    [AutoStaticsCleanup]
    public static class MediaFog
    {
        // Each volume's planes, in the shader's buffer: its top, its bottom, then one per side (up to the most a polygon
        // can have), the rest filled with planes that cut nothing
        private const int PlanesPerVolume = 2 + map.MAXIMUM_VERTICES_PER_POLYGON;
        private static readonly Vector4 NoPlane = new Vector4(0f, 0f, 0f, 1f);

        // Thinner fog than this isn't drawn (meters)
        private const float MinimumThickness = 0.001f;

        // What each media type's submerged fade effect tints the view with (fades.cpp: fade_definitions' soft tints,
        // through fade_effect_definitions), in the order of the media types (water, lava, goo, sewage, Jjaro)
        public static readonly Color[] DefaultColors =
        {
            new Color(0f, 0f, 1f), // _fade_tint_blue
            new Color(1f, 16384f / 65535f, 0f), // _fade_tint_orange
            new Color(137f * 256f / 65535f, 0f, 137f * 256f / 65535f), // _fade_tint_green (purple)
            new Color(32768f / 65535f, 1f, 0f), // _fade_tint_gross
            new Color(32768f / 65535f, 1f, 0f), // _fade_tint_jjaro (as sewage's)
        };

        // Aleph One's below-liquid fog (OGL_Setup.cpp: FogData) is exponential, with a depth of 8 world units, for a
        // density of an eighth per world unit (OGL_Render.cpp: GL_FOG_DENSITY)
        public const float DefaultDensityPerWorldUnit = 1f / 8f;

        private static readonly int planesPropertyId = Shader.PropertyToID("_MediaFogPlanes");
        private static readonly int volumesPropertyId = Shader.PropertyToID("_MediaFogVolumes");
        private static readonly int inFrontOfSurfacesPropertyId = Shader.PropertyToID("_InFrontOfSurfaces");
        private static readonly int minimumLightPropertyId = Shader.PropertyToID("_GlobalMinimumLight");

        private static bool isEnabled = true;
        private static bool mediaIsShown = true;

        private static readonly Color[] colors = (Color[]) DefaultColors.Clone();
        private static readonly float[] densities = { DefaultDensityPerWorldUnit, DefaultDensityPerWorldUnit, DefaultDensityPerWorldUnit, DefaultDensityPerWorldUnit, DefaultDensityPerWorldUnit };

        // Drawn as one mesh (every volume's inside faces), twice: before media surfaces, for volumes the camera is above
        // the top of (whose surface is at or behind where the view enters them), and after, for those it's below the top
        // of (whose surface is where the view leaves them)
        private static GameObject volumesObject;
        private static Mesh mesh;
        private static MeshRenderer meshRenderer;
        private static Material behindSurfacesMaterial;
        private static Material inFrontOfSurfacesMaterial;
        private static GraphicsBuffer planesBuffer;
        private static GraphicsBuffer volumesBuffer;

        // Reused each frame
        private static readonly List<Vector4> planes = new List<Vector4>();
        private static readonly List<Vector4> volumes = new List<Vector4>();
        private static readonly List<Vector3> vertices = new List<Vector3>();
        private static readonly List<Vector2> volumeIndexes = new List<Vector2>();
        private static readonly List<int> triangles = new List<int>();
        private static readonly List<Vector3> builtVertices = new List<Vector3>();
        private static readonly List<Vector3> corners = new List<Vector3>();

        public static bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
            set
            {
                isEnabled = value;
            }
        }

        // Fog isn't drawn while media isn't (the Media visualization option)
        public static bool MediaIsShown
        {
            get
            {
                return mediaIsShown;
            }
            set
            {
                mediaIsShown = value;
            }
        }

        public static void SetAppearance(short mediaType, Color color, float densityPerWorldUnit)
        {
            if (mediaType >= 0 && mediaType < colors.Length)
            {
                colors[mediaType] = color;
                densities[mediaType] = Mathf.Max(0f, densityPerWorldUnit);
            }
        }

        public static void Update()
        {
            var level = LevelEntity_Level.Instance;
            if (!isEnabled || !mediaIsShown || !level || level.Level == null || level.Medias == null || level.Medias.Count == 0)
            {
                Hide();
                return;
            }

            GatherVolumes(level);

            if (volumes.Count == 0)
            {
                Hide();
                return;
            }

            EnsureCreated();

            // The mesh only changes as volumes' shapes do (most frames, nothing moves)
            if (!SameVertices())
            {
                mesh.Clear();
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, volumeIndexes);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();

                builtVertices.Clear();
                builtVertices.AddRange(vertices);
            }

            // Colors follow animating lights, so these are given every frame
            planesBuffer = EnsureBufferSize(planesBuffer, planes.Count);
            volumesBuffer = EnsureBufferSize(volumesBuffer, volumes.Count);
            planesBuffer.SetData(planes);
            volumesBuffer.SetData(volumes);
            Shader.SetGlobalBuffer(planesPropertyId, planesBuffer);
            Shader.SetGlobalBuffer(volumesPropertyId, volumesBuffer);

            if (meshRenderer.forceRenderingOff)
            {
                meshRenderer.forceRenderingOff = false;
            }
        }

        // Each polygon with media that leaves some space between its floor and the media's top (its height, or the
        // polygon's ceiling where that's lower), and that isn't culled (Culling Simulation)
        private static void GatherVolumes(LevelEntity_Level level)
        {
            planes.Clear();
            volumes.Clear();
            vertices.Clear();
            volumeIndexes.Clear();
            triangles.Clear();

            var minimumLight = Shader.GetGlobalFloat(minimumLightPropertyId);
            var polygons = level.Level.PolygonList;

            for (short polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++)
            {
                var polygon = polygons[polygonIndex];
                if (polygon.media_index < 0 ||
                    polygon.vertex_count < 3 ||
                    !level.Medias.TryGetValue(polygon.media_index, out var media) ||
                    PolygonVisibilityCulling.IsCulled(polygonIndex))
                {
                    continue;
                }

                var type = media.NativeObject.type;
                if (type < 0 || type >= colors.Length || densities[type] <= 0f)
                {
                    continue;
                }

                var heights = level.LiveHeights(polygonIndex);
                var bottom = heights.x / GeometryUtilities.WorldUnitIncrementsPerMeter;
                var top = Mathf.Min(media.CurrentHeight, heights.y / GeometryUtilities.WorldUnitIncrementsPerMeter);
                if (top - bottom < MinimumThickness)
                {
                    continue;
                }

                corners.Clear();
                var center = Vector3.zero;
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    var corner = GeometryUtilities.GetMeshVertex(level.Level, polygon.endpoint_indexes[i]);
                    corners.Add(corner);
                    center += corner;
                }

                center /= corners.Count;

                AddVolume(corners, center, bottom, top);

                // Lit as the media's surface is (by its polygon's media light), from the minimum light up (or at full
                // brightness while lighting isn't shown, as the minimum is then)
                var intensity = level.Lights != null && level.Lights.TryGetValue(polygon.media_lightsource_index, out var light) && light != null ?
                                light.CurrentDisplayIntensity :
                                1f;
                var color = colors[type] * Mathf.Lerp(minimumLight, 1f, Mathf.Clamp01(intensity));

                // Density per meter (a world unit is two)
                volumes.Add(new Vector4(color.r, color.g, color.b, densities[type] / GeometryUtilities.MeterToWorldUnit));
            }
        }

        private static void AddVolume(List<Vector3> polygonCorners, Vector3 center, float bottom, float top)
        {
            var volumeIndex = volumes.Count;
            var firstPlane = planes.Count;

            // Inside where dot(normal, point) <= distance
            planes.Add(new Vector4(0f, 1f, 0f, top));
            planes.Add(new Vector4(0f, -1f, 0f, -bottom));

            var count = polygonCorners.Count;
            for (var i = 0; i < count; i++)
            {
                var a = polygonCorners[i];
                var b = polygonCorners[(i + 1) % count];

                var normal = Vector3.Cross(b - a, Vector3.up);
                if (normal.sqrMagnitude < 1e-12f)
                {
                    // Corners at the same place make no side
                    continue;
                }

                normal.Normalize();
                if (Vector3.Dot(normal, center - a) > 0f)
                {
                    normal = -normal;
                }

                planes.Add(new Vector4(normal.x, normal.y, normal.z, Vector3.Dot(normal, a)));

                // The side, facing out
                AddQuad(new Vector3(a.x, bottom, a.z), new Vector3(b.x, bottom, b.z), new Vector3(b.x, top, b.z), new Vector3(a.x, top, a.z), normal, volumeIndex);
            }

            while (planes.Count < firstPlane + PlanesPerVolume)
            {
                planes.Add(NoPlane);
            }

            // The top and bottom, facing up and down (fanned out from the first corner, as the polygon is convex)
            for (var i = 1; i + 1 < count; i++)
            {
                AddTriangle(At(polygonCorners[0], top), At(polygonCorners[i], top), At(polygonCorners[i + 1], top), Vector3.up, volumeIndex);
                AddTriangle(At(polygonCorners[0], bottom), At(polygonCorners[i], bottom), At(polygonCorners[i + 1], bottom), Vector3.down, volumeIndex);
            }
        }

        private static Vector3 At(Vector3 corner, float height)
        {
            return new Vector3(corner.x, height, corner.z);
        }

        private static void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, int volumeIndex)
        {
            AddTriangle(a, b, c, outward, volumeIndex);
            AddTriangle(a, c, d, outward, volumeIndex);
        }

        // Wound to face outward (so the shader, culling front faces, draws the inside)
        private static void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, int volumeIndex)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
            {
                (b, c) = (c, b);
            }

            var first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);

            var index = new Vector2(volumeIndex, 0f);
            volumeIndexes.Add(index);
            volumeIndexes.Add(index);
            volumeIndexes.Add(index);

            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
        }

        private static bool SameVertices()
        {
            if (builtVertices.Count != vertices.Count)
            {
                return false;
            }

            for (var i = 0; i < vertices.Count; i++)
            {
                if (builtVertices[i] != vertices[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static GraphicsBuffer EnsureBufferSize(GraphicsBuffer buffer, int count)
        {
            if (buffer != null && buffer.count >= count)
            {
                return buffer;
            }

            buffer?.Release();

            Application.quitting -= Release;
            Application.quitting += Release;

            // With room to grow, so volumes appearing one by one (such as media rising) don't each make a new one
            return new GraphicsBuffer(GraphicsBuffer.Target.Structured, Mathf.NextPowerOfTwo(Mathf.Max(count, 16)), sizeof(float) * 4);
        }

        private static void EnsureCreated()
        {
            if (volumesObject)
            {
                return;
            }

            var shader = Shader.Find("ForgePlus/MediaFog");

            behindSurfacesMaterial = new Material(shader) { name = "Media Fog (Behind Surfaces)" };
            behindSurfacesMaterial.SetFloat(inFrontOfSurfacesPropertyId, 0f);
            behindSurfacesMaterial.renderQueue = (int) RenderQueue.Transparent - 1;

            inFrontOfSurfacesMaterial = new Material(shader) { name = "Media Fog (In Front Of Surfaces)" };
            inFrontOfSurfacesMaterial.SetFloat(inFrontOfSurfacesPropertyId, 1f);
            inFrontOfSurfacesMaterial.renderQueue = (int) RenderQueue.Transparent + 1;

            mesh = new Mesh { name = "Media Fog Volumes", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();

            // Clicks pass through it (the pointer doesn't raycast this layer)
            volumesObject = new GameObject("Media Fog Volumes") { layer = 2 };
            volumesObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            volumesObject.AddComponent<DestroysMeshOnDestroy>().Mesh = mesh;

            // One submesh, drawn once by each material
            meshRenderer = volumesObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = new[] { behindSurfacesMaterial, inFrontOfSurfacesMaterial };
            RuntimeSurfaceGeometry.ConfigureRenderer(meshRenderer);
            SurfaceCacheRenderingLayers.Exclude(meshRenderer);
            GeometryUtilities.ExcludeFromGpuResidentDrawer(meshRenderer);

            builtVertices.Clear();
        }

        private static void Hide()
        {
            if (meshRenderer && !meshRenderer.forceRenderingOff)
            {
                meshRenderer.forceRenderingOff = true;
            }
        }

        // As Play mode (or ForgePlus) ends, before the buffers' references are cleared (AutoStaticsCleanup)
        private static void Release()
        {
            planesBuffer?.Release();
            planesBuffer = null;
            volumesBuffer?.Release();
            volumesBuffer = null;
        }
    }
}
