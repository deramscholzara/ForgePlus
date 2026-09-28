using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Common;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using TMPro;
using UnityEngine.UI;

namespace ForgePlus.Inspection
{
    public class Inspector_Platform : Inspector_Base, IDestructionPreparable
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Tag;
        public TextMeshProUGUI Value_PolygonIndex;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_Speed;
        public TextMeshProUGUI Value_Delay;
        public TextMeshProUGUI Value_MaximumHeight;
        public TextMeshProUGUI Value_MinimumHeight;

        public Toggle Value_Flags_InitiallyActive;
        public Toggle Value_Flags_InitiallyExtended;
        public Toggle Value_Flags_IsLocked;
        public Toggle Value_Flags_IsPlayerControllable;
        public Toggle Value_Flags_IsMonsterControllable;
        public Toggle Value_Flags_CausesDamage;
        public Toggle Value_Flags_ReversesWhenObstructed;
        public Toggle Value_Flags_DeactivatesAtEachLevel;
        public Toggle Value_Flags_DeactivatesAtInitialLevel;
        public Toggle Value_Flags_ActivatesAdjacentOnActivation;
        public Toggle Value_Flags_ActivatesAdjacentOnDeactivation;
        public Toggle Value_Flags_DeactivatesAdjacentOnActivation;
        public Toggle Value_Flags_DeactivatesAdjacentOnDeactivation;
        public Toggle Value_Flags_ActivatesAdjacentAtEachLevel;
        public Toggle Value_Flags_DelaysBeforeActivation;
        public Toggle Value_Flags_ActivatesOnlyOnce;
        public Toggle Value_Flags_ActivatesLight;
        public Toggle Value_Flags_DeactivatesLight;
        public Toggle Value_Flags_CannotBeExternallyDeactivated;
        public Toggle Value_Flags_ContractsSlower;
        public Toggle Value_Flags_UsesNativePolygonHeights;
        public Toggle Value_Flags_ExtendsFloorToCeiling;
        public Toggle Value_Flags_ComesFromFloor;
        public Toggle Value_Flags_ComesFromCeiling;
        public Toggle Value_Flags_DoesNotActivateParent;
        public Toggle Value_Flags_IsSecret;
        public Toggle Value_Flags_IsDoor;

        public Toggle Simulation_IsActive;
        public Button Simulation_Obstruct;

        private LevelEntity_Platform platform = null;

        public override void RefreshValuesInInspector()
        {
            platform = inspectedObject as LevelEntity_Platform;

            Value_Id.text = platform.NativeIndex.ToString();
            Value_Tag.text = platform.NativeObject.tag.ToString();
            Value_PolygonIndex.text = platform.NativeObject.polygon_index.ToString();
            Value_Type.text = AlephOneNames.PlatformType(platform.NativeObject.type);
            Value_Speed.text = platform.NativeObject.speed.ToString();
            Value_Delay.text = platform.NativeObject.delay.ToString();
            Value_MaximumHeight.text = $"Floor: {platform.NativeObject.maximum_floor_height}  Ceiling: {platform.NativeObject.maximum_ceiling_height}";
            Value_MinimumHeight.text = $"Floor: {platform.NativeObject.minimum_floor_height}  Ceiling: {platform.NativeObject.minimum_ceiling_height}";

            var staticFlags = platform.NativeObject.static_flags;
            Value_Flags_InitiallyActive.SetIsOnWithoutNotify(platforms.PLATFORM_IS_INITIALLY_ACTIVE(staticFlags));
            Value_Flags_InitiallyExtended.SetIsOnWithoutNotify(platforms.PLATFORM_IS_INITIALLY_EXTENDED(staticFlags));
            Value_Flags_IsLocked.SetIsOnWithoutNotify(platforms.PLATFORM_IS_LOCKED(staticFlags));
            Value_Flags_IsPlayerControllable.SetIsOnWithoutNotify(platforms.PLATFORM_IS_PLAYER_CONTROLLABLE(staticFlags));
            Value_Flags_IsMonsterControllable.SetIsOnWithoutNotify(platforms.PLATFORM_IS_MONSTER_CONTROLLABLE(staticFlags));
            Value_Flags_CausesDamage.SetIsOnWithoutNotify(platforms.PLATFORM_CAUSES_DAMAGE(staticFlags));
            Value_Flags_ReversesWhenObstructed.SetIsOnWithoutNotify(platforms.PLATFORM_REVERSES_DIRECTION_WHEN_OBSTRUCTED(staticFlags));
            Value_Flags_DeactivatesAtEachLevel.SetIsOnWithoutNotify(platforms.PLATFORM_DEACTIVATES_AT_EACH_LEVEL(staticFlags));
            Value_Flags_DeactivatesAtInitialLevel.SetIsOnWithoutNotify(platforms.PLATFORM_DEACTIVATES_AT_INITIAL_LEVEL(staticFlags));
            Value_Flags_ActivatesAdjacentOnActivation.SetIsOnWithoutNotify(platforms.PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_WHEN_ACTIVATING(staticFlags));
            Value_Flags_ActivatesAdjacentOnDeactivation.SetIsOnWithoutNotify(platforms.PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_WHEN_DEACTIVATING(staticFlags));
            Value_Flags_DeactivatesAdjacentOnActivation.SetIsOnWithoutNotify(platforms.PLATFORM_DEACTIVATES_ADJACENT_PLATFORMS_WHEN_ACTIVATING(staticFlags));
            Value_Flags_DeactivatesAdjacentOnDeactivation.SetIsOnWithoutNotify(platforms.PLATFORM_DEACTIVATES_ADJACENT_PLATFORMS_WHEN_DEACTIVATING(staticFlags));
            Value_Flags_ActivatesAdjacentAtEachLevel.SetIsOnWithoutNotify(platforms.PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_AT_EACH_LEVEL(staticFlags));
            Value_Flags_DelaysBeforeActivation.SetIsOnWithoutNotify(platforms.PLATFORM_DELAYS_BEFORE_ACTIVATION(staticFlags));
            Value_Flags_ActivatesOnlyOnce.SetIsOnWithoutNotify(platforms.PLATFORM_ACTIVATES_ONLY_ONCE(staticFlags));
            Value_Flags_ActivatesLight.SetIsOnWithoutNotify(platforms.PLATFORM_ACTIVATES_LIGHT(staticFlags));
            Value_Flags_DeactivatesLight.SetIsOnWithoutNotify(platforms.PLATFORM_DEACTIVATES_LIGHT(staticFlags));
            Value_Flags_CannotBeExternallyDeactivated.SetIsOnWithoutNotify(platforms.PLATFORM_CANNOT_BE_EXTERNALLY_DEACTIVATED(staticFlags));
            Value_Flags_ContractsSlower.SetIsOnWithoutNotify(platforms.PLATFORM_CONTRACTS_SLOWER(staticFlags));
            Value_Flags_UsesNativePolygonHeights.SetIsOnWithoutNotify(platforms.PLATFORM_USES_NATIVE_POLYGON_HEIGHTS(staticFlags));
            Value_Flags_ExtendsFloorToCeiling.SetIsOnWithoutNotify(platforms.PLATFORM_EXTENDS_FLOOR_TO_CEILING(staticFlags));
            Value_Flags_ComesFromFloor.SetIsOnWithoutNotify(platforms.PLATFORM_COMES_FROM_FLOOR(staticFlags));
            Value_Flags_ComesFromCeiling.SetIsOnWithoutNotify(platforms.PLATFORM_COMES_FROM_CEILING(staticFlags));
            Value_Flags_DoesNotActivateParent.SetIsOnWithoutNotify(platforms.PLATFORM_DOES_NOT_ACTIVATE_PARENT(staticFlags));
            Value_Flags_IsSecret.SetIsOnWithoutNotify(platforms.PLATFORM_IS_SECRET(staticFlags));
            Value_Flags_IsDoor.SetIsOnWithoutNotify(platforms.PLATFORM_IS_DOOR(staticFlags));

            Simulation_IsActive.onValueChanged.AddListener(delegate { platform.SetRuntimeActive(Simulation_IsActive.isOn); });
            platform.OnInspectionStateChange += OnInspectionStateChange;
            OnInspectionStateChange(platform);

            Simulation_Obstruct.onClick.AddListener(delegate { platform.ObstructRuntimeBehavior(); });
        }

        public void PrepareForDestruction()
        {
            platform.OnInspectionStateChange -= OnInspectionStateChange;

            foreach (var runtimePlatform in LevelEntity_Level.Instance.CeilingPlatforms.Values)
            {
                runtimePlatform.BeginRuntimeStyleBehavior();
            }

            foreach (var runtimePlatform in LevelEntity_Level.Instance.FloorPlatforms.Values)
            {
                runtimePlatform.BeginRuntimeStyleBehavior();
            }
        }

        private void OnInspectionStateChange(LevelEntity_Platform platform)
        {
            // TODO: Make this update everything that is display - a full refresh.

            Simulation_IsActive.SetIsOnWithoutNotify(platform.IsRuntimeActive);
        }
    }
}
