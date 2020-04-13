using ForgePlus.LevelManipulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Weland;

namespace ForgePlus.Inspection
{
    public class InspectorFPLevel : InspectorBase
    {
        public TextMeshProUGUI Value_Name;
        public TextMeshProUGUI Value_Environment;
        public TextMeshProUGUI Value_Landscape;
        public TextMeshProUGUI Value_Flags_EntryPoint;
        public TextMeshProUGUI Value_Flags_Environment;
        public TextMeshProUGUI Value_Flags_EnvironmentM1;

        public override void PopulateValues(IFPInspectable inspectedObject)
        {
            var fpLevel =               inspectedObject as FPLevel;

            Value_Name.text =                   fpLevel.Level.Name.ToString();
            Value_Environment.text =            fpLevel.Level.Environment.ToString();
            Value_Landscape.text =              fpLevel.Level.Landscape.ToString();

            Value_Flags_EntryPoint.text =       $"Single Player: {fpLevel.Level.SinglePlayer}\n" +
                                                $"Co-op: {fpLevel.Level.MultiplayerCooperative}\n" +
                                                $"Carnage: {fpLevel.Level.MultiplayerCarnage}\n" +
                                                $"Kill Man w/Ball: {fpLevel.Level.KillTheManWithTheBall}\n" +
                                                $"King of the Hill: {fpLevel.Level.KingOfTheHill}\n" +
                                                $"Defense: {fpLevel.Level.Defense}\n" +
                                                $"Rugby: {fpLevel.Level.Rugby}\n" +
                                                $"Capture the Flag: {fpLevel.Level.CaptureTheFlag}";

            Value_Flags_Environment.text =      $"Vacuum: {fpLevel.Level.Vacuum}\n" +
                                                $"Magnetic: {fpLevel.Level.Magnetic}\n" +
                                                $"Rebellion: {fpLevel.Level.Rebellion}\n" +
                                                $"LowGravity: {fpLevel.Level.LowGravity}\n" +
                                                $"Extermination: {fpLevel.Level.Extermination}\n" +
                                                $"Exploration: {fpLevel.Level.Exploration}\n" +
                                                $"Retrieval: {fpLevel.Level.Retrieval}\n" +
                                                $"Repair: {fpLevel.Level.Repair}\n" +
                                                $"Rescue: {fpLevel.Level.Rescue}";

            Value_Flags_EnvironmentM1.text =    $"Rebellion M1: {fpLevel.Level.Rebellion}\n" +
                                                $"Exploration M1: {fpLevel.Level.ExplorationM1}\n" +
                                                $"Repair M1: {fpLevel.Level.RepairM1}\n" +
                                                $"Rescue M1: {fpLevel.Level.RescueM1}\n" +
                                                $"Glue M1: {fpLevel.Level.GlueM1}\n" +
                                                $"Ouch M1: {fpLevel.Level.OuchM1}\n" +
                                                $"Song Index M1: {fpLevel.Level.SongIndexM1}\n" +
                                                $"M1 Weapons: {fpLevel.Level.M1Weapons}\n" +
                                                $"M1 Activation Range: {fpLevel.Level.M1ActivationRange}\n" +
                                                $"Terminal Pause: {fpLevel.Level.TerminalsStopTime}";
        }

        public override void UpdateValuesInInspectedObject(IFPInspectable inspectedObject)
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }
    }
}
