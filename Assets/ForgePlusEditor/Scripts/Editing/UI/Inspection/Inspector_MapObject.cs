using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.MapObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    public class Inspector_MapObject : Inspector_Base<LevelEntity_MapObject>
    {
        private const int ActivationBiasCount = 4;

        private Inspector_Placement placementInspector;

        public Inspector_MapObject(LevelEntity_MapObject mapObject) : base(mapObject)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - MapObject";
            }
        }

        private map_object NativeObject
        {
            get
            {
                return Entity.NativeObject;
            }
        }

        [CreateProperty]
        public string Id
        {
            get
            {
                return Entity.NativeIndex.ToString();
            }
        }

        // Choosing another type starts its subtype at the type's first, and its facing at full volume for a sound source
        // (its facing is its volume), or else at 0
        [CreateProperty]
        public string Type
        {
            get
            {
                return TypeChoice(NativeObject.type);
            }
            set
            {
                if (TryFindChoice(TypeIndexes(), TypeChoice, value, out var type) && type != NativeObject.type)
                {
                    SetType(type);
                }
            }
        }

        [CreateProperty]
        public List<string> TypeChoices
        {
            get
            {
                return ChoicesOf(TypeIndexes(), TypeChoice);
            }
        }

        // What kind of monster, item or scenery it is, or a player start's team (a goal's number and a sound source's
        // sound are their own rows)
        [CreateProperty]
        public string Subtype
        {
            get
            {
                return SubtypeChoice(NativeObject.type, NativeObject.index);
            }
            set
            {
                if (TryFindChoice(SubtypeIndexes(), subtype => SubtypeChoice(NativeObject.type, subtype), value, out var chosenSubtype) && chosenSubtype != NativeObject.index)
                {
                    Edit(mapObject => mapObject.NativeObject.index = chosenSubtype);
                    ApplyType();
                }
            }
        }

        [CreateProperty]
        public List<string> SubtypeChoices
        {
            get
            {
                return ChoicesOf(SubtypeIndexes(), subtype => SubtypeChoice(NativeObject.type, subtype));
            }
        }

        [CreateProperty]
        public bool IsSubtypeShown
        {
            get
            {
                return NativeObject.type == map._saved_monster ||
                       NativeObject.type == map._saved_item ||
                       NativeObject.type == map._saved_object ||
                       NativeObject.type == map._saved_player;
            }
        }

        // What terminals' checkpoints find it by (computer_interface.cpp: find_checkpoint_location)
        [CreateProperty]
        public int GoalNumber
        {
            get
            {
                return NativeObject.index;
            }
            set
            {
                Edit(mapObject => mapObject.NativeObject.index = ClampToShort(value));
            }
        }

        [CreateProperty]
        public bool IsGoal
        {
            get
            {
                return NativeObject.type == map._saved_goal;
            }
        }

        [CreateProperty]
        public bool IsSoundSource
        {
            get
            {
                return NativeObject.type == map._saved_sound_source;
            }
        }

        [CreateProperty]
        public bool IsNotSoundSource
        {
            get
            {
                return !IsSoundSource;
            }
        }

        // A sound source's subtype is an ambient sound code (Aleph One's table of them names a sound in the sounds file)
        [CreateProperty]
        public string Sound
        {
            get
            {
                return SoundImageDescriptions.AmbientSoundName(NativeObject.index);
            }
            set
            {
                if (TryFindChoice(ShortRange(0, SoundManagerEnums.NUMBER_OF_AMBIENT_SOUND_DEFINITIONS), SoundImageDescriptions.AmbientSoundName, value, out var sound))
                {
                    Edit(mapObject => mapObject.NativeObject.index = sound);
                }
            }
        }

        [CreateProperty]
        public List<string> SoundChoices
        {
            get
            {
                return SoundCodeChoices(NativeObject.index, SoundManagerEnums.NUMBER_OF_AMBIENT_SOUND_DEFINITIONS, SoundsLoading.Instance.HasAmbientSound, SoundImageDescriptions.AmbientSoundName);
            }
        }

        // Only the sounds the loaded sounds file has can be chosen, and a platform sound source doesn't play its own
        [CreateProperty]
        public bool IsSoundEditable
        {
            get
            {
                return SoundsLoading.Instance.IsLoaded && !IsPlatformSoundSource;
            }
        }

        [CreateProperty]
        public bool IsSoundPlayable
        {
            get
            {
                return SoundPreviews.CanPlayAmbientSound(NativeObject.index);
            }
        }

        // At its volume
        public void PlaySound()
        {
            SoundPreviews.PlayAmbientSound(NativeObject.index, SoundPreviews.SoundSourceVolume(NativeObject));
        }

        // A sound source with the platform sound flag plays its platform's moving sound instead of its own, while the
        // platform moves (map.cpp: _sound_add_ambient_sources_proc)
        [CreateProperty]
        public bool IsPlatformSoundSource
        {
            get
            {
                return IsSoundSource && Invisible;
            }
        }

        [CreateProperty]
        public string PlatformMovingSound
        {
            get
            {
                var polygon = map.get_polygon_data(LevelEntity_Level.Instance.Level, NativeObject.polygon_index);
                if (polygon == null || polygon.type != map._polygon_is_platform)
                {
                    return Strings.Get(Strings.Common, "Inspector.MapObject.PlatformMovingSound.NotPlatform");
                }

                var ambientSound = SoundPreviews.PlatformMovingSound(LevelEntity_Level.Instance.Level, NativeObject);

                return ambientSound == cstypes.NONE ? Strings.Get(Strings.Common, "Inspector.MapObject.PlatformMovingSound.None") : SoundImageDescriptions.AmbientSoundName(ambientSound);
            }
        }

        [CreateProperty]
        public bool IsPlatformMovingSoundPlayable
        {
            get
            {
                return SoundPreviews.CanPlayAmbientSound(SoundPreviews.PlatformMovingSound(LevelEntity_Level.Instance.Level, NativeObject));
            }
        }

        // At its volume
        public void PlayPlatformMovingSound()
        {
            SoundPreviews.PlayAmbientSound(SoundPreviews.PlatformMovingSound(LevelEntity_Level.Instance.Level, NativeObject), SoundPreviews.SoundSourceVolume(NativeObject));
        }

        // In degrees
        [CreateProperty]
        public float Angle
        {
            get
            {
                return (float) Math.Round(AlephOneExtensions.AngleToDegrees(NativeObject.facing), 2);
            }
            set
            {
                var angle = AlephOneExtensions.DegreesToAngle(value);

                Edit(mapObject => mapObject.NativeObject.facing = angle);
                Entity.ApplyPlacement();
            }
        }

        // A sound source's facing is its volume (map.cpp: _sound_add_ambient_sources_proc), or, when it's negative, the
        // intensity of the light whose index it is (negated) while the game runs. Light 0 can't be one (as -0 is 0).
        [CreateProperty]
        public bool VolumeFromLight
        {
            get
            {
                return NativeObject.facing < 0;
            }
            set
            {
                if (value == VolumeFromLight)
                {
                    return;
                }

                if (value)
                {
                    // The first light it can follow
                    var level = LevelEntity_Level.Instance;
                    var light = level ? level.Lights.Keys.Where(index => index > 0).DefaultIfEmpty((short) 0).Min() : (short) 0;

                    if (light > 0)
                    {
                        Edit(mapObject => mapObject.NativeObject.facing = (short) -light);
                    }
                    else
                    {
                        RefreshInspectorsOf(Entity);
                    }
                }
                else
                {
                    Edit(mapObject => mapObject.NativeObject.facing = SoundManagerEnums.MAXIMUM_SOUND_VOLUME);
                }
            }
        }

        [CreateProperty]
        public bool IsVolumeFromLightEditable
        {
            get
            {
                return IsSoundSource;
            }
        }

        [CreateProperty]
        public bool IsVolumeFixed
        {
            get
            {
                return IsSoundSource && !VolumeFromLight;
            }
        }

        [CreateProperty]
        public bool IsVolumeFromLightShown
        {
            get
            {
                return IsSoundSource && VolumeFromLight;
            }
        }

        // From silent (0) to full (MAXIMUM_SOUND_VOLUME)
        [CreateProperty]
        public int Volume
        {
            get
            {
                return Math.Max(0, (int) NativeObject.facing);
            }
            set
            {
                var volume = (short) Math.Clamp(value, 0, SoundManagerEnums.MAXIMUM_SOUND_VOLUME);

                if (volume != NativeObject.facing)
                {
                    Edit(mapObject => mapObject.NativeObject.facing = volume);
                }
                else
                {
                    RefreshInspectorsOf(Entity);
                }
            }
        }

        [CreateProperty]
        public int VolumeSlider
        {
            get
            {
                return Volume;
            }
            set
            {
                if (value != VolumeSlider)
                {
                    Volume = value;
                }
            }
        }

        [CreateProperty]
        public int VolumeLight
        {
            get
            {
                return NativeObject.facing < 0 ? -NativeObject.facing : LightIndexField.NoLight;
            }
            set
            {
                var level = LevelEntity_Level.Instance;
                if (value > 0 && value <= short.MaxValue && level && level.Lights.ContainsKey((short) value))
                {
                    Edit(mapObject => mapObject.NativeObject.facing = (short) -value);
                }
                else
                {
                    RefreshInspectorsOf(Entity);
                }
            }
        }

        // As it's saved (the light's index, negated)
        [CreateProperty]
        public string SavedVolume
        {
            get
            {
                return NativeObject.facing.ToString();
            }
        }

        // What an activated monster goes for (monsters.cpp: activation_bias, from the flags' top 4 bits)
        [CreateProperty]
        public string ActivationBias
        {
            get
            {
                return ActivationBiasChoice(map.DECODE_ACTIVATION_BIAS(NativeObject.flags));
            }
            set
            {
                if (TryFindChoice(Enumerable.Range(0, ActivationBiasCount), ActivationBiasChoice, value, out var bias))
                {
                    Edit(mapObject => mapObject.NativeObject.flags = (ushort) ((mapObject.NativeObject.flags & ~map.ENCODE_ACTIVATION_BIAS(0xF)) | map.ENCODE_ACTIVATION_BIAS(bias)));
                }
            }
        }

        [CreateProperty]
        public List<string> ActivationBiasChoices
        {
            get
            {
                return ChoicesOf(Enumerable.Range(0, ActivationBiasCount), ActivationBiasChoice, map.DECODE_ACTIVATION_BIAS(NativeObject.flags));
            }
        }

        [CreateProperty]
        public bool IsMonster
        {
            get
            {
                return NativeObject.type == map._saved_monster;
            }
        }

        // X and Y in the level (in a polygon, which it's moved to), and Z above its polygon's floor (or below its ceiling,
        // for one hanging from it), in world units (1024 to a meter)
        [CreateProperty]
        public int PositionX
        {
            get
            {
                return NativeObject.location.x;
            }
            set
            {
                Move(ClampToShort(value), NativeObject.location.y);
            }
        }

        [CreateProperty]
        public int PositionY
        {
            get
            {
                return NativeObject.location.y;
            }
            set
            {
                Move(NativeObject.location.x, ClampToShort(value));
            }
        }

        [CreateProperty]
        public int PositionZ
        {
            get
            {
                return NativeObject.location.z;
            }
            set
            {
                Edit(mapObject => mapObject.NativeObject.location.z = ClampToShort(value));
                Entity.ApplyPlacement();
                UpdateSoundSources();
            }
        }

        [CreateProperty]
        public string PolygonIndex
        {
            get
            {
                return NativeObject.polygon_index.ToString();
            }
        }

        // The same flag is a sound source's "platform sound" and anything else's "invisible" (a monster teleports in when
        // it's activated), so it's labelled for the object it's on
        [CreateProperty]
        public bool Invisible
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_is_invisible);
            }
            set
            {
                SetFlag(map._map_object_is_invisible, value);
            }
        }

        [CreateProperty]
        public bool FromCeiling
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_hanging_from_ceiling);
            }
            set
            {
                SetFlag(map._map_object_hanging_from_ceiling, value);
                Entity.ApplyPlacement();
            }
        }

        // A blind or deaf monster can't see or hear what would activate it
        [CreateProperty]
        public bool Blind
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_is_blind);
            }
            set
            {
                SetFlag(map._map_object_is_blind, value);
            }
        }

        [CreateProperty]
        public bool Deaf
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_is_deaf);
            }
            set
            {
                SetFlag(map._map_object_is_deaf, value);
            }
        }

        // Only placed in multiplayer games
        [CreateProperty]
        public bool NetworkOnly
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_is_network_only);
            }
            set
            {
                SetFlag(map._map_object_is_network_only, value);
            }
        }

        [CreateProperty]
        public bool Floats
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_floats);
            }
            set
            {
                SetFlag(map._map_object_floats, value);
            }
        }

        [CreateProperty]
        public bool HasPlacement
        {
            get
            {
                return Entity.Placement != null;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Root.Q(nameof(Subtype)).BindShown(this, nameof(IsSubtypeShown));
            Root.Q(nameof(GoalNumber)).BindShown(this, nameof(IsGoal));
            Root.Q(nameof(Sound)).BindShown(this, nameof(IsSoundSource));
            Root.Q(nameof(Angle)).BindShown(this, nameof(IsNotSoundSource));
            Root.Q(nameof(VolumeFromLight)).BindShown(this, nameof(IsSoundSource));
            Root.Q(nameof(Volume)).BindShown(this, nameof(IsVolumeFixed));
            Root.Q(nameof(VolumeLight)).BindShown(this, nameof(IsVolumeFromLightShown));
            Root.Q(nameof(SavedVolume)).BindShown(this, nameof(IsVolumeFromLightShown));
            Root.Q(nameof(ActivationBias)).BindShown(this, nameof(IsMonster));
            Root.Q("monster-flags").BindEnabled(this, nameof(IsMonster));

            Root.Q(nameof(Volume)).Q<SliderInt>().highValue = SoundManagerEnums.MAXIMUM_SOUND_VOLUME;

            Root.Q("volume-light-note").BindShown(this, nameof(IsVolumeFromLightShown));

            Root.Q(nameof(PlatformMovingSound)).BindShown(this, nameof(IsPlatformSoundSource));
            Root.Q("platform-sound-note").BindShown(this, nameof(IsPlatformSoundSource));

            // Light 0 can't be one a volume follows
            Root.Find<LightIndexField>(nameof(VolumeLight)).MinimumLight = 1;

            Strings.SetText(Root.Find<Toggle>(nameof(Invisible)), IsSoundSource ? Strings.Get(Strings.Common, "Inspector.MapObject.Flag.PlatformSound") : Strings.Get(Strings.Common, "Inspector.MapObject.Flag.Invisible"));

            // Items and monsters are placed by type
            Root.Q("Placement").style.display = HasPlacement ? DisplayStyle.Flex : DisplayStyle.None;
            if (HasPlacement)
            {
                var typeName = NativeObject.type == map._saved_monster ? AlephOneNames.MonsterType(NativeObject.index) : AlephOneNames.ItemType(NativeObject.index);
                Strings.SetText(Root.Find<Label>("PlacementHeading"), Strings.Get(Strings.Common, "Inspector.MapObject.PlacementHeading", typeName));

                placementInspector = new Inspector_Placement(Entity.Placement);
                placementInspector.Load(Root.Q("placement-entry"));
            }

            // Which sounds can be chosen depends on the sounds file
            SoundsLoading.Instance.OnDataLoadCompleted += OnSoundsLoadCompleted;
        }

        protected override void OnUnloading()
        {
            base.OnUnloading();

            placementInspector?.Unload();
            placementInspector = null;

            SoundsLoading.Instance.OnDataLoadCompleted -= OnSoundsLoadCompleted;
        }

        private void OnSoundsLoadCompleted(bool isLoaded)
        {
            RefreshValuesInInspector();
        }

        private static string ActivationBiasName(int bias)
        {
            switch (bias)
            {
                case 0:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.ActivationBias.Player");
                case 1:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.ActivationBias.NearestHostile");
                case 2:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.ActivationBias.Goal");
                default:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.ActivationBias.Random");
            }
        }

        private static IEnumerable<short> TypeIndexes()
        {
            return ShortRange(map._saved_monster, map._saved_sound_source - map._saved_monster + 1);
        }

        private static string TypeChoice(short type)
        {
            switch (type)
            {
                case map._saved_monster:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.Type.Monster");
                case map._saved_object:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.Type.Scenery");
                case map._saved_item:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.Type.Item");
                case map._saved_player:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.Type.Player");
                case map._saved_goal:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.Type.Goal");
                case map._saved_sound_source:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.Type.Sound");
                default:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.Type.Unknown", type);
            }
        }

        // Its type's subtypes (and its own first, if it's none of them)
        private IEnumerable<short> SubtypeIndexes()
        {
            short count;
            switch (NativeObject.type)
            {
                case map._saved_monster:
                    count = monsters.NUMBER_OF_MONSTER_TYPES;
                    break;
                case map._saved_item:
                    count = items.NUMBER_OF_DEFINED_ITEMS;
                    break;
                case map._saved_object:
                    count = scenery.NUMBER_OF_SCENERY_DEFINITIONS;
                    break;
                case map._saved_player:
                    count = player.NUMBER_OF_TEAM_COLORS;
                    break;
                default:
                    count = 0;
                    break;
            }

            if (NativeObject.index < 0 || NativeObject.index >= count)
            {
                yield return NativeObject.index;
            }

            for (short subtype = 0; subtype < count; subtype++)
            {
                yield return subtype;
            }
        }

        // "fighter minor (8)", or a scenery's or team's number
        private static string SubtypeChoice(short type, short subtype)
        {
            switch (type)
            {
                case map._saved_monster:
                    return NameAndNumber(AlephOneNames.MonsterType(subtype), subtype);
                case map._saved_item:
                    return NameAndNumber(AlephOneNames.ItemType(subtype), subtype);
                case map._saved_player:
                    return Strings.Get(Strings.Common, "Inspector.MapObject.Team", subtype);
                default:
                    return $"({subtype})";
            }
        }

        private static string ActivationBiasChoice(int bias)
        {
            return bias >= 0 && bias < ActivationBiasCount ? NameAndNumber(ActivationBiasName(bias), bias) : bias.ToString();
        }

        private void SetType(short type)
        {
            var wasSoundSource = IsSoundSource;

            Edit(mapObject =>
            {
                var nativeObject = mapObject.NativeObject;
                nativeObject.type = type;
                nativeObject.index = 0;

                if (type == map._saved_sound_source)
                {
                    nativeObject.facing = SoundManagerEnums.MAXIMUM_SOUND_VOLUME;
                }
                else if (wasSoundSource)
                {
                    nativeObject.facing = 0;
                }
            });

            ApplyType();

            if (wasSoundSource || IsSoundSource)
            {
                UpdateSoundSources(force: true);
            }
        }

        // Its icon and sprite follow its type, and so do its inspector's rows (some of which are only made for some types)
        private void ApplyType()
        {
            Entity.ApplyType();

            var entity = Entity;
            Root.schedule.Execute(() =>
            {
                if (SelectionManager.Instance.GetIsSelected(entity))
                {
                    InspectorPanel.Instance.ClearAllInspectors();
                    entity.Inspect();
                }
            });
        }

        // Moving it in the level moves it to the polygon it's then in (it can't be moved out of the level's polygons)
        private void Move(short x, short y)
        {
            var level = LevelEntity_Level.Instance.Level;
            var polygonIndex = map.world_point_to_polygon_index(level, new world_point2d { x = x, y = y });

            if (polygonIndex == cstypes.NONE)
            {
                RefreshInspectorsOf(Entity);
                return;
            }

            Edit(mapObject =>
            {
                mapObject.NativeObject.location.x = x;
                mapObject.NativeObject.location.y = y;
                mapObject.NativeObject.polygon_index = polygonIndex;
            });

            Entity.ApplyPlacement();
            UpdateSoundSources();
        }

        // A sound source is heard from the polygons near it, which each polygon lists with the level's other map indexes
        // (map_constructors.cpp: precalculate_polygon_sound_sources)
        private void UpdateSoundSources(bool force = false)
        {
            if (!force && !IsSoundSource)
            {
                return;
            }

            var level = LevelEntity_Level.Instance.Level;
            level.MapIndexList.Clear();
            map_constructors.precalculate_map_indexes(level);
        }

        private void SetFlag(ushort flag, bool isSet)
        {
            Edit(mapObject => mapObject.NativeObject.flags = WithFlag(mapObject.NativeObject.flags, flag, isSet));
        }
    }
}
