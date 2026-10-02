using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_PolygonTextures : Inspector_Base<LevelEntity_Polygon>
    {
        public Inspector_PolygonTextures(LevelEntity_Polygon polygon) : base(polygon)
        {
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

        [CreateProperty]
        public UnityEngine.Texture FloorTexture
        {
            get
            {
                return TextureOrPlaceholder(Polygon.floor_texture);
            }
        }

        // Its bitmap in its collection (-1 for none); one the shapes file doesn't have is shown as a grid
        [CreateProperty]
        public int FloorBitmap
        {
            get
            {
                return BitmapOf(Polygon.floor_texture);
            }
            set
            {
                SetTexture(LevelEntity_Polygon.DataSources.Floor, WithBitmap(Polygon.floor_texture, value));
            }
        }

        [CreateProperty]
        public string FloorOffset
        {
            get
            {
                return Strings.Get(Strings.Textures, "Inspector.PolygonTextures.Offset", Polygon.floor_origin.x, Polygon.floor_origin.y);
            }
        }

        [CreateProperty]
        public string FloorTransferMode
        {
            get
            {
                return TransferModeChoices.Choice(Polygon.floor_transfer_mode);
            }
            set
            {
                if (TransferModeChoices.TryParse(value, out var transferMode))
                {
                    Edit(polygon => polygon.SetTransferMode(LevelEntity_Polygon.DataSources.Floor, transferMode));
                }
            }
        }

        [CreateProperty]
        public List<string> FloorTransferModeChoices
        {
            get { return TransferModeChoices.All; }
        }

        [CreateProperty]
        public List<string> FloorTransferModeUnavailableChoices
        {
            get { return TransferModeChoices.Unavailable; }
        }

        // A surface with no texture isn't drawn
        [CreateProperty]
        public bool IsFloorTransferModeEditable
        {
            get { return !Polygon.floor_texture.IsEmptyShapeDescriptor(); }
        }

        [CreateProperty]
        public string FloorLightIndex
        {
            get
            {
                return Polygon.floor_lightsource_index.ToString();
            }
        }

        [CreateProperty]
        public UnityEngine.Texture CeilingTexture
        {
            get
            {
                return TextureOrPlaceholder(Polygon.ceiling_texture);
            }
        }

        [CreateProperty]
        public int CeilingBitmap
        {
            get
            {
                return BitmapOf(Polygon.ceiling_texture);
            }
            set
            {
                SetTexture(LevelEntity_Polygon.DataSources.Ceiling, WithBitmap(Polygon.ceiling_texture, value));
            }
        }

        [CreateProperty]
        public string CeilingOffset
        {
            get
            {
                return Strings.Get(Strings.Textures, "Inspector.PolygonTextures.Offset", Polygon.ceiling_origin.x, Polygon.ceiling_origin.y);
            }
        }

        [CreateProperty]
        public string CeilingTransferMode
        {
            get
            {
                return TransferModeChoices.Choice(Polygon.ceiling_transfer_mode);
            }
            set
            {
                if (TransferModeChoices.TryParse(value, out var transferMode))
                {
                    Edit(polygon => polygon.SetTransferMode(LevelEntity_Polygon.DataSources.Ceiling, transferMode));
                }
            }
        }

        [CreateProperty]
        public List<string> CeilingTransferModeChoices
        {
            get { return TransferModeChoices.All; }
        }

        [CreateProperty]
        public List<string> CeilingTransferModeUnavailableChoices
        {
            get { return TransferModeChoices.Unavailable; }
        }

        // A surface with no texture isn't drawn
        [CreateProperty]
        public bool IsCeilingTransferModeEditable
        {
            get { return !Polygon.ceiling_texture.IsEmptyShapeDescriptor(); }
        }

        [CreateProperty]
        public string CeilingLightIndex
        {
            get
            {
                return Polygon.ceiling_lightsource_index.ToString();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            MakeTextureChoosable(nameof(FloorTexture), () => Polygon.floor_texture, texture => SetTexture(LevelEntity_Polygon.DataSources.Floor, texture));
            MakeTextureChoosable(nameof(CeilingTexture), () => Polygon.ceiling_texture, texture => SetTexture(LevelEntity_Polygon.DataSources.Ceiling, texture));
        }

        // As painting it from the texture palette does
        private void SetTexture(LevelEntity_Polygon.DataSources dataSource, ushort shapeDescriptor)
        {
            Edit(polygon => polygon.SetShapeDescriptor(dataSource, shapeDescriptor));
        }
    }
}
