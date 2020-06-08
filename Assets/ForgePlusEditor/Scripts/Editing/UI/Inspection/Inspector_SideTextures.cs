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
            Value_Type.text =                       InspectedSide.NativeObject.Type.ToString();

            var hasPrimaryData =                    !InspectedSide.NativeObject.Primary.Texture.IsEmpty();
            Value_Primary_Texture.texture =         hasPrimaryData ? MaterialGeneration_Geometry.GetTexture(InspectedSide.NativeObject.Primary.Texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Primary_Offset.text =             hasPrimaryData ? $"X: {InspectedSide.NativeObject.Primary.X}\nY: {InspectedSide.NativeObject.Primary.Y}" : "X: -\nY: -";
            Value_Primary_LightIndex.text =         hasPrimaryData ? InspectedSide.NativeObject.PrimaryLightsourceIndex.ToString() : "-";
            Value_Primary_TransferMode.text =       hasPrimaryData ? InspectedSide.NativeObject.PrimaryTransferMode.ToString() : "-";

            var hasSecondaryData =                  !InspectedSide.NativeObject.Secondary.Texture.IsEmpty();
            Value_Secondary_Texture.texture =       hasSecondaryData ? MaterialGeneration_Geometry.GetTexture(InspectedSide.NativeObject.Secondary.Texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Secondary_Offset.text =           hasSecondaryData ? $"X: {InspectedSide.NativeObject.Secondary.X}\nY: {InspectedSide.NativeObject.Secondary.Y}" : "X: -\nY: -";
            Value_Secondary_LightIndex.text =       hasSecondaryData ? InspectedSide.NativeObject.SecondaryLightsourceIndex.ToString() : "-";
            Value_Secondary_TransferMode.text =     hasSecondaryData ? InspectedSide.NativeObject.SecondaryTransferMode.ToString() : "-";

            var hasTransparentData =                !InspectedSide.NativeObject.Transparent.Texture.IsEmpty();
            Value_Transparent_Texture.texture =     hasTransparentData ? MaterialGeneration_Geometry.GetTexture(InspectedSide.NativeObject.Transparent.Texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Transparent_Offset.text =         hasTransparentData ? $"X: {InspectedSide.NativeObject.Transparent.X}\nY: {InspectedSide.NativeObject.Transparent.Y}" : "X: -\nY: -";
            Value_Transparent_LightIndex.text =     hasTransparentData ? InspectedSide.NativeObject.TransparentLightsourceIndex.ToString() : "-";
            Value_Transparent_TransferMode.text =   hasTransparentData ? InspectedSide.NativeObject.TransparentTransferMode.ToString() : "-";
        }
    }
}
