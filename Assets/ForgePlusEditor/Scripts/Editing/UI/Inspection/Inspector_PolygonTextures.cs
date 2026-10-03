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

        // Its collection: one of the level's (which the original games load), or another (which only Aleph One does)
        [CreateProperty]
        public string FloorCollection
        {
            get
            {
                return CollectionChoiceOf(Polygon.floor_texture);
            }
            set
            {
                if (TryWithCollection(Polygon.floor_texture, value, out var texture))
                {
                    SetTexture(LevelEntity_Polygon.DataSources.Floor, texture);
                }
            }
        }

        [CreateProperty]
        public List<string> FloorCollectionChoices
        {
            get { return CollectionChoices.All(Polygon.floor_texture); }
        }

        [CreateProperty]
        public bool IsFloorCollectionEditable
        {
            get { return !Polygon.floor_texture.IsEmptyShapeDescriptor(); }
        }

        [CreateProperty]
        public string FloorCollectionNote
        {
            get { return CollectionChoices.Note(Polygon.floor_texture); }
        }

        // Where its texture starts; a landscape (or a surface with no texture) has none to move
        [CreateProperty]
        public int FloorOffsetX
        {
            get
            {
                return Polygon.floor_origin.x;
            }
            set
            {
                SetOffset(LevelEntity_Polygon.DataSources.Floor, ClampToShort(value), Polygon.floor_origin.y);
            }
        }

        [CreateProperty]
        public int FloorOffsetY
        {
            get
            {
                return Polygon.floor_origin.y;
            }
            set
            {
                SetOffset(LevelEntity_Polygon.DataSources.Floor, Polygon.floor_origin.x, ClampToShort(value));
            }
        }

        [CreateProperty]
        public bool IsFloorOffsetEditable
        {
            get { return IsOffsettable(Polygon.floor_texture, Polygon.floor_transfer_mode); }
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
        public int FloorLightIndex
        {
            get
            {
                return Polygon.floor_lightsource_index;
            }
            set
            {
                SetLight(LevelEntity_Polygon.DataSources.Floor, value);
            }
        }

        // A landscape isn't lit
        [CreateProperty]
        public bool IsFloorLightIndexEditable
        {
            get { return !Polygon.floor_texture.UsesLandscapeCollection(); }
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
        public string CeilingCollection
        {
            get
            {
                return CollectionChoiceOf(Polygon.ceiling_texture);
            }
            set
            {
                if (TryWithCollection(Polygon.ceiling_texture, value, out var texture))
                {
                    SetTexture(LevelEntity_Polygon.DataSources.Ceiling, texture);
                }
            }
        }

        [CreateProperty]
        public List<string> CeilingCollectionChoices
        {
            get { return CollectionChoices.All(Polygon.ceiling_texture); }
        }

        [CreateProperty]
        public bool IsCeilingCollectionEditable
        {
            get { return !Polygon.ceiling_texture.IsEmptyShapeDescriptor(); }
        }

        [CreateProperty]
        public string CeilingCollectionNote
        {
            get { return CollectionChoices.Note(Polygon.ceiling_texture); }
        }

        [CreateProperty]
        public int CeilingOffsetX
        {
            get
            {
                return Polygon.ceiling_origin.x;
            }
            set
            {
                SetOffset(LevelEntity_Polygon.DataSources.Ceiling, ClampToShort(value), Polygon.ceiling_origin.y);
            }
        }

        [CreateProperty]
        public int CeilingOffsetY
        {
            get
            {
                return Polygon.ceiling_origin.y;
            }
            set
            {
                SetOffset(LevelEntity_Polygon.DataSources.Ceiling, Polygon.ceiling_origin.x, ClampToShort(value));
            }
        }

        [CreateProperty]
        public bool IsCeilingOffsetEditable
        {
            get { return IsOffsettable(Polygon.ceiling_texture, Polygon.ceiling_transfer_mode); }
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
        public int CeilingLightIndex
        {
            get
            {
                return Polygon.ceiling_lightsource_index;
            }
            set
            {
                SetLight(LevelEntity_Polygon.DataSources.Ceiling, value);
            }
        }

        [CreateProperty]
        public bool IsCeilingLightIndexEditable
        {
            get { return !Polygon.ceiling_texture.UsesLandscapeCollection(); }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            MakeTextureChoosable(nameof(FloorTexture), () => Polygon.floor_texture, texture => SetTexture(LevelEntity_Polygon.DataSources.Floor, texture));
            MakeTextureChoosable(nameof(CeilingTexture), () => Polygon.ceiling_texture, texture => SetTexture(LevelEntity_Polygon.DataSources.Ceiling, texture));

            BindNote(nameof(FloorCollectionNote));
            BindNote(nameof(CeilingCollectionNote));
        }

        // As painting it from the texture palette does
        private void SetTexture(LevelEntity_Polygon.DataSources dataSource, ushort shapeDescriptor)
        {
            Edit(polygon => polygon.SetShapeDescriptor(dataSource, shapeDescriptor));
        }

        private void SetOffset(LevelEntity_Polygon.DataSources dataSource, short x, short y)
        {
            Edit(polygon => polygon.SetOffset(dataSource, x, y, rebatch: true));
        }

        private void SetLight(LevelEntity_Polygon.DataSources dataSource, int lightIndex)
        {
            if (lightIndex >= 0)
            {
                Edit(polygon => polygon.SetLight(dataSource, (short) lightIndex));
            }
        }    }
}
