using AlephOne;
using ForgePlus.Extensions;
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
            var staticWorld = level.Level.static_world;
            var entryPointFlags = (int)staticWorld.entry_point_flags;
            var missionFlags = staticWorld.mission_flags;
            var environmentFlags = staticWorld.environment_flags;

            Value_Name.text = level.Level.GetLevelName();
            Value_Environment.text = staticWorld.environment_code.ToString();
            // Aleph One picks the landscape with the song index (map.cpp: mark_map_collections)
            Value_Landscape.text = staticWorld.song_index.ToString();

            Value_Flags_SinglePlayer.SetIsOnWithoutNotify(csmacros.TEST_FLAG(entryPointFlags, map._single_player_entry_point));
            Value_Flags_Cooperative.SetIsOnWithoutNotify(csmacros.TEST_FLAG(entryPointFlags, map._multiplayer_cooperative_entry_point));
            Value_Flags_Carnage.SetIsOnWithoutNotify(csmacros.TEST_FLAG(entryPointFlags, map._multiplayer_carnage_entry_point));
            Value_Flags_KillTheOneWithTheBall.SetIsOnWithoutNotify(csmacros.TEST_FLAG(entryPointFlags, map._kill_the_man_with_the_ball_entry_point));
            Value_Flags_MonarchOfTheHill.SetIsOnWithoutNotify(csmacros.TEST_FLAG(entryPointFlags, map._king_of_hill_entry_point));
            Value_Flags_Defense.SetIsOnWithoutNotify(csmacros.TEST_FLAG(entryPointFlags, map._defense_entry_point));
            Value_Flags_Rugby.SetIsOnWithoutNotify(csmacros.TEST_FLAG(entryPointFlags, map._rugby_entry_point));
            Value_Flags_CaptureTheFlag.SetIsOnWithoutNotify(csmacros.TEST_FLAG(entryPointFlags, map._capture_the_flag_entry_point));

            Value_Flags_Vacuum.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_vacuum));
            Value_Flags_Magnetic.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_magnetic));
            Value_Flags_Rebellion.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_rebellion));
            Value_Flags_LowGravity.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_low_gravity));
            Value_Flags_Extermination.SetIsOnWithoutNotify(csmacros.TEST_FLAG(missionFlags, map._mission_extermination));
            Value_Flags_Exploration.SetIsOnWithoutNotify(csmacros.TEST_FLAG(missionFlags, map._mission_exploration));
            Value_Flags_Retrieval.SetIsOnWithoutNotify(csmacros.TEST_FLAG(missionFlags, map._mission_retrieval));
            Value_Flags_Repair.SetIsOnWithoutNotify(csmacros.TEST_FLAG(missionFlags, map._mission_repair));
            Value_Flags_Rescue.SetIsOnWithoutNotify(csmacros.TEST_FLAG(missionFlags, map._mission_rescue));

            Value_Flags_TerminalPause.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_terminals_stop_time));
            Value_Flags_M1_Rebellion.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_rebellion_m1));
            Value_Flags_M1_Exploration.SetIsOnWithoutNotify(csmacros.TEST_FLAG(missionFlags, map._mission_exploration_m1));
            Value_Flags_M1_Repair.SetIsOnWithoutNotify(csmacros.TEST_FLAG(missionFlags, map._mission_repair_m1));
            Value_Flags_M1_Rescue.SetIsOnWithoutNotify(csmacros.TEST_FLAG(missionFlags, map._mission_rescue_m1));
            Value_Flags_M1_Glue.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_glue_m1));
            Value_Flags_M1_Ouch.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_ouch_m1));
            Value_Flags_M1_HasMusic.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_song_index_m1));
            Value_Flags_M1_WeaponsStyle.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_m1_weapons));
            Value_Flags_M1_ActivationRange.SetIsOnWithoutNotify(csmacros.TEST_FLAG(environmentFlags, map._environment_activation_ranges));
        }
    }
}
