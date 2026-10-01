using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.MapObjects;
using System;
using System.Collections.Generic;
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

        // A sound source's volume, which is its facing (map.cpp, cause_ambient_sound_source_update): from silent (0) to
        // full (MAXIMUM_SOUND_VOLUME), or, when it's negative, the volume is the intensity of the light whose index it
        // is (negated) while the game runs. A value that's neither (such as a light the level hasn't) is reverted.
        [CreateProperty]
        public int Volume
        {
            get
            {
                return NativeObject.facing;
            }
            set
            {
                var level = LevelEntity_Level.Instance;
                var isVolume = value >= 0 && value <= SoundManagerEnums.MAXIMUM_SOUND_VOLUME;
                var isLight = value < 0 && value >= short.MinValue + 1 && level && level.Lights.ContainsKey((short) -value);

                if (isVolume || isLight)
                {
                    Edit(mapObject => mapObject.NativeObject.facing = (short) value);
                }
                else
                {
                    RefreshInspectorsOf(Entity);
                }
            }
        }

        // The slider only covers volumes (a light's index is typed in the field), and is at silent while it's a light's.
        // It doesn't set a light's volume to silent just by showing it there.
        [CreateProperty]
        public int VolumeSlider
        {
            get
            {
                return Math.Max(0, (int) NativeObject.facing);
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
        public bool IsVolumeFromLight
        {
            get
            {
                return IsSoundSource && NativeObject.facing < 0;
            }
        }

        [CreateProperty]
        public string VolumeNote
        {
            get
            {
                var lightIndex = -NativeObject.facing;

                return NativeObject.facing < 0 ?
                       $"While the game runs, the sound's volume follows light {lightIndex}'s intensity: silent while the light is off, and full volume while it's fully on. (Light 0 can't be used this way.)" :
                       string.Empty;
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
        }

        [CreateProperty]
        public bool FromCeiling
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_hanging_from_ceiling);
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
            Root.Q(nameof(Volume)).BindShown(this, nameof(IsSoundSource));
            Root.Q(nameof(ActivationBias)).BindShown(this, nameof(IsMonster));

            Root.Q(nameof(Volume)).Q<SliderInt>().highValue = SoundManagerEnums.MAXIMUM_SOUND_VOLUME;

            var volumeNote = Root.Q<Label>("volume-note");
            volumeNote.Bind("text", this, nameof(VolumeNote));
            volumeNote.BindShown(this, nameof(IsVolumeFromLight));

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
        private static string AmbientSoundChoice(short ambientSound)
        {
            var name = AlephOneNames.AmbientSound(ambientSound);

            return name == ambientSound.ToString() ? $"({ambientSound})" : $"{name} ({ambientSound})";
        }
    }
}
