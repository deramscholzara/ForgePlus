using ForgePlus.ApplicationGeneral;
using RuntimeCore.Common;
using RuntimeCore.Materials;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    public abstract class RuntimeSurfaceGeometryModule_Base : IDestructionPreparable
    {
        // UV4.x holds the side's ambient delta, from -1 (full dark) to +1 (full light), which offsets the light intensity
        // of every layer; yzw are unused. Surfaces without it (such as polygons) are treated as having no delta.
        public const int AmbientDeltaUVChannel = 4;

        // Which materials it's drawn with (SurfaceMaterials)
        public SurfaceMaterials.Key MaterialKey { get; protected set; }

        public Mesh SurfaceMesh { get; protected set; }

        public MeshRenderer SurfaceRenderer { get; protected set; }

        // The polygon it's drawn as part of (for culling it with that polygon)
        public abstract short OwnerPolygonIndex { get; }

        public abstract void AssembleSurface();

        public abstract void ApplyPositionsAndTriangles();

        public abstract void ApplyTransformPosition();

        public abstract void ApplyPlatform();

        public abstract void ApplyTextureOffset(bool innerLayer);

        // Its materials hold the transfer mode's look (a layered side's outer layer's material, its transparent transfer mode's)
        public abstract void ApplyTransferMode(bool innerLayer);

        public abstract void ApplyLight(bool innerLayer);

        public abstract void ApplyMedia();

        public abstract void ApplySourceMaterial(bool innerLayer);

        // The level's light with that index, if there is one
        protected static LevelEntity_Light GetLight(LevelEntity_Level level, short lightIndex)
        {
            return level.Lights.TryGetValue(lightIndex, out var light) ? light : null;
        }

        public void ApplyRendererMaterials()
        {
            SurfaceRenderer.sharedMaterials = SurfaceMaterials.Get(MaterialKey);

            ApplySurfaceCacheAdmission();
        }

        // Whether Surface Cache Global Illumination takes it in, after its materials or size change
        public void ApplySurfaceCacheAdmission()
        {
            SurfaceCacheAdmission.Admit(SurfaceRenderer, IsExcludedFromSurfaceCache());
        }

        // Placeholders (an unassigned texture, or a texture that's missing) give it nothing worth bouncing
        protected virtual bool IsExcludedFromSurfaceCache()
        {
            return MaterialGeneration_Geometry.IsPlaceholder(MaterialKey.SourceMaterial);
        }

        public abstract void PrepareForDestruction();

        protected abstract void ApplyInteractiveSurface();
    }
}
