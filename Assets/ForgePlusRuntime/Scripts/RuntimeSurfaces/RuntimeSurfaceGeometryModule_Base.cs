using ForgePlus.ApplicationGeneral;
using RuntimeCore.Common;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    public abstract class RuntimeSurfaceGeometryModule_Base : IDestructionPreparable
    {
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

        protected abstract void ApplyInteractiveSurface();
    }
}
