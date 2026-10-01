using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
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

        [CreateProperty]
        public string Type
        {
            get
            {
                return NativeObject.GetTypeName();
            }
        }

        [CreateProperty]
        public string Subtype
        {
            get
            {
                switch (NativeObject.type)
                {
                    case map._saved_monster:
                        return $"{AlephOneNames.MonsterType(NativeObject.index)} ({NativeObject.index})";
                    case map._saved_item:
                        return $"{AlephOneNames.ItemType(NativeObject.index)} ({NativeObject.index})";
                    case map._saved_player:
                    case map._saved_object:
                    case map._saved_sound_source:
                    case map._saved_goal:
                        return $"({NativeObject.index})";
                    default:
                        return "Invalid";
                }
            }
        }

        // A sound object's subtype is its sound, shown as a choice instead
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

        // A sound object's index is an ambient sound code (Aleph One's table of them names a sound in the sounds file).
        // Only the sounds the loaded sounds file has can be chosen, so without one it can't be edited.
        [CreateProperty]
        public string Sound
        {
            get
            {
                return AmbientSoundChoice(NativeObject.index);
            }
            set
            {
                for (short ambientSound = 0; ambientSound < SoundManagerEnums.NUMBER_OF_AMBIENT_SOUND_DEFINITIONS; ambientSound++)
                {
                    if (AmbientSoundChoice(ambientSound) == value)
                    {
                        Edit(mapObject => mapObject.NativeObject.index = ambientSound);
                        return;
                    }
                }
            }
        }

        [CreateProperty]
        public List<string> SoundChoices
        {
            get
            {
                var choices = new List<string>();

                for (short ambientSound = 0; ambientSound < SoundManagerEnums.NUMBER_OF_AMBIENT_SOUND_DEFINITIONS; ambientSound++)
                {
                    if (ambientSound == NativeObject.index || SoundsLoading.Instance.HasAmbientSound(ambientSound))
                    {
                        choices.Add(AmbientSoundChoice(ambientSound));
                    }
                }

                // One the engine has no sound for is still shown (as its number)
                if (!choices.Contains(Sound))
                {
                    choices.Insert(0, Sound);
                }

                return choices;
            }
        }

        [CreateProperty]
        public bool IsSoundEditable
        {
            get
            {
                return SoundsLoading.Instance.IsLoaded;
            }
        }

        // Which way it faces (a sound source's facing is its volume instead)
        [CreateProperty]
        public string Angle
        {
            get
            {
                return AlephOneExtensions.AngleToDegrees(NativeObject.facing).ToString();
            }
        }

        // A sound source's facing is its volume (map.cpp, _sound_add_ambient_sources_proc): from silent (0) to full
        // (MAXIMUM_SOUND_VOLUME), or, when it's negative, the intensity of the light whose index it is (negated) while the
        // game runs. Light 0 can't be one (as -0 is 0).
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

        // The light whose intensity is its volume
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
                var bias = map.DECODE_ACTIVATION_BIAS(NativeObject.flags);

                return bias >= 0 && bias < ActivationBiasNames.Length ? $"{ActivationBiasNames[bias]} ({bias})" : bias.ToString();
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

        [CreateProperty]
        public string Position
        {
            get
            {
                return $"X: {NativeObject.location.x}\n" +
                       $"Y: {NativeObject.location.y}\n" +
                       $"Z: {NativeObject.location.z}";
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

        // The same flag is a sound source's "platform sound" (it plays the moving sound of the platform it's on, while
        // the platform moves) and anything else's "invisible" (a monster teleports in when it's activated), so it's
        // labelled for the object it's on
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

        // A sound source's flags can be edited (the others' not yet)
        [CreateProperty]
        public bool IsInvisibleEditable
        {
            get
            {
                return IsSoundSource;
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

                // It's placed from the ceiling instead of the floor (or the other way)
                Entity.ApplyPlacement();
            }
        }

        [CreateProperty]
        public bool IsFromCeilingEditable
        {
            get
            {
                return IsSoundSource;
            }
        }

        [CreateProperty]
        public bool Blind
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_is_blind);
            }
        }

        [CreateProperty]
        public bool Deaf
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_is_deaf);
            }
        }

        [CreateProperty]
        public bool NetworkOnly
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_is_network_only);
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
        public bool IsFloatsEditable
        {
            get
            {
                return IsSoundSource;
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

            Root.Q(nameof(Subtype)).BindShown(this, nameof(IsNotSoundSource));
            Root.Q(nameof(Sound)).BindShown(this, nameof(IsSoundSource));
            Root.Q(nameof(Angle)).BindShown(this, nameof(IsNotSoundSource));
            Root.Q(nameof(VolumeFromLight)).BindShown(this, nameof(IsSoundSource));
            Root.Q(nameof(Volume)).BindShown(this, nameof(IsVolumeFixed));
            Root.Q(nameof(VolumeLight)).BindShown(this, nameof(IsVolumeFromLightShown));
            Root.Q(nameof(SavedVolume)).BindShown(this, nameof(IsVolumeFromLightShown));
            Root.Q(nameof(ActivationBias)).BindShown(this, nameof(IsMonster));

            Root.Q(nameof(Volume)).Q<SliderInt>().highValue = SoundManagerEnums.MAXIMUM_SOUND_VOLUME;

            Root.Q("volume-light-note").BindShown(this, nameof(IsVolumeFromLightShown));

            // Light 0 can't be one a volume follows
            Root.Find<LightIndexField>(nameof(VolumeLight)).MinimumLight = 1;

            Root.Find<Toggle>(nameof(Invisible)).text = IsSoundSource ? "Platform Sound" : "Invisible";

            // Items and monsters are placed by type (which is also edited with nothing selected, for every type)
            Root.Q("Placement").style.display = HasPlacement ? DisplayStyle.Flex : DisplayStyle.None;
            if (HasPlacement)
            {
                var typeName = NativeObject.type == map._saved_monster ? AlephOneNames.MonsterType(NativeObject.index) : AlephOneNames.ItemType(NativeObject.index);
                Root.Find<Label>("PlacementHeading").text = $"Placement (every {typeName})";

                placementInspector = new Inspector_Placement(Entity.Placement);
                placementInspector.Load(Root.Q("placement-entry"));
            }

            // Which sounds can be chosen, and whether they can be, depends on the sounds file
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

        private static readonly string[] ActivationBiasNames = { "Player", "Nearest Hostile", "Goal", "Random" };

        // "Waterfall (6)", or just the number for a code the engine has no sound for
        private void SetFlag(ushort flag, bool isSet)
        {
            Edit(mapObject => mapObject.NativeObject.flags = isSet ? (ushort) (mapObject.NativeObject.flags | flag) : (ushort) (mapObject.NativeObject.flags & ~flag));
        }

        private static string AmbientSoundChoice(short ambientSound)
        {
            var name = AlephOneNames.AmbientSound(ambientSound);

            return name == ambientSound.ToString() ? $"({ambientSound})" : $"{name} ({ambientSound})";
        }
    }
}
