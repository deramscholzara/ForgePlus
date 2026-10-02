using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
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

        // Its bitmap in its collection (-1 for none); one the shapes file doesn't have is shown as a grid
        [CreateProperty]
        public int PrimaryBitmap
        {
            get
            {
                return BitmapOf(Side.primary_texture.texture);
            }
            set
            {
                SetTexture(LevelEntity_Side.DataSources.Primary, WithBitmap(Side.primary_texture.texture, value));
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
                return HasData(Side.primary_texture) ? TransferModeChoices.Choice(Side.primary_transfer_mode) : "-";
            }
            set
            {
                if (TransferModeChoices.TryParse(value, out var transferMode))
                {
                    Edit(side => side.SetTransferMode(LevelEntity_Side.DataSources.Primary, transferMode));
                }
            }
        }

        [CreateProperty]
        public List<string> PrimaryTransferModeChoices
        {
            get { return TransferModeChoices.All; }
        }

        [CreateProperty]
        public List<string> PrimaryTransferModeUnavailableChoices
        {
            get { return TransferModeChoices.Unavailable; }
        }

        // A surface with no texture isn't drawn
        [CreateProperty]
        public bool IsPrimaryTransferModeEditable
        {
            get { return HasData(Side.primary_texture); }
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
        public int SecondaryBitmap
        {
            get
            {
                return BitmapOf(Side.secondary_texture.texture);
            }
            set
            {
                SetTexture(LevelEntity_Side.DataSources.Secondary, WithBitmap(Side.secondary_texture.texture, value));
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
                return HasData(Side.secondary_texture) ? TransferModeChoices.Choice(Side.secondary_transfer_mode) : "-";
            }
            set
            {
                if (TransferModeChoices.TryParse(value, out var transferMode))
                {
                    Edit(side => side.SetTransferMode(LevelEntity_Side.DataSources.Secondary, transferMode));
                }
            }
        }

        [CreateProperty]
        public List<string> SecondaryTransferModeChoices
        {
            get { return TransferModeChoices.All; }
        }

        [CreateProperty]
        public List<string> SecondaryTransferModeUnavailableChoices
        {
            get { return TransferModeChoices.Unavailable; }
        }

        // A surface with no texture isn't drawn
        [CreateProperty]
        public bool IsSecondaryTransferModeEditable
        {
            get { return HasData(Side.secondary_texture); }
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
        public int TransparentBitmap
        {
            get
            {
                return BitmapOf(Side.transparent_texture.texture);
            }
            set
            {
                SetTexture(LevelEntity_Side.DataSources.Transparent, WithBitmap(Side.transparent_texture.texture, value));
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
                return HasData(Side.transparent_texture) ? TransferModeChoices.Choice(Side.transparent_transfer_mode) : "-";
            }
            set
            {
                if (TransferModeChoices.TryParse(value, out var transferMode))
                {
                    Edit(side => side.SetTransferMode(LevelEntity_Side.DataSources.Transparent, transferMode));
                }
            }
        }

        [CreateProperty]
        public List<string> TransparentTransferModeChoices
        {
            get { return TransferModeChoices.All; }
        }

        [CreateProperty]
        public List<string> TransparentTransferModeUnavailableChoices
        {
            get { return TransferModeChoices.Unavailable; }
        }

        // A surface with no texture isn't drawn
        [CreateProperty]
        public bool IsTransparentTransferModeEditable
        {
            get { return HasData(Side.transparent_texture); }
        }

        [CreateProperty]
        public string TransparentLightIndex
        {
            get
            {
                return HasData(Side.transparent_texture) ? Side.transparent_lightsource_index.ToString() : "-";
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            MakeTextureChoosable(nameof(PrimaryTexture), () => Side.primary_texture.texture, texture => SetTexture(LevelEntity_Side.DataSources.Primary, texture));
            MakeTextureChoosable(nameof(SecondaryTexture), () => Side.secondary_texture.texture, texture => SetTexture(LevelEntity_Side.DataSources.Secondary, texture));
            MakeTextureChoosable(nameof(TransparentTexture), () => Side.transparent_texture.texture, texture => SetTexture(LevelEntity_Side.DataSources.Transparent, texture));
        }

        // As painting it from the texture palette does
        private void SetTexture(LevelEntity_Side.DataSources dataSource, ushort shapeDescriptor)
        {
            Edit(side => side.SetShapeDescriptor(dataSource, shapeDescriptor));
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
