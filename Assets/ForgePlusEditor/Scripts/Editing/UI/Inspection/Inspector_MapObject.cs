using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.UI;
using RuntimeCore.Entities.MapObjects;
using System;
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
        }
    }
}
