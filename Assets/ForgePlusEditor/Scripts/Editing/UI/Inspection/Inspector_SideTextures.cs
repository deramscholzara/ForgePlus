using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Materials;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Inspection
{
    public class Inspector_SideTextures : Inspector_Base
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Type;

        public RawImage Value_Primary_Texture;
        public TextMeshProUGUI Value_Primary_Offset;
        public TextMeshProUGUI Value_Primary_TransferMode;
        public TextMeshProUGUI Value_Primary_LightIndex;

        public RawImage Value_Secondary_Texture;
        public TextMeshProUGUI Value_Secondary_Offset;
        public TextMeshProUGUI Value_Secondary_TransferMode;
        public TextMeshProUGUI Value_Secondary_LightIndex;

        public RawImage Value_Transparent_Texture;
        public TextMeshProUGUI Value_Transparent_Offset;
        public TextMeshProUGUI Value_Transparent_TransferMode;
        public TextMeshProUGUI Value_Transparent_LightIndex;

        private LevelEntity_Side inspectedSide;

        private LevelEntity_Side InspectedSide
        {
            get
            {
                if (inspectedSide == null)
                {
                    inspectedSide = inspectedObject as LevelEntity_Side;
                }

                return inspectedSide;
            }
        }

        public override void RefreshValuesInInspector()
        {
            Value_Id.text =                         InspectedSide.NativeIndex.ToString();
            Value_Type.text =                       AlephOneNames.SideType(InspectedSide.NativeObject.type);

            var hasPrimaryData =                    !InspectedSide.NativeObject.primary_texture.texture.IsEmptyShapeDescriptor();
            Value_Primary_Texture.texture =         hasPrimaryData ? MaterialGeneration_Geometry.GetTexture(InspectedSide.NativeObject.primary_texture.texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Primary_Offset.text =             hasPrimaryData ? $"X: {InspectedSide.NativeObject.primary_texture.x0}\nY: {InspectedSide.NativeObject.primary_texture.y0}" : "X: -\nY: -";
            Value_Primary_LightIndex.text =         hasPrimaryData ? InspectedSide.NativeObject.primary_lightsource_index.ToString() : "-";
            Value_Primary_TransferMode.text =       hasPrimaryData ? InspectedSide.NativeObject.primary_transfer_mode.ToString() : "-";

            var hasSecondaryData =                  !InspectedSide.NativeObject.secondary_texture.texture.IsEmptyShapeDescriptor();
            Value_Secondary_Texture.texture =       hasSecondaryData ? MaterialGeneration_Geometry.GetTexture(InspectedSide.NativeObject.secondary_texture.texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Secondary_Offset.text =           hasSecondaryData ? $"X: {InspectedSide.NativeObject.secondary_texture.x0}\nY: {InspectedSide.NativeObject.secondary_texture.y0}" : "X: -\nY: -";
            Value_Secondary_LightIndex.text =       hasSecondaryData ? InspectedSide.NativeObject.secondary_lightsource_index.ToString() : "-";
            Value_Secondary_TransferMode.text =     hasSecondaryData ? InspectedSide.NativeObject.secondary_transfer_mode.ToString() : "-";

            var hasTransparentData =                !InspectedSide.NativeObject.transparent_texture.texture.IsEmptyShapeDescriptor();
            Value_Transparent_Texture.texture =     hasTransparentData ? MaterialGeneration_Geometry.GetTexture(InspectedSide.NativeObject.transparent_texture.texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Transparent_Offset.text =         hasTransparentData ? $"X: {InspectedSide.NativeObject.transparent_texture.x0}\nY: {InspectedSide.NativeObject.transparent_texture.y0}" : "X: -\nY: -";
            Value_Transparent_LightIndex.text =     hasTransparentData ? InspectedSide.NativeObject.transparent_lightsource_index.ToString() : "-";
            Value_Transparent_TransferMode.text =   hasTransparentData ? InspectedSide.NativeObject.transparent_transfer_mode.ToString() : "-";
        }
    }
}
