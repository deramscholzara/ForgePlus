using ForgePlus.LevelManipulation;
using TMPro;
using UnityEngine.UI;
using Weland;

namespace ForgePlus.Inspection
{
    public class InspectorFPLine : InspectorBase
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
            var fpLine = inspectedObject as FPLine;

            Value_Id.text = fpLine.Index.ToString();

            Value_Flags_Solid.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & LineFlags.Solid) != 0);
            Value_Flags_HasTransparentSide.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & LineFlags.HasTransparentSide) != 0);
            Value_Flags_Transparent.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & LineFlags.Transparent) != 0);
            Value_Flags_Landscape.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & LineFlags.Landscape) != 0);
            Value_Flags_VariableElevation.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & LineFlags.VariableElevation) != 0);
            Value_Flags_Elevation.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & LineFlags.Elevation) != 0);

            Value_Clockwise_Side_Index.text = fpLine.WelandObject.ClockwisePolygonSideIndex.ToString();
            Value_Clockwise_Polygon_Index.text = fpLine.WelandObject.ClockwisePolygonOwner.ToString();

            Value_CounterClockwise_Side_Index.text = fpLine.WelandObject.CounterclockwisePolygonSideIndex.ToString();
            Value_CounterClockwise_Polygon_Index.text = fpLine.WelandObject.CounterclockwisePolygonOwner.ToString();

            Value_Length.text = fpLine.WelandObject.Length.ToString();
            Value_HighestFloorHeight.text = fpLine.WelandObject.HighestAdjacentFloor.ToString();
            Value_LowestCeilingHeight.text = fpLine.WelandObject.LowestAdjacentCeiling.ToString();
        }

        public override void UpdateValuesInInspectedObject()
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }
    }
}
