using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_Level : Inspector_Base<LevelEntity_Level>
    {
        public Inspector_Level(LevelEntity_Level level) : base(level)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Level";
            }
        }

        private static_data StaticWorld
        {
            get
            {
                return Entity.Level.static_world;
            }
        }

        private int EntryPointFlags
        {
            get
            {
                return (int)StaticWorld.entry_point_flags;
            }
        }

        [CreateProperty]
        public string Name
        {
            get
            {
                return Entity.Level.GetLevelName();
            }
        }

        [CreateProperty]
        public string Environment
        {
            get
            {
                return StaticWorld.environment_code.ToString();
            }
        }

        // Aleph One picks the landscape with the song index (map.cpp: mark_map_collections)
        [CreateProperty]
        public string Landscape
        {
            get
            {
                return StaticWorld.song_index.ToString();
            }
        }

        [CreateProperty]
        public bool SinglePlayer
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._single_player_entry_point);
            }
        }

        [CreateProperty]
        public bool Cooperative
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._multiplayer_cooperative_entry_point);
            }
        }

        [CreateProperty]
        public bool Carnage
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._multiplayer_carnage_entry_point);
            }
        }

        [CreateProperty]
        public bool KillTheOneWithTheBall
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._kill_the_man_with_the_ball_entry_point);
            }
        }

        [CreateProperty]
        public bool MonarchOfTheHill
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._king_of_hill_entry_point);
            }
        }

        [CreateProperty]
        public bool Defense
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._defense_entry_point);
            }
        }

        [CreateProperty]
        public bool Rugby
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._rugby_entry_point);
            }
        }

        [CreateProperty]
        public bool CaptureTheFlag
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._capture_the_flag_entry_point);
            }
        }

        [CreateProperty]
        public bool Vacuum
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_vacuum);
            }
        }

        [CreateProperty]
        public bool Magnetic
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_magnetic);
            }
        }

        [CreateProperty]
        public bool Rebellion
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_rebellion);
            }
        }

        [CreateProperty]
        public bool LowGravity
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_low_gravity);
            }
        }

        [CreateProperty]
        public bool Extermination
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_extermination);
            }
        }

        [CreateProperty]
        public bool Exploration
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_exploration);
            }
        }

        [CreateProperty]
        public bool Retrieval
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_retrieval);
            }
        }

        [CreateProperty]
        public bool Repair
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_repair);
            }
        }

        [CreateProperty]
        public bool Rescue
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_rescue);
            }
        }

        [CreateProperty]
        public bool TerminalPause
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_terminals_stop_time);
            }
        }

        [CreateProperty]
        public bool M1Rebellion
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_rebellion_m1);
            }
        }

        [CreateProperty]
        public bool M1Exploration
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_exploration_m1);
            }
        }

        [CreateProperty]
        public bool M1Repair
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_repair_m1);
            }
        }

        [CreateProperty]
        public bool M1Rescue
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_rescue_m1);
            }
        }

        [CreateProperty]
        public bool M1Glue
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_glue_m1);
            }
        }

        [CreateProperty]
        public bool M1Ouch
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_ouch_m1);
            }
        }

        [CreateProperty]
        public bool M1HasMusic
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_song_index_m1);
            }
        }

        [CreateProperty]
        public bool M1WeaponsStyle
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_m1_weapons);
            }
        }

        [CreateProperty]
        public bool M1ActivationRange
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_activation_ranges);
            }
        }
    }
}
