using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    public class Inspector_PolygonTextures : Inspector_Base<LevelEntity_Polygon>
    {
        public Inspector_PolygonTextures(LevelEntity_Polygon polygon) : base(polygon)
        {
            Floor = new PolygonSurfaceView(this, LevelEntity_Polygon.DataSources.Floor);
            Ceiling = new PolygonSurfaceView(this, LevelEntity_Polygon.DataSources.Ceiling);
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Polygon Textures";
            }
        }

        private polygon_data Polygon
        {
            get
            {
                return Entity.NativeObject;
            }
        }

        [CreateProperty]
        public string Id
        {
            get
            {
                return Entity.NativeIndex.ToString();
            }
        }

        public PolygonSurfaceView Floor { get; }

        public PolygonSurfaceView Ceiling { get; }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Floor.MakeTextureChoosable(Root.Q(nameof(Floor)));
            Ceiling.MakeTextureChoosable(Root.Q(nameof(Ceiling)));
        }

        public class PolygonSurfaceView : SurfaceTextureView
        {
            private readonly Inspector_PolygonTextures inspector;
            private readonly LevelEntity_Polygon.DataSources dataSource;

            public PolygonSurfaceView(Inspector_PolygonTextures inspector, LevelEntity_Polygon.DataSources dataSource) : base(inspector)
            {
                this.inspector = inspector;
                this.dataSource = dataSource;
            }

            private bool IsFloor
            {
                get
                {
                    return dataSource == LevelEntity_Polygon.DataSources.Floor;
                }
            }

            protected override ushort ShapeDescriptor
            {
                get
                {
                    return IsFloor ? inspector.Polygon.floor_texture : inspector.Polygon.ceiling_texture;
                }
            }

            protected override short NativeTransferMode
            {
                get
                {
                    return IsFloor ? inspector.Polygon.floor_transfer_mode : inspector.Polygon.ceiling_transfer_mode;
                }
            }

            protected override short NativeOffsetX
            {
                get
                {
                    return IsFloor ? inspector.Polygon.floor_origin.x : inspector.Polygon.ceiling_origin.x;
                }
            }

            protected override short NativeOffsetY
            {
                get
                {
                    return IsFloor ? inspector.Polygon.floor_origin.y : inspector.Polygon.ceiling_origin.y;
                }
            }

            protected override short NativeLightIndex
            {
                get
                {
                    return IsFloor ? inspector.Polygon.floor_lightsource_index : inspector.Polygon.ceiling_lightsource_index;
                }
            }

            // A landscape isn't lit
            protected override bool IsLit
            {
                get
                {
                    return !ShapeDescriptor.UsesLandscapeCollection();
                }
            }

            protected override void SetShapeDescriptor(ushort shapeDescriptor)
            {
                inspector.Edit(polygon => polygon.SetShapeDescriptor(dataSource, shapeDescriptor));
            }

            protected override void SetOffset(short x, short y)
            {
                inspector.Edit(polygon => polygon.SetOffset(dataSource, x, y, rebatch: true));
            }

            protected override void SetTransferMode(short transferMode)
            {
                inspector.Edit(polygon => polygon.SetTransferMode(dataSource, transferMode));
            }

            protected override void SetLight(int lightIndex)
            {
                inspector.SetLight(lightIndex, (polygon, light) => polygon.SetLight(dataSource, light));
            }
        }
    }
}
