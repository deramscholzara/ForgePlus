using RuntimeCore.Common;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using TMPro;
using UnityEngine.UI;
using Weland.Extensions;

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
            Value_Tag.text = platform.NativeObject.Tag.ToString();
            Value_PolygonIndex.text = platform.NativeObject.PolygonIndex.ToString();
            Value_Type.text = platform.NativeObject.Type.ToString();
            Value_Speed.text = platform.NativeObject.Speed.ToString();
            Value_Delay.text = platform.NativeObject.Delay.ToString();
            Value_MaximumHeight.text = platform.NativeObject.RuntimeMaximumHeight(LevelEntity_Level.Instance.Level).ToString();
            Value_MinimumHeight.text = platform.NativeObject.RuntimeMinimumHeight(LevelEntity_Level.Instance.Level).ToString();
            Value_Flags_InitiallyActive.SetIsOnWithoutNotify(platform.NativeObject.InitiallyActive);
            Value_Flags_InitiallyExtended.SetIsOnWithoutNotify(platform.NativeObject.InitiallyExtended);
            Value_Flags_IsLocked.SetIsOnWithoutNotify(platform.NativeObject.IsLocked);
            Value_Flags_IsPlayerControllable.SetIsOnWithoutNotify(platform.NativeObject.IsPlayerControllable);
            Value_Flags_IsMonsterControllable.SetIsOnWithoutNotify(platform.NativeObject.IsMonsterControllable);
            Value_Flags_CausesDamage.SetIsOnWithoutNotify(platform.NativeObject.CausesDamage);
            Value_Flags_ReversesWhenObstructed.SetIsOnWithoutNotify(platform.NativeObject.ReversesDirectionWhenObstructed);
            Value_Flags_DeactivatesAtEachLevel.SetIsOnWithoutNotify(platform.NativeObject.DeactivatesAtEachLevel);
            Value_Flags_DeactivatesAtInitialLevel.SetIsOnWithoutNotify(platform.NativeObject.DeactivatesAtInitialLevel);
            Value_Flags_ActivatesAdjacentOnActivation.SetIsOnWithoutNotify(platform.NativeObject.ActivatesAdjacentPlatformsWhenActivating);
            Value_Flags_ActivatesAdjacentOnDeactivation.SetIsOnWithoutNotify(platform.NativeObject.ActivatesAdjacentPlatformsWhenDeactivating);
            Value_Flags_DeactivatesAdjacentOnActivation.SetIsOnWithoutNotify(platform.NativeObject.DeactivatesAdjacentPlatformsWhenActivating);
            Value_Flags_DeactivatesAdjacentOnDeactivation.SetIsOnWithoutNotify(platform.NativeObject.DeactivatesAdjacentPlatformsWhenDeactivating);
            Value_Flags_ActivatesAdjacentAtEachLevel.SetIsOnWithoutNotify(platform.NativeObject.ActivatesAdjacantPlatformsAtEachLevel);
            Value_Flags_DelaysBeforeActivation.SetIsOnWithoutNotify(platform.NativeObject.DelaysBeforeActivation);
            Value_Flags_ActivatesOnlyOnce.SetIsOnWithoutNotify(platform.NativeObject.ActivatesOnlyOnce);
            Value_Flags_ActivatesLight.SetIsOnWithoutNotify(platform.NativeObject.ActivatesLight);
            Value_Flags_DeactivatesLight.SetIsOnWithoutNotify(platform.NativeObject.DeactivatesLight);
            Value_Flags_CannotBeExternallyDeactivated.SetIsOnWithoutNotify(platform.NativeObject.CannotBeExternallyDeactivated);
            Value_Flags_ContractsSlower.SetIsOnWithoutNotify(platform.NativeObject.ContractsSlower);
            Value_Flags_UsesNativePolygonHeights.SetIsOnWithoutNotify(platform.NativeObject.UsesNativePolygonHeights);
            Value_Flags_ExtendsFloorToCeiling.SetIsOnWithoutNotify(platform.NativeObject.ExtendsFloorToCeiling);
            Value_Flags_ComesFromFloor.SetIsOnWithoutNotify(platform.NativeObject.ComesFromFloor);
            Value_Flags_ComesFromCeiling.SetIsOnWithoutNotify(platform.NativeObject.ComesFromCeiling);
            Value_Flags_DoesNotActivateParent.SetIsOnWithoutNotify(platform.NativeObject.DoesNotActivateParent);
            Value_Flags_IsSecret.SetIsOnWithoutNotify(platform.NativeObject.IsSecret);
            Value_Flags_IsDoor.SetIsOnWithoutNotify(platform.NativeObject.IsDoor);

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
