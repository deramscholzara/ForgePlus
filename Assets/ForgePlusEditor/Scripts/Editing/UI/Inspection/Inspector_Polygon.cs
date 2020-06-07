using ForgePlus.LevelManipulation;
using RuntimeCore.Entities.Geometry;
using TMPro;

namespace ForgePlus.Inspection
{
    public class Inspector_Polygon : Inspector_Base
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_Permutation;

        public TextMeshProUGUI Value_MediaIndex;
        public TextMeshProUGUI Value_MediaLight;

        public TextMeshProUGUI Value_AmbientSound;
        public TextMeshProUGUI Value_RandomSound;

        public TextMeshProUGUI Value_Floor_Height;
        public TextMeshProUGUI Value_Floor_LightIndex;

        public TextMeshProUGUI Value_Ceiling_Height;
        public TextMeshProUGUI Value_Ceiling_LightIndex;

        public TextMeshProUGUI Value_VertexCount;
        public TextMeshProUGUI Value_VertexIndices;
        public TextMeshProUGUI Value_LineIndices;
        public TextMeshProUGUI Value_SideIndices;
        public TextMeshProUGUI Value_FirstObjectIndex;
        public TextMeshProUGUI Value_AdjacentPolygonIndices;

        public override void RefreshValuesInInspector()
        {
            var fpPolygon = inspectedObject as LevelEntity_Polygon;

            Value_Id.text = fpPolygon.NativeIndex.ToString();
            Value_Type.text = fpPolygon.NativeObject.Type.ToString();
            Value_Permutation.text = fpPolygon.NativeObject.Permutation.ToString();

            Value_MediaIndex.text = fpPolygon.NativeObject.MediaIndex.ToString();
            Value_MediaLight.text = fpPolygon.NativeObject.MediaLight.ToString();

            Value_AmbientSound.text = fpPolygon.NativeObject.AmbientSound.ToString();
            Value_RandomSound.text = fpPolygon.NativeObject.RandomSound.ToString();

            Value_Floor_Height.text = fpPolygon.NativeObject.FloorHeight.ToString();
            Value_Floor_LightIndex.text = fpPolygon.NativeObject.FloorLight.ToString();

            Value_Ceiling_Height.text = fpPolygon.NativeObject.CeilingHeight.ToString();
            Value_Ceiling_LightIndex.text = fpPolygon.NativeObject.CeilingLight.ToString();

            Value_VertexCount.text = fpPolygon.NativeObject.VertexCount.ToString();

            var endpointIndices = string.Empty;
            for (var i = 0; i < fpPolygon.NativeObject.VertexCount; i++)
            {
                var index = fpPolygon.NativeObject.EndpointIndexes[i].ToString();

                if (i == 0)
                {
                    endpointIndices += index;
                }
                else
                {
                    endpointIndices += $"\n{index}";
                }
            }

            Value_VertexIndices.text = endpointIndices;

            var lineIndices = string.Empty;
            for (var i = 0; i < fpPolygon.NativeObject.VertexCount; i++)
            {
                var index = fpPolygon.NativeObject.LineIndexes[i].ToString();

                if (i == 0)
                {
                    lineIndices += index;
                }
                else
                {
                    lineIndices += $"\n{index}";
                }
            }

            Value_LineIndices.text = lineIndices;

            var sideIndices = string.Empty;
            for (var i = 0; i < fpPolygon.NativeObject.VertexCount; i++)
            {
                var index = fpPolygon.NativeObject.SideIndexes[i] < 0 ? "- no side -" : fpPolygon.NativeObject.SideIndexes[i].ToString();

                if (i == 0)
                {
                    sideIndices += index;
                }
                else
                {
                    sideIndices += $"\n{index}";
                }
            }

            Value_SideIndices.text = sideIndices;

            var adjacentPolygonIndices = string.Empty;
            for (var i = 0; i < fpPolygon.NativeObject.VertexCount; i++)
            {
                var index = fpPolygon.NativeObject.AdjacentPolygonIndexes[i] < 0 ? "- no polygon -" : fpPolygon.NativeObject.AdjacentPolygonIndexes[i].ToString();

                if (i == 0)
                {
                    adjacentPolygonIndices += index;
                }
                else
                {
                    adjacentPolygonIndices += $"\n{index}";
                }
            }

            Value_SideIndices.text = adjacentPolygonIndices;

            Value_FirstObjectIndex.text = fpPolygon.NativeObject.FirstObjectIndex.ToString();
        }
    }
}
