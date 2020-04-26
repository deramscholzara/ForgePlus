using ForgePlus.LevelManipulation;
using ForgePlus.ShapesCollections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Inspection
{
    public class InspectorFPPolygonTextures : InspectorBase
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

        public override void PopulateValues(IFPInspectable inspectedObject)
        {
            var fpPolygon = inspectedObject as FPPolygon;

            Value_Id.text = fpPolygon.Index.ToString();

            var floorTexture = WallsCollection.GetTexture(fpPolygon.WelandObject.FloorTexture);
            Value_Floor_Texture.texture = floorTexture ? floorTexture : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Floor_Offset.text = $"X: {fpPolygon.WelandObject.FloorOrigin.X}\nY: {fpPolygon.WelandObject.FloorOrigin.Y}";
            Value_Floor_TransferMode.text = fpPolygon.WelandObject.FloorTransferMode.ToString();
            Value_Floor_LightIndex.text = fpPolygon.WelandObject.FloorLight.ToString();

            var ceilingTexture = WallsCollection.GetTexture(fpPolygon.WelandObject.CeilingTexture);
            Value_Ceiling_Texture.texture = ceilingTexture ? ceilingTexture : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Ceiling_Offset.text = $"X: {fpPolygon.WelandObject.CeilingOrigin.X}\nY: {fpPolygon.WelandObject.CeilingOrigin.Y}";
            Value_Ceiling_TransferMode.text = fpPolygon.WelandObject.CeilingTransferMode.ToString();
            Value_Ceiling_LightIndex.text = fpPolygon.WelandObject.CeilingLight.ToString();
        }

        public override void UpdateValuesInInspectedObject(IFPInspectable inspectedObject)
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }
    }
}
