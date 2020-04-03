using ForgePlus.LevelManipulation;
using ForgePlus.ShapesCollections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Weland;

namespace ForgePlus.Inspection
{
    public class InspectorFPSide : InspectorBase
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_LineIndex;
        public TextMeshProUGUI Value_PolygonIndex;
        public TextMeshProUGUI Value_Flags;
        public TextMeshProUGUI Value_ControlPanelType;
        public TextMeshProUGUI Value_ControlPanelPermutation;
        public TextMeshProUGUI Value_AmbientDelta;

        public RawImage Value_Primary_Texture;
        public TextMeshProUGUI Value_Primary_Offset;
        public TextMeshProUGUI Value_Primary_LightIndex;
        public TextMeshProUGUI Value_Primary_TransferMode;

        public RawImage Value_Secondary_Texture;
        public TextMeshProUGUI Value_Secondary_Offset;
        public TextMeshProUGUI Value_Secondary_LightIndex;
        public TextMeshProUGUI Value_Secondary_TransferMode;

        public RawImage Value_Transparent_Texture;
        public TextMeshProUGUI Value_Transparent_Offset;
        public TextMeshProUGUI Value_Transparent_LightIndex;
        public TextMeshProUGUI Value_Transparent_TransferMode;

        public override void PopulateValues(IFPInspectable inspectedObject)
        {
            var fpLine = inspectedObject as FPSide;

            Value_Id.text =                         fpLine.Index.ToString();
            Value_Type.text =                       fpLine.WelandObject.Type.ToString();
            Value_LineIndex.text =                  fpLine.WelandObject.LineIndex.ToString();
            Value_PolygonIndex.text =               fpLine.WelandObject.PolygonIndex.ToString();

            var flagsValue =                        $"Panel Active: {(fpLine.WelandObject.Flags & SideFlags.ControlPanelStatus) != 0}\n" +
                                                    $"Control Panel: {(fpLine.WelandObject.Flags & SideFlags.IsControlPanel) != 0}\n" +
                                                    $"Repair Switch: {(fpLine.WelandObject.Flags & SideFlags.IsRepairSwitch) != 0}\n" +
                                                    $"Destructive Switch: {(fpLine.WelandObject.Flags & SideFlags.IsDestructiveSwitch) != 0}\n" +
                                                    $"Lighted Switch: {(fpLine.WelandObject.Flags & SideFlags.IsLightedSwitch) != 0}\n" +
                                                    $"Can Be Destroyed: {(fpLine.WelandObject.Flags & SideFlags.SwitchCanBeDestroyed) != 0}\n" +
                                                    $"Projectiles Only: {(fpLine.WelandObject.Flags & SideFlags.SwitchCanOnlyBeHitByProjectiles) != 0}\n" +
                                                    $"Dirty: {(fpLine.WelandObject.Flags & SideFlags.Dirty) != 0}";

            Value_ControlPanelType.text =           fpLine.WelandObject.ControlPanelType.ToString();
            Value_ControlPanelPermutation.text =    fpLine.WelandObject.ControlPanelPermutation.ToString();
            Value_AmbientDelta.text =               fpLine.WelandObject.AmbientDelta.ToString();

            var hasPrimaryData =                    (ushort)fpLine.WelandObject.Primary.Texture != ushort.MaxValue;
            Value_Primary_Texture.texture =         hasPrimaryData ? WallsCollection.GetTexture(fpLine.WelandObject.Primary.Texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Primary_Offset.text =             hasPrimaryData ? $"X: {fpLine.WelandObject.Primary.X}\nY: {fpLine.WelandObject.Primary.Y}" : "X: -\nY: -";
            Value_Primary_LightIndex.text =         hasPrimaryData ? fpLine.WelandObject.PrimaryLightsourceIndex.ToString() : "-";
            Value_Primary_TransferMode.text =       hasPrimaryData ? fpLine.WelandObject.PrimaryTransferMode.ToString() : "-";

            var hasSecondaryData =                  (ushort)fpLine.WelandObject.Secondary.Texture != ushort.MaxValue;
            Value_Secondary_Texture.texture =       hasSecondaryData ? WallsCollection.GetTexture(fpLine.WelandObject.Secondary.Texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Secondary_Offset.text =           hasSecondaryData ? $"X: {fpLine.WelandObject.Secondary.X}\nY: {fpLine.WelandObject.Secondary.Y}" : "X: -\nY: -";
            Value_Secondary_LightIndex.text =       hasSecondaryData ? fpLine.WelandObject.SecondaryLightsourceIndex.ToString() : "-";
            Value_Secondary_TransferMode.text =     hasSecondaryData ? fpLine.WelandObject.SecondaryTransferMode.ToString() : "-";

            var hasTransparentData =                (ushort)fpLine.WelandObject.Transparent.Texture != ushort.MaxValue;
            Value_Transparent_Texture.texture =     hasTransparentData ? WallsCollection.GetTexture(fpLine.WelandObject.Transparent.Texture) : Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder");
            Value_Transparent_Offset.text =         hasTransparentData ? $"X: {fpLine.WelandObject.Transparent.X}\nY: {fpLine.WelandObject.Transparent.Y}" : "X: -\nY: -";
            Value_Transparent_LightIndex.text =     hasTransparentData ? fpLine.WelandObject.TransparentLightsourceIndex.ToString() : "-";
            Value_Transparent_TransferMode.text =   hasTransparentData ? fpLine.WelandObject.TransparentTransferMode.ToString() : "-";
        }

        public override void UpdateValuesInInspectedObject(IFPInspectable inspectedObject)
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject (casted to FPMapObject in this case)
            throw new System.NotImplementedException();
        }
    }
}
