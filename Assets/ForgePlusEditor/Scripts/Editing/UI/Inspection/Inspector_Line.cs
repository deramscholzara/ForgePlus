using AlephOne;
using RuntimeCore.Entities.Geometry;
using TMPro;
using UnityEngine.UI;

namespace ForgePlus.Inspection
{
    public class Inspector_Line : Inspector_Base
    {
        public TextMeshProUGUI Value_Id;

        public Toggle Value_Flags_Solid;
        public Toggle Value_Flags_HasTransparentSide;
        public Toggle Value_Flags_Transparent;
        public Toggle Value_Flags_Landscape;
        public Toggle Value_Flags_VariableElevation;
        public Toggle Value_Flags_Elevation;

        public TextMeshProUGUI Value_Length;
        public TextMeshProUGUI Value_HighestFloorHeight;
        public TextMeshProUGUI Value_LowestCeilingHeight;

        public TextMeshProUGUI Value_Clockwise_Side_Index;
        public TextMeshProUGUI Value_Clockwise_Polygon_Index;

        public TextMeshProUGUI Value_CounterClockwise_Side_Index;
        public TextMeshProUGUI Value_CounterClockwise_Polygon_Index;

        public override void RefreshValuesInInspector()
        {
            var line = inspectedObject as LevelEntity_Line;

            Value_Id.text = line.NativeIndex.ToString();

            Value_Flags_Solid.SetIsOnWithoutNotify(map.LINE_IS_SOLID(line.NativeObject));
            Value_Flags_HasTransparentSide.SetIsOnWithoutNotify(map.LINE_HAS_TRANSPARENT_SIDE(line.NativeObject));
            Value_Flags_Transparent.SetIsOnWithoutNotify(map.LINE_IS_TRANSPARENT(line.NativeObject));
            Value_Flags_Landscape.SetIsOnWithoutNotify(map.LINE_IS_LANDSCAPED(line.NativeObject));
            Value_Flags_VariableElevation.SetIsOnWithoutNotify(map.LINE_IS_VARIABLE_ELEVATION(line.NativeObject));
            Value_Flags_Elevation.SetIsOnWithoutNotify(map.LINE_IS_ELEVATION(line.NativeObject));

            Value_Clockwise_Side_Index.text = line.NativeObject.clockwise_polygon_side_index.ToString();
            Value_Clockwise_Polygon_Index.text = line.NativeObject.clockwise_polygon_owner.ToString();

            Value_CounterClockwise_Side_Index.text = line.NativeObject.counterclockwise_polygon_side_index.ToString();
            Value_CounterClockwise_Polygon_Index.text = line.NativeObject.counterclockwise_polygon_owner.ToString();

            Value_Length.text = line.NativeObject.length.ToString();
            Value_HighestFloorHeight.text = line.NativeObject.highest_adjacent_floor.ToString();
            Value_LowestCeilingHeight.text = line.NativeObject.lowest_adjacent_ceiling.ToString();
        }
    }
}
