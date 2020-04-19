using ForgePlus.LevelManipulation;
using TMPro;

namespace ForgePlus.Inspection
{
    public class InspectorFPPlatform : InspectorBase
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Tag;
        public TextMeshProUGUI Value_PolygonIndex;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_Speed;
        public TextMeshProUGUI Value_Delay;
        public TextMeshProUGUI Value_MaximumHeight;
        public TextMeshProUGUI Value_MinimumHeight;
        public TextMeshProUGUI Value_Flags;



        public override void PopulateValues(IFPInspectable inspectedObject)
        {
            var fpPlatform = inspectedObject as FPPlatform;

            Value_Id.text = fpPlatform.Index.ToString();
            Value_Tag.text = fpPlatform.WelandObject.Tag.ToString();
            Value_PolygonIndex.text = fpPlatform.WelandObject.PolygonIndex.ToString();
            Value_Type.text = fpPlatform.WelandObject.Type.ToString();
            Value_Speed.text = fpPlatform.WelandObject.Speed.ToString();
            Value_Delay.text = fpPlatform.WelandObject.Delay.ToString();
            Value_MaximumHeight.text = fpPlatform.WelandObject.MaximumHeight.ToString();
            Value_MinimumHeight.text = fpPlatform.WelandObject.MinimumHeight.ToString();

            Value_Flags.text = $"Initially Active: {fpPlatform.WelandObject.InitiallyActive}\n" +
                               $"Initially Extended: {fpPlatform.WelandObject.InitiallyExtended}\n" +
                               $"Deactivates At Each Level: {fpPlatform.WelandObject.DeactivatesAtEachLevel}\n" +
                               $"Deactivates At Initial Level: {fpPlatform.WelandObject.DeactivatesAtInitialLevel}\n" +
                               $"Activates Adjacent Platforms When Deactivating: {fpPlatform.WelandObject.ActivatesAdjacentPlatformsWhenDeactivating}\n" +
                               $"Extends Floor To Ceiling: {fpPlatform.WelandObject.ExtendsFloorToCeiling}\n" +
                               $"Comes From Floor: {fpPlatform.WelandObject.ComesFromFloor}\n" +
                               $"Comes From Ceiling: {fpPlatform.WelandObject.ComesFromCeiling}\n" +
                               $"Causes Damage: {fpPlatform.WelandObject.CausesDamage}\n" +
                               $"Does Not Activate Parent: {fpPlatform.WelandObject.DoesNotActivateParent}\n" +
                               $"Activates Only Once: {fpPlatform.WelandObject.ActivatesOnlyOnce}\n" +
                               $"Activates Light: {fpPlatform.WelandObject.ActivatesLight}\n" +
                               $"Deactivates Light: {fpPlatform.WelandObject.DeactivatesLight}\n" +
                               $"Is Player Controllable: {fpPlatform.WelandObject.IsPlayerControllable}\n" +
                               $"Is Monster Controllable: {fpPlatform.WelandObject.IsMonsterControllable}\n" +
                               $"Reverses Direction When Obstructed: {fpPlatform.WelandObject.ReversesDirectionWhenObstructed}\n" +
                               $"Cannot Be Externally Deactivated: {fpPlatform.WelandObject.CannotBeExternallyDeactivated}\n" +
                               $"Uses Native Polygon Heights: {fpPlatform.WelandObject.UsesNativePolygonHeights}\n" +
                               $"Delays Before Activation: {fpPlatform.WelandObject.DelaysBeforeActivation}\n" +
                               $"Activates Adjacent Platforms When Activating: {fpPlatform.WelandObject.ActivatesAdjacentPlatformsWhenActivating}\n" +
                               $"Deactivates Adjacent Platforms When Activating: {fpPlatform.WelandObject.DeactivatesAdjacentPlatformsWhenActivating}\n" +
                               $"Deactivates Adjacent Platforms When Deactivating: {fpPlatform.WelandObject.DeactivatesAdjacentPlatformsWhenDeactivating}\n" +
                               $"Contracts Slower: {fpPlatform.WelandObject.ContractsSlower}\n" +
                               $"Activates Adjacant Platforms At Each Level: {fpPlatform.WelandObject.ActivatesAdjacantPlatformsAtEachLevel}\n" +
                               $"Is Locked: {fpPlatform.WelandObject.IsLocked}\n" +
                               $"Is Secret: {fpPlatform.WelandObject.IsSecret}\n" +
                               $"Is Door: {fpPlatform.WelandObject.IsDoor}";
        }

        public override void UpdateValuesInInspectedObject(IFPInspectable inspectedObject)
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }
    }
}
