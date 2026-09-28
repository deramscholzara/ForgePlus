using UnityEngine;
using AlephOne;

namespace RuntimeCore.Entities.Geometry
{
    public partial class LevelEntity_Polygon : LevelEntity_GeometryBase
    {
        public enum DataSources
        {
            Ceiling,
            Floor,
            Media,
        }

        public RuntimeSurfaceGeometry CeilingSurface;
        public RuntimeSurfaceGeometry FloorSurface;
        public RuntimeSurfaceGeometry MediaSurface;

        public new polygon_data NativeObject => base.NativeObject as polygon_data;

        protected override void AssembleEntity()
        {
            base.AssembleEntity();

            var floorRoot = new GameObject($"Floor (polygon: {NativeIndex})");
            FloorSurface = floorRoot.AddComponent<RuntimeSurfaceGeometry>();
            FloorSurface.InitializeRuntimeSurface(this, DataSources.Floor);
            floorRoot.transform.SetParent(transform);

            var ceilingRoot = new GameObject($"Ceiling (polygon: {NativeIndex})");
            CeilingSurface = ceilingRoot.AddComponent<RuntimeSurfaceGeometry>();
            CeilingSurface.InitializeRuntimeSurface(this, DataSources.Ceiling);
            ceilingRoot.transform.SetParent(transform);

            if (NativeObject.media_index >= 0)
            {
                var mediaRoot = new GameObject($"Media (polygon: {NativeIndex})");
                MediaSurface = mediaRoot.AddComponent<RuntimeSurfaceGeometry>();
                MediaSurface.InitializeRuntimeSurface(this, DataSources.Media);
                mediaRoot.transform.SetParent(transform);
            }
        }
    }
}
