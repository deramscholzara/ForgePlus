using ForgePlus.ApplicationGeneral;
using RuntimeCore.Common;
using System.Collections.Generic;
using UnityEngine;
using AlephOne;

namespace RuntimeCore.Entities.Geometry
{
    public abstract class RuntimeSurfaceGeometryModule_Base : IDestructionPreparable
    {
        // UV3 holds the transfer mode's effects on drawing (vertex colors hold its texture motion): xy for the base layer,
        // zw for a layered side's outer layer; x is 1 for static, y is the texture's scale
        public static readonly Vector2 DefaultTransferModeEffects = new Vector2(0f, 1f);

        // UV4.x holds the side's ambient delta, from -1 (full dark) to +1 (full light), which offsets the light intensity
        // of every layer; yzw are unused. Surfaces without it (such as polygons) are treated as having no delta.
        public const int AmbientDeltaUVChannel = 4;

        protected int lastTextureIndex;
        protected int lastLightIndex;

        public SurfaceBatchingManager.BatchKey BatchKey { get; protected set; }

        public bool IsStaticBatchable { get; protected set; } = true;

        public Mesh SurfaceMesh { get; protected set; }

        public MeshRenderer SurfaceRenderer { get; protected set; }

        public RuntimeSurfaceGeometryModule_Base()
        {
            BatchKey = new SurfaceBatchingManager.BatchKey();
        }

        public abstract void AssembleSurface();

        public abstract void ApplyPositionsAndTriangles();

        public abstract void ApplyTransformPosition();

        public abstract void ApplyPlatform();

        public abstract void ApplyTextureOffset(bool innerLayer);

        public abstract void ApplyTransferMode(bool innerLayer);

        public abstract void ApplyLight(bool innerLayer);

        public abstract void ApplyMedia();

        public abstract void ApplyBatchKeyMaterial(bool innerLayer);

        public void ApplyRendererMaterials()
        {
            SurfaceRenderer.sharedMaterials = SurfaceBatchingManager.Instance.GetUniqueMaterials(BatchKey);
        }

        public abstract void PrepareForDestruction();

        public static Vector2 GetTransferModeEffects(short transferMode)
        {
            var effects = DefaultTransferModeEffects;

            switch (transferMode)
            {
                case map._xfer_static:
                    effects.x = 1f;
                    break;
                case map._xfer_2x:
                    effects.y = 2f;
                    break;
                case map._xfer_4x:
                    effects.y = 4f;
                    break;
            }

            return effects;
        }

        // Keeps the other layer's half (or its default)
        protected void ApplyTransferModeEffects(short transferMode, bool innerLayer)
        {
            var vertexCount = SurfaceMesh.vertexCount;

            var effects = new List<Vector4>(vertexCount);
            SurfaceMesh.GetUVs(3, effects);

            if (effects.Count != vertexCount)
            {
                effects.Clear();
                for (var i = 0; i < vertexCount; i++)
                {
                    effects.Add(new Vector4(DefaultTransferModeEffects.x, DefaultTransferModeEffects.y, DefaultTransferModeEffects.x, DefaultTransferModeEffects.y));
                }
            }

            var layerEffects = GetTransferModeEffects(transferMode);
            for (var i = 0; i < vertexCount; i++)
            {
                var vertexEffects = effects[i];

                if (innerLayer)
                {
                    vertexEffects.x = layerEffects.x;
                    vertexEffects.y = layerEffects.y;
                }
                else
                {
                    vertexEffects.z = layerEffects.x;
                    vertexEffects.w = layerEffects.y;
                }

                effects[i] = vertexEffects;
            }

            SurfaceMesh.SetUVs(3, effects);
        }

        protected abstract void ApplyInteractiveSurface();
    }
}
