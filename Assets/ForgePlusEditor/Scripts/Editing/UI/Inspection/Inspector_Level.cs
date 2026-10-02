using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using ForgePlus.UI;
using RuntimeCore.Entities;
using System.Linq;
using System.Text;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
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

                var name = EnvironmentName(code);

                return name != null ? Strings.Get(Strings.Level, "Inspector.Level.Environment.Value", name, code) : code.ToString();
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

        // ---------- Physics: whether the level has physics saved in it (which Marathon Infinity and Aleph One use instead
        // of the physics file, and which ForgePlus uses for its sprites)

        private static readonly uint[] PhysicsTags =
        {
            tags.MONSTER_PHYSICS_TAG, tags.EFFECTS_PHYSICS_TAG, tags.PROJECTILE_PHYSICS_TAG, tags.PHYSICS_PHYSICS_TAG,
            tags.WEAPONS_PHYSICS_TAG,
        };

        [CreateProperty]
        public string EmbeddedPhysics
        {
            get
            {
                foreach (var tag in PhysicsTags)
                {
                    var chunk = GetChunk(tag);
                    if (chunk != null && chunk.Length > 0)
                    {
                        return Strings.Get(Strings.Level, "Inspector.Level.EmbeddedPhysics.Yes");
                    }
                }

                return Strings.Get(Strings.Level, "Inspector.Level.EmbeddedPhysics.No");
            }
        }

        [CreateProperty]
        public string PhysicsInUse
        {
            get
            {
                switch (PhysicsLoading.Instance.Source)
                {
                    case PhysicsModelSource.EmbeddedInLevel:
                        return Strings.Get(Strings.Level, "Inspector.Level.PhysicsInUse.Level");
                    case PhysicsModelSource.PhysicsFile:
                    case PhysicsModelSource.Marathon1PhysicsFile:
                        return Strings.Get(Strings.Level, "Inspector.Level.PhysicsInUse.PhysicsFile");
                    default:
                        return Strings.Get(Strings.Level, "Inspector.Level.PhysicsInUse.EngineDefaults");
                }
            }
        }

        // ---------- Chapter screen: the picture Aleph One shows as the level starts, from the map's resources (see
        // MapsFile.ChapterScreenPictureId)

        private static MapsFile MapsFile
        {
            get
            {
                return MapsLoading.Instance.MapsFile;
            }
        }

        private static int LevelIndex
        {
            get
            {
                return MapsLoading.Instance.OpenLevelIndex;
            }
        }

        private static short ChapterScreenId
        {
            get
            {
                return MapsFile != null && LevelIndex >= 0 ? MapsFile.ChapterScreenPictureId(LevelIndex) : (short) -1;
            }
        }

        // Turning it off leaves it out of the saved map, so a chapter screen can be turned off only while the map has one
        // (and the resources can be changed)
        [CreateProperty]
        public bool ShowChapterScreen
        {
            get
            {
                return ChapterScreenId >= 0 && MapsFile.IsChapterScreenShown(LevelIndex);
            }
            set
            {
                if (ChapterScreenId >= 0)
                {
                    MapsFile.SetChapterScreenShown(LevelIndex, value);
                    RefreshValuesInInspector();
                    ShowChapterScreenPreview();
                }
            }
        }

        [CreateProperty]
        public bool IsShowChapterScreenEditable
        {
            get
            {
                return ChapterScreenId >= 0 && MapsFile.Forks.CanEditResources;
            }
        }

        // Its resource ID, and which bit depth's version it is (the deepest the map has)
        [CreateProperty]
        public string ChapterScreenPicture
        {
            get
            {
                var id = ChapterScreenId;
                if (id < 0)
                {
                    return Strings.Get(Strings.Level, "Inspector.Level.None");
                }

                var depthOffset = id - 1500 - LevelIndex;
                var depth = depthOffset >= 20000 ? 32 : depthOffset >= 10000 ? 16 : 8;

                return Strings.Get(Strings.Level, "Inspector.Level.ChapterScreen.Picture", id, depth);
            }
        }

        // ---------- Sounds: how many ambient and random sounds the level has, which its polygons play (Sounds mode edits them)

        [CreateProperty]
        public string AmbientSoundCount
        {
            get
            {
                return Entity.Level.AmbientSoundImageList.Count.ToString();
            }
        }

        [CreateProperty]
        public string RandomSoundCount
        {
            get
            {
                return Entity.Level.RandomSoundImageList.Count.ToString();
            }
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

        private static readonly string[] TabNames = { "tab-info", "tab-flags", "tab-chapter-screen", "tab-sounds", "tab-shape-patches", "tab-sound-patches", "tab-mml", "tab-lua" };

        // Longer text than this is cut off (a text element can only draw so many characters)
        private const int MaximumTextLength = 12000;

        // The environment's name, or null for an unknown code
        private static string EnvironmentName(int code)
        {
            switch (code)
            {
                case 0:
                    return Strings.Get(Strings.Level, "Inspector.Level.Environment.LhowonWater");
                case 1:
                    return Strings.Get(Strings.Level, "Inspector.Level.Environment.LhowonLava");
                case 2:
                    return Strings.Get(Strings.Level, "Inspector.Level.Environment.LhowonSewage");
                case 3:
                    return Strings.Get(Strings.Level, "Inspector.Level.Environment.Jjaro");
                case 4:
                    return Strings.Get(Strings.Level, "Inspector.Level.Environment.Pfhor");
                default:
                    return null;
            }
        }

        private VisualElement chapterScreenPreview;
        private float chapterScreenAspect;

        protected override void OnLoaded()
        {
            base.OnLoaded();

            var tabs = Root.Q<RadioButtonGroup>("tabs");
            tabs.SetValueWithoutNotify(selectedTab);
            tabs.RegisterValueChangedCallback(changeEvent => ShowTab(changeEvent.newValue));

            Root.Query<Label>(className: "fp-inspector-text-block__text").ForEach(text => text.selection.isSelectable = true);

            // As wide as the inspector, and as tall as its picture's shape makes it
            chapterScreenPreview = Root.Q("chapter-screen-preview");
            chapterScreenPreview.RegisterCallback<GeometryChangedEvent>(geometryChangedEvent => FitChapterScreenPreview());
            ShowChapterScreenPreview();

            ShowTab(selectedTab);

            // Saving reloads the map file, which no longer has a chapter screen that was turned off
            MapsLoading.Instance.OnSaveCompleted += OnSaveCompleted;
        }

        protected override void OnUnloading()
        {
            base.OnUnloading();

            MapsLoading.Instance.OnSaveCompleted -= OnSaveCompleted;
        }

        private void OnSaveCompleted()
        {
            RefreshValuesInInspector();
            ShowChapterScreenPreview();
        }

        private void ShowTab(int tab)
        {
            selectedTab = tab;

            for (var i = 0; i < TabNames.Length; i++)
            {
                Root.Q(TabNames[i]).style.display = i == tab ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // The chapter screen (from the map's resources, as Aleph One shows it at 32 bits), dimmed while it's turned off
        private void ShowChapterScreenPreview()
        {
            var picture = ChapterScreenId >= 0 ? ScenarioPictures.Get((short) (1500 + LevelIndex)) : null;
            var texture = picture?.Texture;

            if (!texture)
            {
                chapterScreenPreview.style.display = DisplayStyle.None;
                return;
            }

            chapterScreenAspect = (float) texture.height / texture.width;
            chapterScreenPreview.style.backgroundImage = texture;
            chapterScreenPreview.style.display = DisplayStyle.Flex;
            chapterScreenPreview.EnableInClassList("fp-chapter-screen--off", !ShowChapterScreen);

            FitChapterScreenPreview();
        }

        private void FitChapterScreenPreview()
        {
            var height = chapterScreenPreview.resolvedStyle.width * chapterScreenAspect;
            if (!float.IsNaN(height) && Mathf.Abs(chapterScreenPreview.resolvedStyle.height - height) > 0.5f)
            {
                chapterScreenPreview.style.height = height;
            }
        }

        private byte[] GetChunk(uint tag)
        {
            return Entity.Level.loaded_wad.preserved_chunks.TryGetValue(tag, out var chunk) ? chunk : null;
        }

        private string ChunkSize(uint tag)
        {
            var chunk = GetChunk(tag);

            return chunk != null ? Strings.Get(Strings.Level, "Inspector.Level.Bytes", chunk.Length.ToString("N0")) : Strings.Get(Strings.Level, "Inspector.Level.None");
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
                   Strings.Get(Strings.Level, "Inspector.Level.TextCutOff", text.Substring(0, MaximumTextLength), (text.Length - MaximumTextLength).ToString("N0")) :
                   text;
        }
    }
}
