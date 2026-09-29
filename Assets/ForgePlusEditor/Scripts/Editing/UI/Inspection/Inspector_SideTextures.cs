using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_SideTextures : Inspector_Base<LevelEntity_Side>
    {
        public Inspector_SideTextures(LevelEntity_Side side) : base(side)
        {
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

        [CreateProperty]
        public UnityEngine.Texture PrimaryTexture
        {
            get
            {
                return TextureOrPlaceholder(Side.primary_texture.texture);
            }
        }

        [CreateProperty]
        public string PrimaryOffset
        {
            get
            {
                return SurfaceOffset(Side.primary_texture);
            }
        }

        [CreateProperty]
        public string PrimaryTransferMode
        {
            get
            {
                return HasData(Side.primary_texture) ? Side.primary_transfer_mode.ToString() : "-";
            }
        }

        [CreateProperty]
        public string PrimaryLightIndex
        {
            get
            {
                return HasData(Side.primary_texture) ? Side.primary_lightsource_index.ToString() : "-";
            }
        }

        [CreateProperty]
        public UnityEngine.Texture SecondaryTexture
        {
            get
            {
                return TextureOrPlaceholder(Side.secondary_texture.texture);
            }
        }

        [CreateProperty]
        public string SecondaryOffset
        {
            get
            {
                return SurfaceOffset(Side.secondary_texture);
            }
        }

        [CreateProperty]
        public string SecondaryTransferMode
        {
            get
            {
                return HasData(Side.secondary_texture) ? Side.secondary_transfer_mode.ToString() : "-";
            }
        }

        [CreateProperty]
        public string SecondaryLightIndex
        {
            get
            {
                return HasData(Side.secondary_texture) ? Side.secondary_lightsource_index.ToString() : "-";
            }
        }

        [CreateProperty]
        public UnityEngine.Texture TransparentTexture
        {
            get
            {
                return TextureOrPlaceholder(Side.transparent_texture.texture);
            }
        }

        [CreateProperty]
        public string TransparentOffset
        {
            get
            {
                return SurfaceOffset(Side.transparent_texture);
            }
        }

        [CreateProperty]
        public string TransparentTransferMode
        {
            get
            {
                return HasData(Side.transparent_texture) ? Side.transparent_transfer_mode.ToString() : "-";
            }
        }

        [CreateProperty]
        public string TransparentLightIndex
        {
            get
            {
                return HasData(Side.transparent_texture) ? Side.transparent_lightsource_index.ToString() : "-";
            }
        }

        private static bool HasData(side_texture_definition surface)
        {
            return !surface.texture.IsEmptyShapeDescriptor();
        }

        private static string SurfaceOffset(side_texture_definition surface)
        {
            return HasData(surface) ? $"X: {surface.x0}\nY: {surface.y0}" : "X: -\nY: -";
        }
    }
}
