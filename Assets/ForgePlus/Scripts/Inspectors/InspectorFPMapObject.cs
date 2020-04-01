using ForgePlus.LevelManipulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Weland;

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

            switch(fpMapObject.WelandObject.Type)
            {
                case Weland.ObjectType.Player:
                    Value_Index.text = $"({fpMapObject.WelandObject.Index})";
                    break;
                case Weland.ObjectType.Monster:
                    Value_Index.text = $"{(MonsterType)fpMapObject.WelandObject.Index} ({fpMapObject.WelandObject.Index})";
                    break;
                case Weland.ObjectType.Item:
                    Value_Index.text = $"{(ItemType)fpMapObject.WelandObject.Index} ({fpMapObject.WelandObject.Index})";
                    break;
                case Weland.ObjectType.Scenery:
                    Value_Index.text = $"({fpMapObject.WelandObject.Index})";// Needs physics loaded?  Not sure why this isn't an enum in Weland - maybe I should make one...
                    break;
                case Weland.ObjectType.Sound:
                    Value_Index.text = $"({fpMapObject.WelandObject.Index})";
                    break;
                case Weland.ObjectType.Goal:
                    Value_Index.text = $"({fpMapObject.WelandObject.Index})";
                    break;
                default:
                    Value_Index.text = "Invalid";
                    break;
            }

            Value_PolygonIndex.text =   fpMapObject.WelandObject.PolygonIndex.ToString();

            Value_Flags.text = $"Invisible: {fpMapObject.WelandObject.Invisible}\n" +
                               $"OnPlatform: {fpMapObject.WelandObject.OnPlatform}\n" +
                               $"FromCeiling: {fpMapObject.WelandObject.FromCeiling}\n" +
                               $"Blind: {fpMapObject.WelandObject.Blind}\n" +
                               $"Deaf: {fpMapObject.WelandObject.Deaf}\n" +
                               $"Floats: {fpMapObject.WelandObject.Floats}\n" +
                               $"NetworkOnly: {fpMapObject.WelandObject.NetworkOnly}\n";


            Value_Angle.text =          fpMapObject.WelandObject.Facing.ToString();
            Value_Position.text =       $"X: {fpMapObject.WelandObject.X}\n" +
                                        $"Y: {fpMapObject.WelandObject.Y}\n" +
                                        $"Z: {fpMapObject.WelandObject.Z}";
        }

        public override void UpdateValuesInInspectedObject(IFPInspectable inspectedObject)
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject (casted to FPMapObject in this case)
            throw new System.NotImplementedException();
        }
    }
}
