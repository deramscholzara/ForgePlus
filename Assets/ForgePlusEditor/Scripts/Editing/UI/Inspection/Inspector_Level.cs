using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using ForgePlus.UI;
using RuntimeCore.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    [AutoStaticsCleanup]
    public partial class Inspector_Level : Inspector_Base<LevelEntity_Level>
    {
        // Landscape collections 1 to 4 (_collection_landscape1 to _collection_landscape4)
        private const short NumberOfLandscapes = 4;

        private static readonly uint[] PhysicsTags =
        {
            tags.MONSTER_PHYSICS_TAG, tags.EFFECTS_PHYSICS_TAG, tags.PROJECTILE_PHYSICS_TAG, tags.PHYSICS_PHYSICS_TAG,
            tags.WEAPONS_PHYSICS_TAG,
        };

        private static readonly string[] TabNames = { "tab-info", "tab-flags", "tab-chapter-screen", "tab-sounds", "tab-shape-patches", "tab-sound-patches", "tab-mml", "tab-lua" };

        // The tab last shown, which the next level inspector shows too
        private static int selectedTab;

        private VisualElement chapterScreenPreview;
        private float chapterScreenAspect;

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
                return (int) StaticWorld.entry_point_flags;
            }
        }

        // Saved in the level and the map's directory of levels, where a level with no name ends the list (so it can't be
        // emptied)
        [CreateProperty]
        public string Name
        {
            get
            {
                return Entity.Level.GetLevelName();
            }
            set
            {
                var name = (value ?? string.Empty).Trim();
                if (name.Length == 0 || name == Name)
                {
                    RefreshInspectorsOf(Entity);
                    return;
                }

                Edit(level => level.Level.static_world.level_name.SetMacRomanText(name));
                MapsLoading.Instance.UpdateOpenLevelDirectory();
            }
        }

        // Its walls and scenery collections (map.cpp: Environments). Choosing another offers to move the level's textures
        // (and control panels and media) to the new walls collection.
        [CreateProperty]
        public string Environment
        {
            get
            {
                return EnvironmentChoice(StaticWorld.environment_code);
            }
            set
            {
                if (TryFindChoice(ShortRange(0, map.NUMBER_OF_ENVIRONMENTS), EnvironmentChoice, value, out var code) && code != StaticWorld.environment_code)
                {
                    SetEnvironment(code);
                }
            }
        }

        [CreateProperty]
        public List<string> EnvironmentChoices
        {
            get
            {
                return ChoicesOf(ShortRange(0, map.NUMBER_OF_ENVIRONMENTS), EnvironmentChoice);
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

        // The landscape collection the song index picks (map.cpp: mark_map_collections). Choosing another offers to move
        // the level's landscape textures to it.
        [CreateProperty]
        public string Landscape
        {
            get
            {
                return LandscapeChoice(StaticWorld.song_index);
            }
            set
            {
                if (TryFindChoice(ShortRange(0, NumberOfLandscapes), LandscapeChoice, value, out var songIndex) && songIndex != StaticWorld.song_index)
                {
                    SetLandscape(songIndex);
                }
            }
        }

        // One past the landscapes is still shown (as its number), last
        [CreateProperty]
        public List<string> LandscapeChoices
        {
            get
            {
                var choices = ChoicesOf(ShortRange(0, NumberOfLandscapes), LandscapeChoice);
                if (!choices.Contains(Landscape))
                {
                    choices.Add(Landscape);
                }

                return choices;
            }
        }

        [CreateProperty]
        public bool SinglePlayer
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._single_player_entry_point);
            }
            set
            {
                SetEntryPointFlag(map._single_player_entry_point, value);
            }
        }

        [CreateProperty]
        public bool Cooperative
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._multiplayer_cooperative_entry_point);
            }
            set
            {
                SetEntryPointFlag(map._multiplayer_cooperative_entry_point, value);
            }
        }

        [CreateProperty]
        public bool Carnage
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._multiplayer_carnage_entry_point);
            }
            set
            {
                SetEntryPointFlag(map._multiplayer_carnage_entry_point, value);
            }
        }

        [CreateProperty]
        public bool KillTheOneWithTheBall
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._kill_the_man_with_the_ball_entry_point);
            }
            set
            {
                SetEntryPointFlag(map._kill_the_man_with_the_ball_entry_point, value);
            }
        }

        [CreateProperty]
        public bool MonarchOfTheHill
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._king_of_hill_entry_point);
            }
            set
            {
                SetEntryPointFlag(map._king_of_hill_entry_point, value);
            }
        }

        [CreateProperty]
        public bool Defense
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._defense_entry_point);
            }
            set
            {
                SetEntryPointFlag(map._defense_entry_point, value);
            }
        }

        [CreateProperty]
        public bool Rugby
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._rugby_entry_point);
            }
            set
            {
                SetEntryPointFlag(map._rugby_entry_point, value);
            }
        }

        [CreateProperty]
        public bool CaptureTheFlag
        {
            get
            {
                return csmacros.TEST_FLAG(EntryPointFlags, map._capture_the_flag_entry_point);
            }
            set
            {
                SetEntryPointFlag(map._capture_the_flag_entry_point, value);
            }
        }

        [CreateProperty]
        public bool Vacuum
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_vacuum);
            }
            set
            {
                SetEnvironmentFlag(map._environment_vacuum, value);
            }
        }

        [CreateProperty]
        public bool Magnetic
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_magnetic);
            }
            set
            {
                SetEnvironmentFlag(map._environment_magnetic, value);
            }
        }

        [CreateProperty]
        public bool Rebellion
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_rebellion);
            }
            set
            {
                SetEnvironmentFlag(map._environment_rebellion, value);
            }
        }

        [CreateProperty]
        public bool LowGravity
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_low_gravity);
            }
            set
            {
                SetEnvironmentFlag(map._environment_low_gravity, value);
            }
        }

        [CreateProperty]
        public bool Extermination
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_extermination);
            }
            set
            {
                SetMissionFlag(map._mission_extermination, value);
            }
        }

        [CreateProperty]
        public bool Exploration
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_exploration);
            }
            set
            {
                SetMissionFlag(map._mission_exploration, value);
            }
        }

        [CreateProperty]
        public bool Retrieval
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_retrieval);
            }
            set
            {
                SetMissionFlag(map._mission_retrieval, value);
            }
        }

        [CreateProperty]
        public bool Repair
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_repair);
            }
            set
            {
                SetMissionFlag(map._mission_repair, value);
            }
        }

        [CreateProperty]
        public bool Rescue
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_rescue);
            }
            set
            {
                SetMissionFlag(map._mission_rescue, value);
            }
        }

        [CreateProperty]
        public bool TerminalPause
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_terminals_stop_time);
            }
            set
            {
                SetEnvironmentFlag(map._environment_terminals_stop_time, value);
            }
        }

        [CreateProperty]
        public bool M1Rebellion
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_rebellion_m1);
            }
            set
            {
                SetEnvironmentFlag(map._environment_rebellion_m1, value);
            }
        }

        [CreateProperty]
        public bool M1Exploration
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_exploration_m1);
            }
            set
            {
                SetMissionFlag(map._mission_exploration_m1, value);
            }
        }

        [CreateProperty]
        public bool M1Repair
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_repair_m1);
            }
            set
            {
                SetMissionFlag(map._mission_repair_m1, value);
            }
        }

        [CreateProperty]
        public bool M1Rescue
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.mission_flags, map._mission_rescue_m1);
            }
            set
            {
                SetMissionFlag(map._mission_rescue_m1, value);
            }
        }

        [CreateProperty]
        public bool M1Glue
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_glue_m1);
            }
            set
            {
                SetEnvironmentFlag(map._environment_glue_m1, value);
            }
        }

        [CreateProperty]
        public bool M1Ouch
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_ouch_m1);
            }
            set
            {
                SetEnvironmentFlag(map._environment_ouch_m1, value);
            }
        }

        [CreateProperty]
        public bool M1HasMusic
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_song_index_m1);
            }
            set
            {
                SetEnvironmentFlag(map._environment_song_index_m1, value);
            }
        }

        [CreateProperty]
        public bool M1WeaponsStyle
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_m1_weapons);
            }
            set
            {
                SetEnvironmentFlag(map._environment_m1_weapons, value);
            }
        }

        [CreateProperty]
        public bool M1ActivationRange
        {
            get
            {
                return csmacros.TEST_FLAG(StaticWorld.environment_flags, map._environment_activation_ranges);
            }
            set
            {
                SetEnvironmentFlag(map._environment_activation_ranges, value);
            }
        }

        // Whether the level has physics saved in it (which Marathon Infinity and Aleph One use instead of the physics file)
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

        // The picture Aleph One shows as the level starts, from the map's resources
        private static short ChapterScreenId
        {
            get
            {
                return MapsFile != null && LevelIndex >= 0 ? MapsFile.ChapterScreenPictureId(LevelIndex) : (short) -1;
            }
        }

        // Turning it off leaves it out of the saved map, so it can be turned off only while the map has one (and the
        // resources can be changed)
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
                    return NoneText;
                }

                var depthOffset = id - MapsFile.ChapterScreenBase - LevelIndex;
                var depth = depthOffset >= 20000 ? 32 : depthOffset >= 10000 ? 16 : 8;

                return Strings.Get(Strings.Level, "Inspector.Level.ChapterScreen.Picture", id, depth);
            }
        }

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

        // The chunks ForgePlus keeps (and saves) as they were loaded, without reading them
        [CreateProperty]
        public string ShapePatchSize
        {
            get
            {
                return ByteCount(GetChunk(tags.SHAPE_PATCH_TAG));
            }
        }

        [CreateProperty]
        public string SoundPatchSize
        {
            get
            {
                return ByteCount(GetChunk(tags.SOUND_PATCH_TAG));
            }
        }

        [CreateProperty]
        public string MmlSize
        {
            get
            {
                return ByteCount(GetChunk(tags.MMLS_TAG));
            }
        }

        [CreateProperty]
        public string MmlText
        {
            get
            {
                return ChunkText(tags.MMLS_TAG);
            }
        }

        [CreateProperty]
        public string LuaSize
        {
            get
            {
                return ByteCount(GetChunk(tags.LUAS_TAG));
            }
        }

        [CreateProperty]
        public string LuaText
        {
            get
            {
                return ChunkText(tags.LUAS_TAG);
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            var nameField = Root.Find<TextField>(nameof(Name));
            nameField.isDelayed = true;
            nameField.maxLength = Entity.Level.static_world.level_name.MacRomanTextCapacity();

            var tabs = Root.Q<RadioButtonGroup>("tabs");
            tabs.SetValueWithoutNotify(selectedTab);
            tabs.RegisterValueChangedCallback(changeEvent => ShowTab(changeEvent.newValue));

            MakeTextBlocksSelectable();

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

        private static string EnvironmentChoice(short code)
        {
            var name = EnvironmentName(code);

            return name != null ? Strings.Get(Strings.Level, "Inspector.Level.Environment.Value", name, code) : code.ToString();
        }

        // "walls4 (20)"
        private static string CollectionName(int collection)
        {
            return Strings.Get(Strings.Level, "Inspector.Level.CollectionName", AlephOneNames.Collection(collection), collection);
        }

        private static string LandscapeChoice(short songIndex)
        {
            return songIndex >= 0 && songIndex < NumberOfLandscapes ?
                   Strings.Get(Strings.Level, "Inspector.Level.Landscape.Choice", AlephOneNames.Collection(CollectionChoices.LandscapeCollectionOf(songIndex)), songIndex) :
                   songIndex.ToString();
        }

        private void SetEntryPointFlag(int flag, bool isSet)
        {
            Edit(level => level.Level.static_world.entry_point_flags = WithFlag(level.Level.static_world.entry_point_flags, flag, isSet));
            MapsLoading.Instance.UpdateOpenLevelDirectory();
        }

        private void SetEnvironmentFlag(int flag, bool isSet)
        {
            Edit(level => level.Level.static_world.environment_flags = WithFlag(level.Level.static_world.environment_flags, flag, isSet));
            MapsLoading.Instance.UpdateOpenLevelDirectory();
        }

        private void SetMissionFlag(int flag, bool isSet)
        {
            Edit(level => level.Level.static_world.mission_flags = WithFlag(level.Level.static_world.mission_flags, flag, isSet));
            MapsLoading.Instance.UpdateOpenLevelDirectory();
        }

        // Asks whether to move what's drawn from the previous walls collection to the new one (unless nothing is), as only
        // the level's environment's walls are loaded by the original games
        private async void SetEnvironment(short code)
        {
            var previousCollection = CollectionChoices.WallCollectionOf(StaticWorld.environment_code);
            var newCollection = CollectionChoices.WallCollectionOf(code);

            var moveTextures = await AskToMoveTextures(
                Strings.Get(Strings.Level, "Inspector.Level.ChangeEnvironment.Title", EnvironmentName(code) ?? code.ToString()),
                Strings.Get(Strings.Level, "Inspector.Level.ChangeEnvironment.Message", CollectionName(newCollection), CollectionName(previousCollection)),
                previousCollection, newCollection);

            if (moveTextures.HasValue)
            {
                LevelCollectionEditing.SetEnvironment(code, moveTextures.Value);
            }
            else
            {
                RefreshInspectorsOf(Entity);
            }
        }

        private async void SetLandscape(short songIndex)
        {
            var previousCollection = CollectionChoices.LandscapeCollectionOf(StaticWorld.song_index);
            var newCollection = CollectionChoices.LandscapeCollectionOf(songIndex);

            var moveTextures = await AskToMoveTextures(
                Strings.Get(Strings.Level, "Inspector.Level.ChangeLandscape.Title", AlephOneNames.Collection(newCollection)),
                Strings.Get(Strings.Level, "Inspector.Level.ChangeLandscape.Message", CollectionName(newCollection), CollectionName(previousCollection)),
                previousCollection, newCollection);

            if (moveTextures.HasValue)
            {
                LevelCollectionEditing.SetLandscape(songIndex, moveTextures.Value);
            }
            else
            {
                RefreshInspectorsOf(Entity);
            }
        }

        // Whether to move the textures (null to cancel the change); with nothing drawn from the previous collection,
        // there's nothing to ask
        private async Task<bool?> AskToMoveTextures(string title, string message, int previousCollection, int newCollection)
        {
            if (!UsesCollection(Entity.Level, previousCollection))
            {
                return false;
            }

            var result = await DialogManager.Instance.DisplayQueuedDialog(
                title,
                message,
                new[] { "Move", "Keep" },
                new[] { Strings.Get(Strings.Level, "Inspector.Level.MoveTextures"), Strings.Get(Strings.Level, "Inspector.Level.KeepTextures") },
                checkboxLabel: null);

            switch (result.Option)
            {
                case "Move":
                    return true;
                case "Keep":
                    return false;
                default:
                    return null;
            }
        }

        private static bool UsesCollection(MapLevel level, int collection)
        {
            bool Uses(ushort shapeDescriptor)
            {
                return !shapeDescriptor.IsEmptyShapeDescriptor() && shapeDescriptor.GetCollection() == collection;
            }

            return level.PolygonList.Any(polygon => Uses(polygon.floor_texture) || Uses(polygon.ceiling_texture)) ||
                   level.SideList.Any(side => Uses(side.primary_texture.texture) || Uses(side.secondary_texture.texture) || Uses(side.transparent_texture.texture)) ||
                   level.SideList.Any(side => map.SIDE_IS_CONTROL_PANEL(side) && devices.get_control_panel_definition(side.control_panel_type)?.collection == collection) ||
                   level.MediaList.Any(mediaData => media.get_media_definition(mediaData.type)?.collection == collection);
        }

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
            var picture = ChapterScreenId >= 0 ? ScenarioPictures.Get((short) (MapsFile.ChapterScreenBase + LevelIndex)) : null;
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

        // MML and Lua are text (UTF-8, which is also plain ASCII), maybe with a terminating NUL
        private string ChunkText(uint tag)
        {
            var chunk = GetChunk(tag);
            if (chunk == null)
            {
                return string.Empty;
            }

            var text = Encoding.UTF8.GetString(chunk).TrimEnd('\0');

            return CutOff(text);
        }
    }
}
