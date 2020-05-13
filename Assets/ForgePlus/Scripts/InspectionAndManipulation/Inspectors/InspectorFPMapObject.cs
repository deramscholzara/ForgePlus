using ForgePlus.LevelManipulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Weland;

namespace ForgePlus.Inspection
{
    public class InspectorFPMapObject : InspectorBase
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_Index;
        public TextMeshProUGUI Value_PolygonIndex;
        public TextMeshProUGUI Value_Angle;
        public TextMeshProUGUI Value_Position;

        public Toggle Value_Flags_Invisible;
        public Toggle Value_Flags_FromCeiling;
        public Toggle Value_Flags_Blind;
        public Toggle Value_Flags_Deaf;
        public Toggle Value_Flags_NetworkOnly;
        public Toggle Value_Flags_Floats;
        public Toggle Value_Flags_OnPlatform;

        public GameObject PlacementValuesRoot;
        public TextMeshProUGUI Value_Placement_InitialCount;
        public TextMeshProUGUI Value_Placement_MinimumCount;
        public TextMeshProUGUI Value_Placement_MaximumCount;
        public TextMeshProUGUI Value_Placement_RandomCount;
        public TextMeshProUGUI Value_Placement_RandomChance;
        public TextMeshProUGUI Value_Placement_RandomLocation;

        public override void RefreshValuesInInspector()
        {
            var fpMapObject = inspectedObject as FPMapObject;

            Value_Id.text = fpMapObject.Index.ToString();
            Value_Type.text = fpMapObject.WelandObject.Type.ToString();

            switch (fpMapObject.WelandObject.Type)
            {
                case Weland.ObjectType.Player:
                    Value_Index.text = $"({fpMapObject.WelandObject.Index})";
                    break;
                case Weland.ObjectType.Monster:
                    // TODO: Need to also inspect the Placement object "MonsterPlacement" from Weland.Level
                    Value_Index.text = $"{(MonsterType)fpMapObject.WelandObject.Index} ({fpMapObject.WelandObject.Index})";
                    break;
                case Weland.ObjectType.Item:
                    // TODO: Need to also inspect the Placement object "ItemPlacement" from Weland.Level
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

            Value_PolygonIndex.text = fpMapObject.WelandObject.PolygonIndex.ToString();

            Value_Flags_Invisible.SetIsOnWithoutNotify(fpMapObject.WelandObject.Invisible);
            Value_Flags_FromCeiling.SetIsOnWithoutNotify(fpMapObject.WelandObject.FromCeiling);
            Value_Flags_Blind.SetIsOnWithoutNotify(fpMapObject.WelandObject.Blind);
            Value_Flags_Deaf.SetIsOnWithoutNotify(fpMapObject.WelandObject.Deaf);
            Value_Flags_NetworkOnly.SetIsOnWithoutNotify(fpMapObject.WelandObject.NetworkOnly);
            Value_Flags_Floats.SetIsOnWithoutNotify(fpMapObject.WelandObject.Floats);
            Value_Flags_OnPlatform.SetIsOnWithoutNotify(fpMapObject.WelandObject.OnPlatform);

            Value_Angle.text = fpMapObject.WelandObject.Facing.ToString();
            Value_Position.text = $"X: {fpMapObject.WelandObject.X}\n" +
                                        $"Y: {fpMapObject.WelandObject.Y}\n" +
                                        $"Z: {fpMapObject.WelandObject.Z}";

            var placement = fpMapObject.Placement;
            if (placement == null)
            {
                PlacementValuesRoot.SetActive(false);
            }
            else
            {
                Value_Placement_InitialCount.text = placement.InitialCount.ToString();
                Value_Placement_MinimumCount.text = placement.MinimumCount.ToString();
                Value_Placement_MaximumCount.text = placement.MaximumCount.ToString();
                Value_Placement_RandomCount.text = placement.RandomCount.ToString();
                Value_Placement_RandomChance.text = $"{placement.RandomPercent} %";
                Value_Placement_RandomLocation.text = placement.RandomLocation.ToString();
            }
        }

        public override void UpdateValuesInInspectedObject()
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject (casted to FPMapObject in this case)
            throw new System.NotImplementedException();
        }
    }
}
