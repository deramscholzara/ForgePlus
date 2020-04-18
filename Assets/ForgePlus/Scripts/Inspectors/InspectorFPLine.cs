using ForgePlus.LevelManipulation;
using TMPro;
using Weland;

namespace ForgePlus.Inspection
{
    public class InspectorFPLine : InspectorBase
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Flags;
        public TextMeshProUGUI Value_Length;
        public TextMeshProUGUI Value_HighestFloorHeight;
        public TextMeshProUGUI Value_LowestCeilingHeight;

        public TextMeshProUGUI Value_Clockwise_Side_Index;
        public TextMeshProUGUI Value_Clockwise_Polygon_Index;

        public TextMeshProUGUI Value_CounterClockwise_Side_Index;
        public TextMeshProUGUI Value_CounterClockwise_Polygon_Index;

        public override void PopulateValues(IFPInspectable inspectedObject)
        {
            var fpLine = inspectedObject as FPLine;

            Value_Id.text =                             fpLine.Index.ToString();

            Value_Flags.text =                          $"Has Transparent Side: {(fpLine.WelandObject.Flags & LineFlags.HasTransparentSide) != 0}\n" +
                                                        $"Variable Elevation: {(fpLine.WelandObject.Flags & LineFlags.VariableElevation) != 0}\n" +
                                                        $"Elevation: {(fpLine.WelandObject.Flags & LineFlags.Elevation) != 0}\n" +
                                                        $"Landscape: {(fpLine.WelandObject.Flags & LineFlags.Landscape) != 0}\n" +
                                                        $"Transparent: {(fpLine.WelandObject.Flags & LineFlags.Transparent) != 0}\n" +
                                                        $"Solid: {(fpLine.WelandObject.Flags & LineFlags.Solid)}";

            Value_Length.text =                         fpLine.WelandObject.Length.ToString();
            Value_HighestFloorHeight.text =             fpLine.WelandObject.HighestAdjacentFloor.ToString();
            Value_LowestCeilingHeight.text =            fpLine.WelandObject.LowestAdjacentCeiling.ToString();

            Value_Clockwise_Side_Index.text =           fpLine.WelandObject.ClockwisePolygonSideIndex.ToString();
            Value_Clockwise_Polygon_Index.text =        fpLine.WelandObject.ClockwisePolygonOwner.ToString();

            Value_CounterClockwise_Side_Index.text =    fpLine.WelandObject.CounterclockwisePolygonSideIndex.ToString();
            Value_CounterClockwise_Polygon_Index.text = fpLine.WelandObject.CounterclockwisePolygonOwner.ToString();
        }

        public override void UpdateValuesInInspectedObject(IFPInspectable inspectedObject)
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }
    }
}
