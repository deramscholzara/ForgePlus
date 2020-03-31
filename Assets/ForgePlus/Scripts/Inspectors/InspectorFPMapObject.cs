using ForgePlus.LevelManipulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ForgePlus.Inspection
{
    public class InspectorFPMapObject : InspectorBase
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_Index;
        public TextMeshProUGUI Value_PolygonIndex;
        public TextMeshProUGUI Value_Flags;
        public TextMeshProUGUI Value_Angle;
        public TextMeshProUGUI Value_Position;

        public override void PopulateValues(IFPInspectable inspectedObject)
        {
            var fpMapObject = inspectedObject as FPMapObject;

            Value_Id.text =             fpMapObject.Index.ToString();
            Value_Type.text =           fpMapObject.WelandObject.Type.ToString();
            Value_Index.text =          fpMapObject.WelandObject.Index.ToString(); // TODO: Use this to get the exact type
            Value_PolygonIndex.text =   fpMapObject.WelandObject.PolygonIndex.ToString();
            Value_Flags.text =          "TODO: Flags";
            Value_Angle.text =          fpMapObject.WelandObject.Facing.ToString();
            Value_Position.text =       $"X: {fpMapObject.WelandObject.X}\n" +
                                        $"Y: {fpMapObject.WelandObject.Y}\n" +
                                        $"Z: {fpMapObject.WelandObject.Z}";
        }
    }
}
