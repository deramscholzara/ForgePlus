using ForgePlus.ApplicationGeneral;
using RuntimeCore.Common;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    public abstract class RuntimeSurfaceGeometryModule_Base : IDestructionPreparable
    {
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

        // UV3 holds the transfer mode's effects on how a surface is drawn (rather than how its texture moves, which the
        // vertex colors hold): xy for the base layer, zw for a layered transparent side's outer layer. x is 1 for static
        // (and 0 otherwise); y is the texture's scale (2 or 4, or 1 otherwise). Big landscapes are drawn with the landscape
        // material, like landscapes, so they need nothing here.
        public static readonly Vector2 DefaultTransferModeEffects = new Vector2(0f, 1f);

        public static Vector2 GetTransferModeEffects(short transferMode)
        {
            var effects = DefaultTransferModeEffects;

            switch (transferMode)
            {
                case AlephOne.map._xfer_static:
                    effects.x = 1f;
                    break;
                case AlephOne.map._xfer_2x:
                    effects.y = 2f;
                    break;
                case AlephOne.map._xfer_4x:
                    effects.y = 4f;
                    break;
            }

            return effects;
        }

        // Sets one layer's half of UV3, keeping the other layer's (or its default)
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
