using RuntimeCore.Entities;
using TMPro;
using UnityEngine.UI;

namespace ForgePlus.Inspection
{
    public class Inspector_Level : Inspector_Base
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

        public override void RefreshValuesInInspector()
        {
            var level = inspectedObject as LevelEntity_Level;

            Value_Name.text = level.Level.Name.ToString();
            Value_Environment.text = level.Level.Environment.ToString();
            Value_Landscape.text = level.Level.Landscape.ToString();

            Value_Flags_SinglePlayer.SetIsOnWithoutNotify(level.Level.SinglePlayer);
            Value_Flags_Cooperative.SetIsOnWithoutNotify(level.Level.MultiplayerCooperative);
            Value_Flags_Carnage.SetIsOnWithoutNotify(level.Level.MultiplayerCarnage);
            Value_Flags_KillTheOneWithTheBall.SetIsOnWithoutNotify(level.Level.KillTheManWithTheBall);
            Value_Flags_MonarchOfTheHill.SetIsOnWithoutNotify(level.Level.KingOfTheHill);
            Value_Flags_Defense.SetIsOnWithoutNotify(level.Level.Defense);
            Value_Flags_Rugby.SetIsOnWithoutNotify(level.Level.Rugby);
            Value_Flags_CaptureTheFlag.SetIsOnWithoutNotify(level.Level.CaptureTheFlag);

            Value_Flags_Vacuum.SetIsOnWithoutNotify(level.Level.Vacuum);
            Value_Flags_Magnetic.SetIsOnWithoutNotify(level.Level.Magnetic);
            Value_Flags_Rebellion.SetIsOnWithoutNotify(level.Level.Rebellion);
            Value_Flags_LowGravity.SetIsOnWithoutNotify(level.Level.LowGravity);
            Value_Flags_Extermination.SetIsOnWithoutNotify(level.Level.Extermination);
            Value_Flags_Exploration.SetIsOnWithoutNotify(level.Level.Exploration);
            Value_Flags_Retrieval.SetIsOnWithoutNotify(level.Level.Retrieval);
            Value_Flags_Repair.SetIsOnWithoutNotify(level.Level.Repair);
            Value_Flags_Rescue.SetIsOnWithoutNotify(level.Level.Rescue);

            Value_Flags_TerminalPause.SetIsOnWithoutNotify(level.Level.TerminalsStopTime);
            Value_Flags_M1_Rebellion.SetIsOnWithoutNotify(level.Level.RebellionM1);
            Value_Flags_M1_Exploration.SetIsOnWithoutNotify(level.Level.ExplorationM1);
            Value_Flags_M1_Repair.SetIsOnWithoutNotify(level.Level.RepairM1);
            Value_Flags_M1_Rescue.SetIsOnWithoutNotify(level.Level.RescueM1);
            Value_Flags_M1_Glue.SetIsOnWithoutNotify(level.Level.GlueM1);
            Value_Flags_M1_Ouch.SetIsOnWithoutNotify(level.Level.OuchM1);
            Value_Flags_M1_HasMusic.SetIsOnWithoutNotify(level.Level.SongIndexM1);
            Value_Flags_M1_WeaponsStyle.SetIsOnWithoutNotify(level.Level.M1Weapons);
            Value_Flags_M1_ActivationRange.SetIsOnWithoutNotify(level.Level.M1ActivationRange);
        }
    }
}
