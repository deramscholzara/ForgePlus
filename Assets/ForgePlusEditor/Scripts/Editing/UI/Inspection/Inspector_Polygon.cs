using ForgePlus.Extensions;
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
            var polygon = inspectedObject as LevelEntity_Polygon;

            Value_Id.text = polygon.NativeIndex.ToString();
            Value_Type.text = AlephOneNames.PolygonType(polygon.NativeObject.type);
            Value_Permutation.text = polygon.NativeObject.permutation.ToString();

            Value_MediaIndex.text = polygon.NativeObject.media_index.ToString();
            Value_MediaLight.text = polygon.NativeObject.media_lightsource_index.ToString();

            Value_AmbientSound.text = polygon.NativeObject.ambient_sound_image_index.ToString();
            Value_RandomSound.text = polygon.NativeObject.random_sound_image_index.ToString();

            Value_Floor_Height.text = polygon.NativeObject.floor_height.ToString();
            Value_Floor_LightIndex.text = polygon.NativeObject.floor_lightsource_index.ToString();

            Value_Ceiling_Height.text = polygon.NativeObject.ceiling_height.ToString();
            Value_Ceiling_LightIndex.text = polygon.NativeObject.ceiling_lightsource_index.ToString();

            Value_VertexCount.text = polygon.NativeObject.vertex_count.ToString();

            var endpointIndices = string.Empty;
            for (var i = 0; i < polygon.NativeObject.vertex_count; i++)
            {
                var index = polygon.NativeObject.endpoint_indexes[i].ToString();

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
            for (var i = 0; i < polygon.NativeObject.vertex_count; i++)
            {
                var index = polygon.NativeObject.line_indexes[i].ToString();

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
            for (var i = 0; i < polygon.NativeObject.vertex_count; i++)
            {
                var index = polygon.NativeObject.side_indexes[i] < 0 ? "- no side -" : polygon.NativeObject.side_indexes[i].ToString();

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
            for (var i = 0; i < polygon.NativeObject.vertex_count; i++)
            {
                var index = polygon.NativeObject.adjacent_polygon_indexes[i] < 0 ? "- no polygon -" : polygon.NativeObject.adjacent_polygon_indexes[i].ToString();

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

            Value_FirstObjectIndex.text = polygon.NativeObject.first_object.ToString();
        }
    }
}
