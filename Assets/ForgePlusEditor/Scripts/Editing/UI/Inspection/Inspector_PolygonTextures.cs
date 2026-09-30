using AlephOne;
using ForgePlus.Extensions;
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

        [CreateProperty]
        public string FloorOffset
        {
            get
            {
                return $"X: {Polygon.floor_origin.x}\nY: {Polygon.floor_origin.y}";
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
        public string CeilingOffset
        {
            get
            {
                return $"X: {Polygon.ceiling_origin.x}\nY: {Polygon.ceiling_origin.y}";
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
    }
}
