using ForgePlus.LevelManipulation;
using RuntimeCore.Materials;
using RuntimeCore.Entities.Geometry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Inspection
{
    public class Inspector_PolygonTextures : Inspector_Base
    {
        public TextMeshProUGUI Value_Id;

        public RawImage Value_Floor_Texture;
        public TextMeshProUGUI Value_Floor_Offset;
        public TextMeshProUGUI Value_Floor_TransferMode;
        public TextMeshProUGUI Value_Floor_LightIndex;

        public RawImage Value_Ceiling_Texture;
        public TextMeshProUGUI Value_Ceiling_Offset;
        public TextMeshProUGUI Value_Ceiling_TransferMode;
        public TextMeshProUGUI Value_Ceiling_LightIndex;

        public override void RefreshValuesInInspector()
        {
            var fpPolygon = inspectedObject as LevelEntity_Polygon;

            Value_Id.text = fpPolygon.NativeIndex.ToString();

            var floorTexture = MaterialGeneration_Geometry.GetTexture(fpPolygon.NativeObject.FloorTexture);
            Value_Floor_Texture.texture = floorTexture ? floorTexture : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Floor_Offset.text = $"X: {fpPolygon.NativeObject.FloorOrigin.X}\nY: {fpPolygon.NativeObject.FloorOrigin.Y}";
            Value_Floor_TransferMode.text = fpPolygon.NativeObject.FloorTransferMode.ToString();
            Value_Floor_LightIndex.text = fpPolygon.NativeObject.FloorLight.ToString();

            var ceilingTexture = MaterialGeneration_Geometry.GetTexture(fpPolygon.NativeObject.CeilingTexture);
            Value_Ceiling_Texture.texture = ceilingTexture ? ceilingTexture : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Ceiling_Offset.text = $"X: {fpPolygon.NativeObject.CeilingOrigin.X}\nY: {fpPolygon.NativeObject.CeilingOrigin.Y}";
            Value_Ceiling_TransferMode.text = fpPolygon.NativeObject.CeilingTransferMode.ToString();
            Value_Ceiling_LightIndex.text = fpPolygon.NativeObject.CeilingLight.ToString();
        }
    }
}
