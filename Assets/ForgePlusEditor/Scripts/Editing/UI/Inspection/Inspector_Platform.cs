using ForgePlus.LevelManipulation;
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

        private LevelEntity_Platform fpPlatform = null;

        public override void RefreshValuesInInspector()
        {
            fpPlatform = inspectedObject as LevelEntity_Platform;

            Value_Id.text = fpPlatform.NativeIndex.ToString();
            Value_Tag.text = fpPlatform.NativeObject.Tag.ToString();
            Value_PolygonIndex.text = fpPlatform.NativeObject.PolygonIndex.ToString();
            Value_Type.text = fpPlatform.NativeObject.Type.ToString();
            Value_Speed.text = fpPlatform.NativeObject.Speed.ToString();
            Value_Delay.text = fpPlatform.NativeObject.Delay.ToString();
            Value_MaximumHeight.text = fpPlatform.NativeObject.RuntimeMaximumHeight(LevelEntity_Level.Instance.Level).ToString();
            Value_MinimumHeight.text = fpPlatform.NativeObject.RuntimeMinimumHeight(LevelEntity_Level.Instance.Level).ToString();
            Value_Flags_InitiallyActive.SetIsOnWithoutNotify(fpPlatform.NativeObject.InitiallyActive);
            Value_Flags_InitiallyExtended.SetIsOnWithoutNotify(fpPlatform.NativeObject.InitiallyExtended);
            Value_Flags_IsLocked.SetIsOnWithoutNotify(fpPlatform.NativeObject.IsLocked);
            Value_Flags_IsPlayerControllable.SetIsOnWithoutNotify(fpPlatform.NativeObject.IsPlayerControllable);
            Value_Flags_IsMonsterControllable.SetIsOnWithoutNotify(fpPlatform.NativeObject.IsMonsterControllable);
            Value_Flags_CausesDamage.SetIsOnWithoutNotify(fpPlatform.NativeObject.CausesDamage);
            Value_Flags_ReversesWhenObstructed.SetIsOnWithoutNotify(fpPlatform.NativeObject.ReversesDirectionWhenObstructed);
            Value_Flags_DeactivatesAtEachLevel.SetIsOnWithoutNotify(fpPlatform.NativeObject.DeactivatesAtEachLevel);
            Value_Flags_DeactivatesAtInitialLevel.SetIsOnWithoutNotify(fpPlatform.NativeObject.DeactivatesAtInitialLevel);
            Value_Flags_ActivatesAdjacentOnActivation.SetIsOnWithoutNotify(fpPlatform.NativeObject.ActivatesAdjacentPlatformsWhenActivating);
            Value_Flags_ActivatesAdjacentOnDeactivation.SetIsOnWithoutNotify(fpPlatform.NativeObject.ActivatesAdjacentPlatformsWhenDeactivating);
            Value_Flags_DeactivatesAdjacentOnActivation.SetIsOnWithoutNotify(fpPlatform.NativeObject.DeactivatesAdjacentPlatformsWhenActivating);
            Value_Flags_DeactivatesAdjacentOnDeactivation.SetIsOnWithoutNotify(fpPlatform.NativeObject.DeactivatesAdjacentPlatformsWhenDeactivating);
            Value_Flags_ActivatesAdjacentAtEachLevel.SetIsOnWithoutNotify(fpPlatform.NativeObject.ActivatesAdjacantPlatformsAtEachLevel);
            Value_Flags_DelaysBeforeActivation.SetIsOnWithoutNotify(fpPlatform.NativeObject.DelaysBeforeActivation);
            Value_Flags_ActivatesOnlyOnce.SetIsOnWithoutNotify(fpPlatform.NativeObject.ActivatesOnlyOnce);
            Value_Flags_ActivatesLight.SetIsOnWithoutNotify(fpPlatform.NativeObject.ActivatesLight);
            Value_Flags_DeactivatesLight.SetIsOnWithoutNotify(fpPlatform.NativeObject.DeactivatesLight);
            Value_Flags_CannotBeExternallyDeactivated.SetIsOnWithoutNotify(fpPlatform.NativeObject.CannotBeExternallyDeactivated);
            Value_Flags_ContractsSlower.SetIsOnWithoutNotify(fpPlatform.NativeObject.ContractsSlower);
            Value_Flags_UsesNativePolygonHeights.SetIsOnWithoutNotify(fpPlatform.NativeObject.UsesNativePolygonHeights);
            Value_Flags_ExtendsFloorToCeiling.SetIsOnWithoutNotify(fpPlatform.NativeObject.ExtendsFloorToCeiling);
            Value_Flags_ComesFromFloor.SetIsOnWithoutNotify(fpPlatform.NativeObject.ComesFromFloor);
            Value_Flags_ComesFromCeiling.SetIsOnWithoutNotify(fpPlatform.NativeObject.ComesFromCeiling);
            Value_Flags_DoesNotActivateParent.SetIsOnWithoutNotify(fpPlatform.NativeObject.DoesNotActivateParent);
            Value_Flags_IsSecret.SetIsOnWithoutNotify(fpPlatform.NativeObject.IsSecret);
            Value_Flags_IsDoor.SetIsOnWithoutNotify(fpPlatform.NativeObject.IsDoor);

            Simulation_IsActive.onValueChanged.AddListener(delegate { fpPlatform.SetRuntimeActive(Simulation_IsActive.isOn); });
            fpPlatform.OnInspectionStateChange += OnInspectionStateChange;
            OnInspectionStateChange(fpPlatform);

            Simulation_Obstruct.onClick.AddListener(delegate { fpPlatform.ObstructRuntimeBehavior(); });
        }

        public void PrepareForDestruction()
        {
            fpPlatform.OnInspectionStateChange -= OnInspectionStateChange;

            foreach (var fpPlatform in LevelEntity_Level.Instance.FPCeilingFpPlatforms.Values)
            {
                fpPlatform.BeginRuntimeStyleBehavior();
            }

            foreach (var fpPlatform in LevelEntity_Level.Instance.FPFloorFpPlatforms.Values)
            {
                fpPlatform.BeginRuntimeStyleBehavior();
            }
        }

        private void OnInspectionStateChange(LevelEntity_Platform platform)
        {
            // TODO: Make this update everything that is display - a full refresh.

            Simulation_IsActive.SetIsOnWithoutNotify(platform.IsRuntimeActive);
        }
    }
}
