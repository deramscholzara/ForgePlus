using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;
using AlephOne;
using ForgePlus.LevelManipulation.Utilities;

namespace RuntimeCore.Entities.Geometry
{
    [AutoStaticsCleanup]
    public partial class RuntimeSurfaceGeometry : MonoBehaviour
    {
        // Counts every change to any surface's shape or placement (including platforms moving them), so what's drawn from
        // where surfaces are can tell when to look again without reading them all
        public static int GeometryVersion { get; private set; }

        public static void MarkGeometryChanged()
        {
            GeometryVersion++;
        }

        public Mesh SurfaceMesh => geometryModule.SurfaceMesh;
        
        public MeshRenderer SurfaceRenderer => geometryModule.SurfaceRenderer;

        protected RuntimeSurfaceGeometryModule_Base geometryModule;

        // The media whose height it follows (a media surface's)
        private LevelEntity_Media subscribedMedia;

        public virtual void InitializeRuntimeSurface(
            LevelEntity_Polygon entity,
            LevelEntity_Polygon.DataSources dataSource)
        {
            if (geometryModule != null)
            {
                throw new Exception("Cannot initialize surface more than once.");
            }

            var mesh = CreateMesh();

            geometryModule = new RuntimeSurfaceGeometryModule_Polygon(
                entity,
                dataSource,
                mesh,
                CreateRenderer());

            kind = dataSource == LevelEntity_Polygon.DataSources.Media ? Kind.Other : Kind.Polygon;

            AssembleSurface();
        }

        public virtual void InitializeRuntimeSurface(
            LevelEntity_Side entity,
            LevelEntity_Side.DataSources dataSource,
            LevelEntity_Side.Sections section)
        {
            if (geometryModule != null)
            {
                throw new Exception("Cannot initialize surface more than once.");
            }

            var mesh = CreateMesh();

            geometryModule = new RuntimeSurfaceGeometryModule_Side(
                entity,
                dataSource,
                section,
                mesh,
                CreateRenderer());

            kind = Kind.Side;

            AssembleSurface();
        }

        // Surfaces are lit by the level's own light-intensity texture rather than by Unity's lighting,
        // so Unity's per-renderer lighting features would only add culling and draw costs
        public static void ConfigureRenderer(MeshRenderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        public void PrepareForDestruction()
        {
            SubscribeToMedia(null);

            geometryModule.PrepareForDestruction();
        }

        public void ApplyPositions()
        {
            geometryModule.ApplyPositionsAndTriangles();
            geometryModule.ApplySurfaceCacheAdmission();
            MarkGeometryChanged();
        }

        public void ApplyPlatform()
        {
            geometryModule.ApplyPositionsAndTriangles();
            geometryModule.ApplySurfaceCacheAdmission();
            MarkGeometryChanged();
        }

        // For a floor's, ceiling's or media's corners that moved: its positions, the texture coordinates that follow them,
        // and its collider
        public void ApplyShape()
        {
            geometryModule.ApplyPositionsAndTriangles();
            geometryModule.ApplyTextureOffset(innerLayer: true);
            geometryModule.ApplySurfaceCacheAdmission();
            MarkGeometryChanged();

            if (!TryGetComponent<MeshCollider>(out var meshCollider) && HasArea(SurfaceMesh))
            {
                gameObject.AddComponent<MeshCollider>();
            }
            else if (meshCollider)
            {
                // The collider only takes a mesh's new shape as it's assigned, and PhysX can't make one with no area (as a
                // polygon whose corners are in a line has, while it's being moved)
                meshCollider.sharedMesh = null;
                if (HasArea(SurfaceMesh))
                {
                    meshCollider.sharedMesh = SurfaceMesh;
                }
            }
        }
        private static readonly List<Vector3> hasAreaVertices = new List<Vector3>();
        private static readonly List<int> hasAreaTriangles = new List<int>();

        // Whether any of the mesh's triangles has area
        public static bool HasArea(Mesh mesh)
        {
            // Read into reused lists, as this runs as surfaces are reshaped (each frame they're dragged)
            var vertices = hasAreaVertices;
            var triangles = hasAreaTriangles;
            mesh.GetVertices(vertices);
            mesh.GetTriangles(triangles, 0);

            for (var i = 0; i + 2 < triangles.Count; i += 3)
            {
                var a = vertices[triangles[i]];
                if (Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a).sqrMagnitude > 1e-10f)
                {
                    return true;
                }
            }

            return false;
        }

        // For a floor's or ceiling's height
        public void ApplyHeight()
        {
            geometryModule.ApplyTransformPosition();
            MarkGeometryChanged();
        }

        public void ApplyTextureOffset(bool innerLayer = true)
        {
            geometryModule.ApplyTextureOffset(innerLayer);
        }

        public void ApplyTransferMode(bool innerLayer = true)
        {
            geometryModule.ApplyTransferMode(innerLayer);
            geometryModule.ApplyRendererMaterials();
        }

        public void ApplyTexture(bool innerLayer = true)
        {
            geometryModule.ApplySourceMaterial(innerLayer);
            geometryModule.ApplyRendererMaterials();
        }

        public void ApplyLight(bool innerLayer = true)
        {
            geometryModule.ApplyLight(innerLayer);
            geometryModule.ApplyRendererMaterials();
        }

        public void ApplyAmbientDelta()
        {
            if (geometryModule is RuntimeSurfaceGeometryModule_Side sideModule)
            {
                sideModule.ApplyAmbientDelta();
            }
        }

        public void ApplyMedia()
        {
            if (geometryModule is RuntimeSurfaceGeometryModule_Polygon &&
                geometryModule.MaterialKey.SourceMedia != null)
            {
                geometryModule.ApplyMedia();

                if (geometryModule.MaterialKey.SourceMedia == null)
                {
                    PrepareForDestruction();
                    Destroy(gameObject);
                    return;
                }

                // Its media (or its media's type) may have changed its texture
                geometryModule.ApplySourceMaterial(innerLayer: true);
                geometryModule.ApplyRendererMaterials();

                SubscribeToMedia(geometryModule.MaterialKey.SourceMedia);
            }
        }

        // Destroyed with the surface (DestroysMeshOnDestroy)
        private Mesh CreateMesh()
        {
            var mesh = new Mesh();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<DestroysMeshOnDestroy>().Mesh = mesh;

            return mesh;
        }

        private MeshRenderer CreateRenderer()
        {
            var surfaceRenderer = gameObject.AddComponent<MeshRenderer>();
            ConfigureRenderer(surfaceRenderer);

            return surfaceRenderer;
        }

        // Which of the visualization options (Polygons, Sides) shows and hides it (a media surface is Media's)
        private enum Kind
        {
            Other,
            Polygon,
            Side,
        }

        private Kind kind;

        // Clicks pass through what's hidden (the pointer doesn't raycast this layer)
        private const int HiddenLayer = 2;
        private const int ShownLayer = 0;

        private static bool polygonsAreVisible = true;
        private static bool sidesAreVisible = true;

        // Every surface (floors', ceilings', sides' and media's), for showing and hiding them
        // Not readonly, so AutoStaticsCleanup resets it each Play session
        private static HashSet<RuntimeSurfaceGeometry> surfaces = new HashSet<RuntimeSurfaceGeometry>();

        // The polygon it's drawn as part of: a floor's, ceiling's or media's own, or the one a side faces into
        public short OwnerPolygonIndex
        {
            get
            {
                return geometryModule != null ? geometryModule.OwnerPolygonIndex : cstypes.NONE;
            }
        }

        // Floors and ceilings (Polygons) and sides (Sides) that are hidden aren't drawn, and clicks pass through them,
        // though they're still there
        public static void SetKindVisibility(bool polygonsVisible, bool sidesVisible)
        {
            if (polygonsVisible == polygonsAreVisible && sidesVisible == sidesAreVisible)
            {
                return;
            }

            polygonsAreVisible = polygonsVisible;
            sidesAreVisible = sidesVisible;

            ApplyVisibilityToAll();
        }

        // After what's shown changes for every surface (such as what PolygonVisibilityCulling hides)
        public static void ApplyVisibilityToAll()
        {
            surfaces.RemoveWhere(isDestroyed);
            foreach (var surface in surfaces)
            {
                surface.ApplyVisibility();
            }
        }

        // Only those of the polygons whose culling just changed (PolygonVisibilityCulling)
        public static void ApplyVisibilityToSurfacesIn(bool[] polygonChanged)
        {
            surfaces.RemoveWhere(isDestroyed);
            foreach (var surface in surfaces)
            {
                var polygonIndex = surface.OwnerPolygonIndex;
                if (polygonIndex >= 0 && polygonIndex < polygonChanged.Length && polygonChanged[polygonIndex])
                {
                    surface.ApplyVisibility();
                }
            }
        }

        private static readonly Predicate<RuntimeSurfaceGeometry> isDestroyed = surface => !surface;

        // While platforms leave none of a side to draw (RuntimeSurfaceGeometryModule_Side)
        private bool isClippedAway;

        public void SetClippedAway(bool clippedAway)
        {
            if (clippedAway == isClippedAway)
            {
                return;
            }

            isClippedAway = clippedAway;
            ApplyVisibility();
        }

        // Shown while its kind is (a media surface's is Media's), its polygon isn't culled (PolygonVisibilityCulling), and
        // platforms leave some of it to draw. Clicks pass through what's hidden by kind or culling, except media hidden by
        // its kind (whose clicks SelectionManager decides). Each is only written where it differs, as Surface Cache Global
        // Illumination and the GPU Resident Drawer update every renderer that's changed.
        public void ApplyVisibility()
        {
            var isCulled = PolygonVisibilityCulling.IsCulled(OwnerPolygonIndex);
            bool isKindShown;
            switch (kind)
            {
                case Kind.Polygon:
                    isKindShown = polygonsAreVisible;
                    break;
                case Kind.Side:
                    isKindShown = sidesAreVisible;
                    break;
                default:
                    isKindShown = LevelEntity_Media.SurfacesAreVisible;
                    break;
            }

            var surfaceRenderer = SurfaceRenderer;
            if (surfaceRenderer)
            {
                var isHidden = !isKindShown || isCulled || isClippedAway;
                if (surfaceRenderer.forceRenderingOff != isHidden)
                {
                    surfaceRenderer.forceRenderingOff = isHidden;
                }
            }

            var isClickable = kind == Kind.Other ? !isCulled : isKindShown && !isCulled;
            var layer = isClickable ? ShownLayer : HiddenLayer;
            if (gameObject.layer != layer)
            {
                gameObject.layer = layer;
            }
        }

        private void OnDestroy()
        {
            surfaces.Remove(this);
        }

        private void AssembleSurface()
        {
            geometryModule.AssembleSurface();

            surfaces.Add(this);
            ApplyVisibility();

            // A media's surfaces follow its height
            SubscribeToMedia(geometryModule.MaterialKey.SourceMedia);

            // If editing isn't possible, then the surface should never need to be updated or rebuilt
#if NO_EDITING
            Destroy(this);
#endif
        }

        private void SubscribeToMedia(LevelEntity_Media media)
        {
            if (media == subscribedMedia)
            {
                return;
            }

            subscribedMedia?.UnsubscribeSurface(transform);
            subscribedMedia = media;
            subscribedMedia?.SubscribeSurface(transform);
        }    }
}
