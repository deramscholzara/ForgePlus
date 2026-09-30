using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.UI;
using RuntimeCore.Entities.MapObjects;
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    public class Inspector_MapObject : Inspector_Base<LevelEntity_MapObject>
    {
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

        [CreateProperty]
        public string Angle
        {
            get
            {
                return AlephOneExtensions.AngleToDegrees(NativeObject.facing).ToString();
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
        public bool OnPlatform
        {
            get
            {
                return csmacros.TEST_FLAG(NativeObject.flags, map._map_object_is_platform_sound);
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

        [CreateProperty]
        public string PlacementInitialCount
        {
            get
            {
                return HasPlacement ? Entity.Placement.initial_count.ToString() : "-";
            }
        }

        [CreateProperty]
        public string PlacementMinimumCount
        {
            get
            {
                return HasPlacement ? Entity.Placement.minimum_count.ToString() : "-";
            }
        }

        [CreateProperty]
        public string PlacementMaximumCount
        {
            get
            {
                return HasPlacement ? Entity.Placement.maximum_count.ToString() : "-";
            }
        }

        [CreateProperty]
        public string PlacementRandomCount
        {
            get
            {
                return HasPlacement ? Entity.Placement.random_count.ToString() : "-";
            }
        }

        // random_chance is in (0, 65535]
        [CreateProperty]
        public string PlacementRandomChance
        {
            get
            {
                return HasPlacement ? $"{Math.Round(Entity.Placement.random_chance * 100.0 / ushort.MaxValue)} %" : "-";
            }
        }

        [CreateProperty]
        public string PlacementRandomLocation
        {
            get
            {
                return HasPlacement ? csmacros.TEST_FLAG(Entity.Placement.flags, map._reappears_in_random_location).ToString() : "-";
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Root.Q("Placement").BindEnabled(this, nameof(HasPlacement));
            Root.Q(nameof(Subtype)).BindShown(this, nameof(IsNotSoundSource));
            Root.Q(nameof(Sound)).BindShown(this, nameof(IsSoundSource));

            // Which sounds can be chosen, and whether they can be, depends on the sounds file
            SoundsLoading.Instance.OnDataLoadCompleted += OnSoundsLoadCompleted;
        }

        protected override void OnUnloading()
        {
            base.OnUnloading();

            SoundsLoading.Instance.OnDataLoadCompleted -= OnSoundsLoadCompleted;
        }

        private void OnSoundsLoadCompleted(bool isLoaded)
        {
            RefreshValuesInInspector();
        }

        // "Waterfall (6)", or just the number for a code the engine has no sound for
        private static string AmbientSoundChoice(short ambientSound)
        {
            var name = AlephOneNames.AmbientSound(ambientSound);

            return name == ambientSound.ToString() ? $"({ambientSound})" : $"{name} ({ambientSound})";
        }
    }
}
