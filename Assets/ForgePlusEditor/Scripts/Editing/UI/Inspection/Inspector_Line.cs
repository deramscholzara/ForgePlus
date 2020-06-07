using RuntimeCore.Entities.Geometry;
using TMPro;
using UnityEngine.UI;
using Weland;

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

            Value_Flags_Solid.SetIsOnWithoutNotify((line.NativeObject.Flags & LineFlags.Solid) != 0);
            Value_Flags_HasTransparentSide.SetIsOnWithoutNotify((line.NativeObject.Flags & LineFlags.HasTransparentSide) != 0);
            Value_Flags_Transparent.SetIsOnWithoutNotify((line.NativeObject.Flags & LineFlags.Transparent) != 0);
            Value_Flags_Landscape.SetIsOnWithoutNotify((line.NativeObject.Flags & LineFlags.Landscape) != 0);
            Value_Flags_VariableElevation.SetIsOnWithoutNotify((line.NativeObject.Flags & LineFlags.VariableElevation) != 0);
            Value_Flags_Elevation.SetIsOnWithoutNotify((line.NativeObject.Flags & LineFlags.Elevation) != 0);

            Value_Clockwise_Side_Index.text = line.NativeObject.ClockwisePolygonSideIndex.ToString();
            Value_Clockwise_Polygon_Index.text = line.NativeObject.ClockwisePolygonOwner.ToString();

            Value_CounterClockwise_Side_Index.text = line.NativeObject.CounterclockwisePolygonSideIndex.ToString();
            Value_CounterClockwise_Polygon_Index.text = line.NativeObject.CounterclockwisePolygonOwner.ToString();

            Value_Length.text = line.NativeObject.Length.ToString();
            Value_HighestFloorHeight.text = line.NativeObject.HighestAdjacentFloor.ToString();
            Value_LowestCeilingHeight.text = line.NativeObject.LowestAdjacentCeiling.ToString();
        }
    }
}
