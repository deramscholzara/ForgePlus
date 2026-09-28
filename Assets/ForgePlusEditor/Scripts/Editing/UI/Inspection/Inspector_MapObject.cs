using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.MapObjects;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            var mapObject = inspectedObject as LevelEntity_MapObject;

            Value_Id.text = mapObject.NativeIndex.ToString();
            Value_Type.text = mapObject.NativeObject.GetTypeName();

            switch (mapObject.NativeObject.type)
            {
                case map._saved_monster:
                    Value_Index.text = $"{AlephOneNames.MonsterType(mapObject.NativeObject.index)} ({mapObject.NativeObject.index})";
                    break;
                case map._saved_item:
                    Value_Index.text = $"{AlephOneNames.ItemType(mapObject.NativeObject.index)} ({mapObject.NativeObject.index})";
                    break;
                case map._saved_player:
                case map._saved_object:
                case map._saved_sound_source:
                case map._saved_goal:
                    Value_Index.text = $"({mapObject.NativeObject.index})";
                    break;
                default:
                    Value_Index.text = "Invalid";
                    break;
            }

            Value_PolygonIndex.text = mapObject.NativeObject.polygon_index.ToString();

            var flags = mapObject.NativeObject.flags;
            Value_Flags_Invisible.SetIsOnWithoutNotify(csmacros.TEST_FLAG(flags, map._map_object_is_invisible));
            Value_Flags_FromCeiling.SetIsOnWithoutNotify(csmacros.TEST_FLAG(flags, map._map_object_hanging_from_ceiling));
            Value_Flags_Blind.SetIsOnWithoutNotify(csmacros.TEST_FLAG(flags, map._map_object_is_blind));
            Value_Flags_Deaf.SetIsOnWithoutNotify(csmacros.TEST_FLAG(flags, map._map_object_is_deaf));
            Value_Flags_NetworkOnly.SetIsOnWithoutNotify(csmacros.TEST_FLAG(flags, map._map_object_is_network_only));
            Value_Flags_Floats.SetIsOnWithoutNotify(csmacros.TEST_FLAG(flags, map._map_object_floats));
            Value_Flags_OnPlatform.SetIsOnWithoutNotify(csmacros.TEST_FLAG(flags, map._map_object_is_platform_sound));

            Value_Angle.text = AlephOneExtensions.AngleToDegrees(mapObject.NativeObject.facing).ToString();
            Value_Position.text = $"X: {mapObject.NativeObject.location.x}\n" +
                                        $"Y: {mapObject.NativeObject.location.y}\n" +
                                        $"Z: {mapObject.NativeObject.location.z}";

            var placement = mapObject.Placement;
            if (placement == null)
            {
                PlacementValuesRoot.SetActive(false);
            }
            else
            {
                Value_Placement_InitialCount.text = placement.initial_count.ToString();
                Value_Placement_MinimumCount.text = placement.minimum_count.ToString();
                Value_Placement_MaximumCount.text = placement.maximum_count.ToString();
                Value_Placement_RandomCount.text = placement.random_count.ToString();
                // random_chance is in (0, 65535]
                Value_Placement_RandomChance.text = $"{Math.Round(placement.random_chance * 100.0 / ushort.MaxValue)} %";
                Value_Placement_RandomLocation.text = csmacros.TEST_FLAG(placement.flags, map._reappears_in_random_location).ToString();
            }
        }
    }
}
