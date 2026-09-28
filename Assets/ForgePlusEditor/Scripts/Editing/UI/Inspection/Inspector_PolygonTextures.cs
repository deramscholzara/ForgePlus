using RuntimeCore.Entities.Geometry;
using RuntimeCore.Materials;
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
            var polygon = inspectedObject as LevelEntity_Polygon;

            Value_Id.text = polygon.NativeIndex.ToString();

            var floorTexture = MaterialGeneration_Geometry.GetTexture(polygon.NativeObject.floor_texture);
            Value_Floor_Texture.texture = floorTexture ? floorTexture : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Floor_Offset.text = $"X: {polygon.NativeObject.floor_origin.x}\nY: {polygon.NativeObject.floor_origin.y}";
            Value_Floor_TransferMode.text = polygon.NativeObject.floor_transfer_mode.ToString();
            Value_Floor_LightIndex.text = polygon.NativeObject.floor_lightsource_index.ToString();

            var ceilingTexture = MaterialGeneration_Geometry.GetTexture(polygon.NativeObject.ceiling_texture);
            Value_Ceiling_Texture.texture = ceilingTexture ? ceilingTexture : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Ceiling_Offset.text = $"X: {polygon.NativeObject.ceiling_origin.x}\nY: {polygon.NativeObject.ceiling_origin.y}";
            Value_Ceiling_TransferMode.text = polygon.NativeObject.ceiling_transfer_mode.ToString();
            Value_Ceiling_LightIndex.text = polygon.NativeObject.ceiling_lightsource_index.ToString();
        }
    }
}
