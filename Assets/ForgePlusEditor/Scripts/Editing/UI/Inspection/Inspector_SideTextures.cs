using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using RuntimeCore.Entities.Geometry;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    public class Inspector_SideTextures : Inspector_Base<LevelEntity_Side>
    {
        public Inspector_SideTextures(LevelEntity_Side side) : base(side)
        {
            Primary = new SideSurfaceView(this, LevelEntity_Side.DataSources.Primary);
            Secondary = new SideSurfaceView(this, LevelEntity_Side.DataSources.Secondary);
            Transparent = new SideSurfaceView(this, LevelEntity_Side.DataSources.Transparent);
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Side Textures";
            }
        }

        private side_data Side
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

        [CreateProperty]
        public string Type
        {
            get
            {
                return AlephOneNames.SideType(Side.type);
            }
        }

        public SideSurfaceView Primary { get; }

        public SideSurfaceView Secondary { get; }

        public SideSurfaceView Transparent { get; }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Primary.MakeTextureChoosable(Root.Q(nameof(Primary)));
            Secondary.MakeTextureChoosable(Root.Q(nameof(Secondary)));
            Transparent.MakeTextureChoosable(Root.Q(nameof(Transparent)));
        }

        public class SideSurfaceView : SurfaceTextureView
        {
            private readonly Inspector_SideTextures inspector;
            private readonly LevelEntity_Side.DataSources dataSource;

            public SideSurfaceView(Inspector_SideTextures inspector, LevelEntity_Side.DataSources dataSource) : base(inspector)
            {
                this.inspector = inspector;
                this.dataSource = dataSource;
            }

            [CreateProperty]
            public bool IsBitmapEditable
            {
                get
                {
                    return IsTextureAssignable;
                }
            }

            [CreateProperty]
            public string TextureNote
            {
                get
                {
                    return IsTextureAssignable ? string.Empty : Strings.Get(Strings.Textures, "Inspector.SideTextures.PrimaryTextureNote");
                }
            }

            protected override ushort ShapeDescriptor
            {
                get
                {
                    return inspector.Side.GetTexture(dataSource).texture;
                }
            }

            protected override short NativeTransferMode
            {
                get
                {
                    return inspector.Side.GetTransferMode(dataSource);
                }
            }

            protected override short NativeOffsetX
            {
                get
                {
                    return inspector.Side.GetTexture(dataSource).x0;
                }
            }

            protected override short NativeOffsetY
            {
                get
                {
                    return inspector.Side.GetTexture(dataSource).y0;
                }
            }

            protected override short NativeLightIndex
            {
                get
                {
                    return inspector.Side.GetLightsourceIndex(dataSource);
                }
            }

            protected override bool IsLit
            {
                get
                {
                    return UsesLight(ShapeDescriptor);
                }
            }

            // A control panel's primary texture is set by its panel type and state (in Geometry mode), as the game sets it
            protected override bool IsTextureAssignable
            {
                get
                {
                    return dataSource != LevelEntity_Side.DataSources.Primary || !inspector.Entity.PanelSetsPrimaryTexture;
                }
            }

            protected override string TransferModeChoice
            {
                get
                {
                    return HasTexture ? base.TransferModeChoice : "-";
                }
            }

            protected override void SetShapeDescriptor(ushort shapeDescriptor)
            {
                inspector.Edit(side => side.SetShapeDescriptor(dataSource, shapeDescriptor));
            }

            protected override void SetOffset(short x, short y)
            {
                inspector.Edit(side => side.SetOffset(dataSource, x, y, rebatch: true));
            }

            protected override void SetTransferMode(short transferMode)
            {
                inspector.Edit(side => side.SetTransferMode(dataSource, transferMode));
            }

            protected override void SetLight(int lightIndex)
            {
                inspector.SetLight(lightIndex, (side, light) => side.SetLight(dataSource, light));
            }
        }
    }
}
