using ForgePlus.LevelManipulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Weland;
using UnityEngine.UI;

namespace ForgePlus.Inspection
{
    public class InspectorFPLevel : InspectorBase
    {
        public TextMeshProUGUI Value_Name;
        public TextMeshProUGUI Value_Environment;
        public TextMeshProUGUI Value_Landscape;

        public Toggle Value_Flags_SinglePlayer;
        public Toggle Value_Flags_Cooperative;
        public Toggle Value_Flags_Carnage;
        public Toggle Value_Flags_KillTheOneWithTheBall;
        public Toggle Value_Flags_MonarchOfTheHill;
        public Toggle Value_Flags_Defense;
        public Toggle Value_Flags_Rugby;
        public Toggle Value_Flags_CaptureTheFlag;

        public Toggle Value_Flags_Vacuum;
        public Toggle Value_Flags_Magnetic;
        public Toggle Value_Flags_Rebellion;
        public Toggle Value_Flags_LowGravity;
        public Toggle Value_Flags_Extermination;
        public Toggle Value_Flags_Exploration;
        public Toggle Value_Flags_Retrieval;
        public Toggle Value_Flags_Repair;
        public Toggle Value_Flags_Rescue;

        public Toggle Value_Flags_TerminalPause;
        public Toggle Value_Flags_M1_Rebellion;
        public Toggle Value_Flags_M1_Exploration;
        public Toggle Value_Flags_M1_Repair;
        public Toggle Value_Flags_M1_Rescue;
        public Toggle Value_Flags_M1_Glue;
        public Toggle Value_Flags_M1_Ouch;
        public Toggle Value_Flags_M1_HasMusic;
        public Toggle Value_Flags_M1_WeaponsStyle;
        public Toggle Value_Flags_M1_ActivationRange;

        public override void PopulateValues(IFPInspectable inspectedObject)
        {
            var fpLevel = inspectedObject as FPLevel;

            Value_Name.text = fpLevel.Level.Name.ToString();
            Value_Environment.text = fpLevel.Level.Environment.ToString();
            Value_Landscape.text = fpLevel.Level.Landscape.ToString();

            Value_Flags_SinglePlayer.SetIsOnWithoutNotify(fpLevel.Level.SinglePlayer);
            Value_Flags_Cooperative.SetIsOnWithoutNotify(fpLevel.Level.MultiplayerCooperative);
            Value_Flags_Carnage.SetIsOnWithoutNotify(fpLevel.Level.MultiplayerCarnage);
            Value_Flags_KillTheOneWithTheBall.SetIsOnWithoutNotify(fpLevel.Level.KillTheManWithTheBall);
            Value_Flags_MonarchOfTheHill.SetIsOnWithoutNotify(fpLevel.Level.KingOfTheHill);
            Value_Flags_Defense.SetIsOnWithoutNotify(fpLevel.Level.Defense);
            Value_Flags_Rugby.SetIsOnWithoutNotify(fpLevel.Level.Rugby);
            Value_Flags_CaptureTheFlag.SetIsOnWithoutNotify(fpLevel.Level.CaptureTheFlag);

            Value_Flags_Vacuum.SetIsOnWithoutNotify(fpLevel.Level.Vacuum);
            Value_Flags_Magnetic.SetIsOnWithoutNotify(fpLevel.Level.Magnetic);
            Value_Flags_Rebellion.SetIsOnWithoutNotify(fpLevel.Level.Rebellion);
            Value_Flags_LowGravity.SetIsOnWithoutNotify(fpLevel.Level.LowGravity);
            Value_Flags_Extermination.SetIsOnWithoutNotify(fpLevel.Level.Extermination);
            Value_Flags_Exploration.SetIsOnWithoutNotify(fpLevel.Level.Exploration);
            Value_Flags_Retrieval.SetIsOnWithoutNotify(fpLevel.Level.Retrieval);
            Value_Flags_Repair.SetIsOnWithoutNotify(fpLevel.Level.Repair);
            Value_Flags_Rescue.SetIsOnWithoutNotify(fpLevel.Level.Rescue);

            Value_Flags_TerminalPause.SetIsOnWithoutNotify(fpLevel.Level.TerminalsStopTime);
            Value_Flags_M1_Rebellion.SetIsOnWithoutNotify(fpLevel.Level.RebellionM1);
            Value_Flags_M1_Exploration.SetIsOnWithoutNotify(fpLevel.Level.ExplorationM1);
            Value_Flags_M1_Repair.SetIsOnWithoutNotify(fpLevel.Level.RepairM1);
            Value_Flags_M1_Rescue.SetIsOnWithoutNotify(fpLevel.Level.RescueM1);
            Value_Flags_M1_Glue.SetIsOnWithoutNotify(fpLevel.Level.GlueM1);
            Value_Flags_M1_Ouch.SetIsOnWithoutNotify(fpLevel.Level.OuchM1);
            Value_Flags_M1_HasMusic.SetIsOnWithoutNotify(fpLevel.Level.SongIndexM1);
            Value_Flags_M1_WeaponsStyle.SetIsOnWithoutNotify(fpLevel.Level.M1Weapons);
            Value_Flags_M1_ActivationRange.SetIsOnWithoutNotify(fpLevel.Level.M1ActivationRange);
        }

        public override void UpdateValuesInInspectedObject(IFPInspectable inspectedObject)
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }
    }
}
