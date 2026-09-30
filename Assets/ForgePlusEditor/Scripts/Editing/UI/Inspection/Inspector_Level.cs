using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using RuntimeCore.Entities;
using System.Text;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    [AutoStaticsCleanup]
    public partial class Inspector_Level : Inspector_Base<LevelEntity_Level>
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

        // Which walls and scenery collections the level uses (map.cpp: Environments)
        [CreateProperty]
        public string Environment
        {
            get
            {
                var code = StaticWorld.environment_code;

                return code >= 0 && code < EnvironmentNames.Length ? $"{EnvironmentNames[code]} ({code})" : code.ToString();
            }
        }

        [CreateProperty]
        public string PhysicsModel
        {
            get
            {
                return StaticWorld.physics_model.ToString();
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

        // ---------- Physics: the physics chunks saved in the level (which Aleph One uses instead of the physics file)

        [CreateProperty]
        public string PhysicsInUse
        {
            get
            {
                switch (PhysicsLoading.Instance.Source)
                {
                    case PhysicsModelSource.EmbeddedInLevel:
                        return "The level's";
                    case PhysicsModelSource.PhysicsFile:
                    case PhysicsModelSource.Marathon1PhysicsFile:
                        return "The physics file's";
                    default:
                        return "The engine's defaults";
                }
            }
        }

        [CreateProperty]
        public string EmbeddedMonsters
        {
            get { return ChunkCount(tags.MONSTER_PHYSICS_TAG, monsters.SIZEOF_monster_definition, "definitions"); }
        }

        [CreateProperty]
        public string EmbeddedEffects
        {
            get { return ChunkCount(tags.EFFECTS_PHYSICS_TAG, effects.SIZEOF_effect_definition, "definitions"); }
        }

        [CreateProperty]
        public string EmbeddedProjectiles
        {
            get { return ChunkCount(tags.PROJECTILE_PHYSICS_TAG, projectiles.SIZEOF_projectile_definition, "definitions"); }
        }

        [CreateProperty]
        public string EmbeddedPhysicsConstants
        {
            get { return ChunkCount(tags.PHYSICS_PHYSICS_TAG, player.SIZEOF_physics_constants, "sets"); }
        }

        [CreateProperty]
        public string EmbeddedWeapons
        {
            get { return ChunkCount(tags.WEAPONS_PHYSICS_TAG, weapons.SIZEOF_weapon_definition, "definitions"); }
        }

        // ---------- The chunks ForgePlus keeps (and saves) as they were loaded, without reading them

        [CreateProperty]
        public string ShapePatchSize
        {
            get { return ChunkSize(tags.SHAPE_PATCH_TAG); }
        }

        [CreateProperty]
        public string SoundPatchSize
        {
            get { return ChunkSize(tags.SOUND_PATCH_TAG); }
        }

        [CreateProperty]
        public string MmlSize
        {
            get { return ChunkSize(tags.MMLS_TAG); }
        }

        [CreateProperty]
        public string MmlText
        {
            get { return ChunkText(tags.MMLS_TAG); }
        }

        [CreateProperty]
        public string LuaSize
        {
            get { return ChunkSize(tags.LUAS_TAG); }
        }

        [CreateProperty]
        public string LuaText
        {
            get { return ChunkText(tags.LUAS_TAG); }
        }

        // The tab last shown, which the next level inspector shows too
        private static int selectedTab;

        private static readonly string[] TabNames = { "tab-info", "tab-flags", "tab-physics", "tab-shape-patches", "tab-sound-patches", "tab-mml", "tab-lua" };

        // Longer text than this is cut off (a text element can only draw so many characters)
        private const int MaximumTextLength = 12000;

        private static readonly string[] EnvironmentNames = { "Lh'owon Water", "Lh'owon Lava", "Lh'owon Sewage", "Jjaro", "Pfhor" };

        protected override void OnLoaded()
        {
            base.OnLoaded();

            var tabs = Root.Q<RadioButtonGroup>("tabs");
            tabs.SetValueWithoutNotify(selectedTab);
            tabs.RegisterValueChangedCallback(changeEvent => ShowTab(changeEvent.newValue));

            Root.Query<Label>(className: "fp-inspector-text-block__text").ForEach(text => text.selection.isSelectable = true);

            ShowTab(selectedTab);
        }

        private void ShowTab(int tab)
        {
            selectedTab = tab;

            for (var i = 0; i < TabNames.Length; i++)
            {
                Root.Q(TabNames[i]).style.display = i == tab ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private byte[] GetChunk(uint tag)
        {
            return Entity.Level.loaded_wad.preserved_chunks.TryGetValue(tag, out var chunk) ? chunk : null;
        }

        private string ChunkSize(uint tag)
        {
            var chunk = GetChunk(tag);

            return chunk != null ? $"{chunk.Length:N0} bytes" : "None";
        }

        private string ChunkCount(uint tag, int unitSize, string units)
        {
            var chunk = GetChunk(tag);

            return chunk != null ? $"{chunk.Length / unitSize} {units}" : "None";
        }

        // MML and Lua are text (UTF-8, which is also plain ASCII), maybe with a terminating NUL
        private string ChunkText(uint tag)
        {
            var chunk = GetChunk(tag);
            if (chunk == null)
            {
                return string.Empty;
            }

            var text = Encoding.UTF8.GetString(chunk).TrimEnd('\0');

            return text.Length > MaximumTextLength ?
                   $"{text.Substring(0, MaximumTextLength)}\n\n(… and {text.Length - MaximumTextLength:N0} more characters)" :
                   text;
        }
    }
}
