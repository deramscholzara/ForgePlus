using ForgePlus.LevelManipulation;
using RuntimeCore.Entities.MapObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Weland;

namespace ForgePlus.Inspection
{
    public class Inspector_MapObject : Inspector_Base
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
            var fpMapObject = inspectedObject as LevelEntity_MapObject;

            Value_Id.text = fpMapObject.NativeIndex.ToString();
            Value_Type.text = fpMapObject.NativeObject.Type.ToString();

            switch (fpMapObject.NativeObject.Type)
            {
                case Weland.ObjectType.Player:
                    Value_Index.text = $"({fpMapObject.NativeObject.Index})";
                    break;
                case Weland.ObjectType.Monster:
                    // TODO: Need to also inspect the Placement object "MonsterPlacement" from Weland.Level
                    Value_Index.text = $"{(MonsterType)fpMapObject.NativeObject.Index} ({fpMapObject.NativeObject.Index})";
                    break;
                case Weland.ObjectType.Item:
                    // TODO: Need to also inspect the Placement object "ItemPlacement" from Weland.Level
                    Value_Index.text = $"{(ItemType)fpMapObject.NativeObject.Index} ({fpMapObject.NativeObject.Index})";
                    break;
                case Weland.ObjectType.Scenery:
                    Value_Index.text = $"({fpMapObject.NativeObject.Index})";// Needs physics loaded?  Not sure why this isn't an enum in Weland - maybe I should make one...
                    break;
                case Weland.ObjectType.Sound:
                    Value_Index.text = $"({fpMapObject.NativeObject.Index})";
                    break;
                case Weland.ObjectType.Goal:
                    Value_Index.text = $"({fpMapObject.NativeObject.Index})";
                    break;
                default:
                    Value_Index.text = "Invalid";
                    break;
            }

            Value_PolygonIndex.text = fpMapObject.NativeObject.PolygonIndex.ToString();

            Value_Flags_Invisible.SetIsOnWithoutNotify(fpMapObject.NativeObject.Invisible);
            Value_Flags_FromCeiling.SetIsOnWithoutNotify(fpMapObject.NativeObject.FromCeiling);
            Value_Flags_Blind.SetIsOnWithoutNotify(fpMapObject.NativeObject.Blind);
            Value_Flags_Deaf.SetIsOnWithoutNotify(fpMapObject.NativeObject.Deaf);
            Value_Flags_NetworkOnly.SetIsOnWithoutNotify(fpMapObject.NativeObject.NetworkOnly);
            Value_Flags_Floats.SetIsOnWithoutNotify(fpMapObject.NativeObject.Floats);
            Value_Flags_OnPlatform.SetIsOnWithoutNotify(fpMapObject.NativeObject.OnPlatform);

            Value_Angle.text = fpMapObject.NativeObject.Facing.ToString();
            Value_Position.text = $"X: {fpMapObject.NativeObject.X}\n" +
                                        $"Y: {fpMapObject.NativeObject.Y}\n" +
                                        $"Z: {fpMapObject.NativeObject.Z}";

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
    }
}
