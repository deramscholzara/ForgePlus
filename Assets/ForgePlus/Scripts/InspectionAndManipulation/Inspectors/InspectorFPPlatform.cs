using ForgePlus.LevelManipulation;
using TMPro;
using UnityEngine.UI;
using Weland.Extensions;

namespace ForgePlus.Inspection
{
    public class InspectorFPPlatform : InspectorBase, IFPDestructionPreparable
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

        private FPPlatform fpPlatform = null;

        public override void RefreshValuesInInspector()
        {
            fpPlatform = inspectedObject as FPPlatform;

            Value_Id.text = fpPlatform.Index.ToString();
            Value_Tag.text = fpPlatform.WelandObject.Tag.ToString();
            Value_PolygonIndex.text = fpPlatform.WelandObject.PolygonIndex.ToString();
            Value_Type.text = fpPlatform.WelandObject.Type.ToString();
            Value_Speed.text = fpPlatform.WelandObject.Speed.ToString();
            Value_Delay.text = fpPlatform.WelandObject.Delay.ToString();
            Value_MaximumHeight.text = fpPlatform.WelandObject.RuntimeMaximumHeight(FPLevel.Instance.Level).ToString();
            Value_MinimumHeight.text = fpPlatform.WelandObject.RuntimeMinimumHeight(FPLevel.Instance.Level).ToString();
            Value_Flags_InitiallyActive.SetIsOnWithoutNotify(fpPlatform.WelandObject.InitiallyActive);
            Value_Flags_InitiallyExtended.SetIsOnWithoutNotify(fpPlatform.WelandObject.InitiallyExtended);
            Value_Flags_IsLocked.SetIsOnWithoutNotify(fpPlatform.WelandObject.IsLocked);
            Value_Flags_IsPlayerControllable.SetIsOnWithoutNotify(fpPlatform.WelandObject.IsPlayerControllable);
            Value_Flags_IsMonsterControllable.SetIsOnWithoutNotify(fpPlatform.WelandObject.IsMonsterControllable);
            Value_Flags_CausesDamage.SetIsOnWithoutNotify(fpPlatform.WelandObject.CausesDamage);
            Value_Flags_ReversesWhenObstructed.SetIsOnWithoutNotify(fpPlatform.WelandObject.ReversesDirectionWhenObstructed);
            Value_Flags_DeactivatesAtEachLevel.SetIsOnWithoutNotify(fpPlatform.WelandObject.DeactivatesAtEachLevel);
            Value_Flags_DeactivatesAtInitialLevel.SetIsOnWithoutNotify(fpPlatform.WelandObject.DeactivatesAtInitialLevel);
            Value_Flags_ActivatesAdjacentOnActivation.SetIsOnWithoutNotify(fpPlatform.WelandObject.ActivatesAdjacentPlatformsWhenActivating);
            Value_Flags_ActivatesAdjacentOnDeactivation.SetIsOnWithoutNotify(fpPlatform.WelandObject.ActivatesAdjacentPlatformsWhenDeactivating);
            Value_Flags_DeactivatesAdjacentOnActivation.SetIsOnWithoutNotify(fpPlatform.WelandObject.DeactivatesAdjacentPlatformsWhenActivating);
            Value_Flags_DeactivatesAdjacentOnDeactivation.SetIsOnWithoutNotify(fpPlatform.WelandObject.DeactivatesAdjacentPlatformsWhenDeactivating);
            Value_Flags_ActivatesAdjacentAtEachLevel.SetIsOnWithoutNotify(fpPlatform.WelandObject.ActivatesAdjacantPlatformsAtEachLevel);
            Value_Flags_DelaysBeforeActivation.SetIsOnWithoutNotify(fpPlatform.WelandObject.DelaysBeforeActivation);
            Value_Flags_ActivatesOnlyOnce.SetIsOnWithoutNotify(fpPlatform.WelandObject.ActivatesOnlyOnce);
            Value_Flags_ActivatesLight.SetIsOnWithoutNotify(fpPlatform.WelandObject.ActivatesLight);
            Value_Flags_DeactivatesLight.SetIsOnWithoutNotify(fpPlatform.WelandObject.DeactivatesLight);
            Value_Flags_CannotBeExternallyDeactivated.SetIsOnWithoutNotify(fpPlatform.WelandObject.CannotBeExternallyDeactivated);
            Value_Flags_ContractsSlower.SetIsOnWithoutNotify(fpPlatform.WelandObject.ContractsSlower);
            Value_Flags_UsesNativePolygonHeights.SetIsOnWithoutNotify(fpPlatform.WelandObject.UsesNativePolygonHeights);
            Value_Flags_ExtendsFloorToCeiling.SetIsOnWithoutNotify(fpPlatform.WelandObject.ExtendsFloorToCeiling);
            Value_Flags_ComesFromFloor.SetIsOnWithoutNotify(fpPlatform.WelandObject.ComesFromFloor);
            Value_Flags_ComesFromCeiling.SetIsOnWithoutNotify(fpPlatform.WelandObject.ComesFromCeiling);
            Value_Flags_DoesNotActivateParent.SetIsOnWithoutNotify(fpPlatform.WelandObject.DoesNotActivateParent);
            Value_Flags_IsSecret.SetIsOnWithoutNotify(fpPlatform.WelandObject.IsSecret);
            Value_Flags_IsDoor.SetIsOnWithoutNotify(fpPlatform.WelandObject.IsDoor);

            Simulation_IsActive.onValueChanged.AddListener(delegate { fpPlatform.SetRuntimeActive(Simulation_IsActive.isOn); });
            fpPlatform.OnInspectionStateChange += OnInspectionStateChange;
            OnInspectionStateChange(fpPlatform);

            Simulation_Obstruct.onClick.AddListener(delegate { fpPlatform.ObstructRuntimeBehavior(); });
        }

        public override void UpdateValuesInInspectedObject()
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }

        public void PrepareForDestruction()
        {
            fpPlatform.OnInspectionStateChange -= OnInspectionStateChange;

            foreach (var fpPlatform in FPLevel.Instance.FPCeilingFpPlatforms.Values)
            {
                fpPlatform.BeginRuntimeStyleBehavior();
            }

            foreach (var fpPlatform in FPLevel.Instance.FPFloorFpPlatforms.Values)
            {
                fpPlatform.BeginRuntimeStyleBehavior();
            }
        }

        private void OnInspectionStateChange(FPPlatform platform)
        {
            // TODO: Make this update everything that is display - a full refresh.

            Simulation_IsActive.SetIsOnWithoutNotify(platform.IsRuntimeActive);
        }
    }
}
