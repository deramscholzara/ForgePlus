using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using ForgePlus.Palette;
using ForgePlus.UI;
using RuntimeCore.Common;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;
using static AlephOne.platforms;

namespace ForgePlus.Inspection
{
    // The static data the level saves for a platform (see PlatformEditing), which the platform is initialized from.
    // Settings that make others irrelevant (as Aleph One ignores them then) gray them out.
    public class Inspector_Platform : Inspector_Base<LevelEntity_Platform>, IDestructionPreparable
    {
        // Which way the platform moves (the index of the Direction choice)
        private const int FromFloor = 0;
        private const int FromCeiling = 1;
        private const int FromFloorAndCeiling = 2;

        // What the platform does to adjacent platforms when it activates or deactivates (the index of those choices)
        private const int AdjacentUnaffected = 0;
        private const int ActivatesAdjacent = 1;
        private const int DeactivatesAdjacent = 2;

        public Inspector_Platform(LevelEntity_Platform platform) : base(platform)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Platform";
            }
        }

        private MapLevel Level
        {
            get
            {
                return Entity.ParentLevel.Level;
            }
        }

        private static_platform_data StaticData
        {
            get
            {
                return PlatformEditing.GetStaticData(Level, Entity.NativeIndex);
            }
        }

        private uint StaticFlags
        {
            get
            {
                return StaticData.static_flags;
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
                return TypeChoice(StaticData.type);
            }
            set
            {
                if (TryFindChoice(ShortRange(0, NUMBER_OF_PLATFORM_TYPES), TypeChoice, value, out var type) && type != StaticData.type)
                {
                    EditPlatform(staticData => staticData.type = type);

                    // The platforms list shows types
                    PaletteManager.Instance.RefreshSwatches();
                }
            }
        }

        [CreateProperty]
        public List<string> TypeChoices
        {
            get
            {
                return ChoicesOf(ShortRange(0, NUMBER_OF_PLATFORM_TYPES), TypeChoice);
            }
        }

        // Tagged switches and lights change every platform with their tag (0 is untagged)
        [CreateProperty]
        public int Tag
        {
            get
            {
                return StaticData.tag;
            }
            set
            {
                EditPlatform(staticData => staticData.tag = ClampToShort(value));
            }
        }

        // World units (1024 to a meter) per tick (30 per second)
        [CreateProperty]
        public int Speed
        {
            get
            {
                return StaticData.speed;
            }
            set
            {
                EditPlatform(staticData => staticData.speed = ClampToNonNegativeShort(value));
            }
        }

        // Ticks (30 per second) to wait at each level
        [CreateProperty]
        public int Delay
        {
            get
            {
                return StaticData.delay;
            }
            set
            {
                EditPlatform(staticData => staticData.delay = ClampToNonNegativeShort(value));
            }
        }

        // A platform that moves neither way does nothing (Aleph One allows it), and shows no direction
        [CreateProperty]
        public int Direction
        {
            get
            {
                if (PLATFORM_GOES_BOTH_WAYS(StaticFlags))
                {
                    return FromFloorAndCeiling;
                }

                if (PLATFORM_COMES_FROM_FLOOR(StaticFlags))
                {
                    return FromFloor;
                }

                return PLATFORM_COMES_FROM_CEILING(StaticFlags) ? FromCeiling : -1;
            }
            set
            {
                EditPlatform(staticData =>
                {
                    SET_PLATFORM_COMES_FROM_FLOOR(staticData, value == FromFloor || value == FromFloorAndCeiling);
                    SET_PLATFORM_COMES_FROM_CEILING(staticData, value == FromCeiling || value == FromFloorAndCeiling);
                }, changesShape: true);
            }
        }

        [CreateProperty]
        public bool InitiallyActive
        {
            get
            {
                return PLATFORM_IS_INITIALLY_ACTIVE(StaticFlags);
            }
            set
            {
                EditFlag(_platform_is_initially_active, value);
            }
        }

        // High for floor platforms, low for ceiling platforms, closed for platforms that go both ways
        [CreateProperty]
        public bool InitiallyExtended
        {
            get
            {
                return PLATFORM_IS_INITIALLY_EXTENDED(StaticFlags);
            }
            set
            {
                EditFlag(_platform_is_initially_extended, value);
            }
        }

        [CreateProperty]
        public bool ExtendsFloorToCeiling
        {
            get
            {
                return PLATFORM_EXTENDS_FLOOR_TO_CEILING(StaticFlags);
            }
            set
            {
                EditFlag(_platform_extends_floor_to_ceiling, value, changesShape: true);
            }
        }

        // How low and high the moving surface goes (for a platform that goes both ways, the floor's lowest and the
        // ceiling's highest). Automatic heights are worked out from the adjacent polygons as the level loads.
        [CreateProperty]
        public bool LowestIsAutomatic
        {
            get
            {
                return StaticData.minimum_height == cstypes.NONE;
            }
            set
            {
                EditPlatform(staticData => staticData.minimum_height = value ? cstypes.NONE : CurrentLowest, changesShape: true);
            }
        }

        [CreateProperty]
        public int Lowest
        {
            get
            {
                return LowestIsAutomatic ? CurrentLowest : StaticData.minimum_height;
            }
            set
            {
                EditPlatform(staticData => staticData.minimum_height = ClampToShort(value), changesShape: true);
            }
        }

        [CreateProperty]
        public bool IsLowestEditable
        {
            get
            {
                return !LowestIsAutomatic;
            }
        }

        [CreateProperty]
        public bool HighestIsAutomatic
        {
            get
            {
                return StaticData.maximum_height == cstypes.NONE;
            }
            set
            {
                EditPlatform(staticData => staticData.maximum_height = value ? cstypes.NONE : CurrentHighest, changesShape: true);
            }
        }

        [CreateProperty]
        public int Highest
        {
            get
            {
                return HighestIsAutomatic ? CurrentHighest : StaticData.maximum_height;
            }
            set
            {
                EditPlatform(staticData => staticData.maximum_height = ClampToShort(value), changesShape: true);
            }
        }

        [CreateProperty]
        public bool IsHighestEditable
        {
            get
            {
                return !HighestIsAutomatic;
            }
        }

        // Only used when working out an automatic height for a platform that goes one way
        [CreateProperty]
        public bool UsesNativePolygonHeights
        {
            get
            {
                return PLATFORM_USES_NATIVE_POLYGON_HEIGHTS(StaticFlags);
            }
            set
            {
                EditFlag(_platform_uses_native_polygon_heights, value, changesShape: true);
            }
        }

        [CreateProperty]
        public bool IsUsesNativePolygonHeightsEditable
        {
            get
            {
                return (Direction == FromFloor || Direction == FromCeiling) && (LowestIsAutomatic || HighestIsAutomatic);
            }
        }

        [CreateProperty]
        public string FloorRange
        {
            get
            {
                return Strings.Get(Strings.Platforms, "Inspector.Platform.Range", Entity.NativeObject.minimum_floor_height, Entity.NativeObject.maximum_floor_height);
            }
        }

        [CreateProperty]
        public string CeilingRange
        {
            get
            {
                return Strings.Get(Strings.Platforms, "Inspector.Platform.Range", Entity.NativeObject.minimum_ceiling_height, Entity.NativeObject.maximum_ceiling_height);
            }
        }

        [CreateProperty]
        public bool DelaysBeforeActivation
        {
            get
            {
                return PLATFORM_DELAYS_BEFORE_ACTIVATION(StaticFlags);
            }
            set
            {
                EditFlag(_platform_delays_before_activation, value);
            }
        }

        // At a quarter of its speed
        [CreateProperty]
        public bool ContractsSlower
        {
            get
            {
                return PLATFORM_CONTRACTS_SLOWER(StaticFlags);
            }
            set
            {
                EditFlag(_platform_contracts_slower, value);
            }
        }

        [CreateProperty]
        public bool IsPlayerControllable
        {
            get
            {
                return PLATFORM_IS_PLAYER_CONTROLLABLE(StaticFlags);
            }
            set
            {
                EditFlag(_platform_is_player_controllable, value);
            }
        }

        [CreateProperty]
        public bool IsMonsterControllable
        {
            get
            {
                return PLATFORM_IS_MONSTER_CONTROLLABLE(StaticFlags);
            }
            set
            {
                EditFlag(_platform_is_monster_controllable, value);
            }
        }

        // Only switches, tags and other platforms deactivating it are stopped (it still deactivates itself)
        [CreateProperty]
        public bool CannotBeExternallyDeactivated
        {
            get
            {
                return PLATFORM_CANNOT_BE_EXTERNALLY_DEACTIVATED(StaticFlags);
            }
            set
            {
                EditFlag(_platform_cannot_be_externally_deactivated, value);
            }
        }

        [CreateProperty]
        public bool ActivatesOnlyOnce
        {
            get
            {
                return PLATFORM_ACTIVATES_ONLY_ONCE(StaticFlags);
            }
            set
            {
                EditFlag(_platform_activates_only_once, value);
            }
        }

        // The action key only works on doors; other platforms activate when they're walked onto
        [CreateProperty]
        public bool IsDoor
        {
            get
            {
                return PLATFORM_IS_DOOR(StaticFlags);
            }
            set
            {
                EditFlag(_platform_is_door, value);
            }
        }

        [CreateProperty]
        public bool IsLocked
        {
            get
            {
                return PLATFORM_IS_LOCKED(StaticFlags);
            }
            set
            {
                EditFlag(_platform_is_locked, value);
            }
        }

        [CreateProperty]
        public bool IsSecret
        {
            get
            {
                return PLATFORM_IS_SECRET(StaticFlags);
            }
            set
            {
                EditFlag(_platform_is_secret, value);
            }
        }

        [CreateProperty]
        public bool ReversesWhenObstructed
        {
            get
            {
                return PLATFORM_REVERSES_DIRECTION_WHEN_OBSTRUCTED(StaticFlags);
            }
            set
            {
                EditFlag(_platform_reverses_direction_when_obstructed, value);
            }
        }

        // Crushes monsters and players obstructing it
        [CreateProperty]
        public bool CausesDamage
        {
            get
            {
                return PLATFORM_CAUSES_DAMAGE(StaticFlags);
            }
            set
            {
                EditFlag(_platform_causes_damage, value);
            }
        }

        [CreateProperty]
        public bool DeactivatesAtEachLevel
        {
            get
            {
                return PLATFORM_DEACTIVATES_AT_EACH_LEVEL(StaticFlags);
            }
            set
            {
                EditFlag(_platform_deactivates_at_each_level, value);
            }
        }

        [CreateProperty]
        public bool DeactivatesAtInitialLevel
        {
            get
            {
                return PLATFORM_DEACTIVATES_AT_INITIAL_LEVEL(StaticFlags);
            }
            set
            {
                EditFlag(_platform_deactivates_at_initial_level, value);
            }
        }

        // Deactivating at each level includes the initial one
        [CreateProperty]
        public bool IsDeactivatesAtInitialLevelEditable
        {
            get
            {
                return !DeactivatesAtEachLevel;
            }
        }

        // Aleph One would activate adjacent platforms and then try to deactivate them (which does nothing in the same
        // tick), so a platform with both is shown as activating them, and choosing replaces both
        [CreateProperty]
        public int WhenActivating
        {
            get
            {
                return AdjacentChoice(PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_WHEN_ACTIVATING(StaticFlags),
                                      PLATFORM_DEACTIVATES_ADJACENT_PLATFORMS_WHEN_ACTIVATING(StaticFlags));
            }
            set
            {
                EditPlatform(staticData =>
                {
                    staticData.static_flags = csmacros.SET_FLAG32(staticData.static_flags, _platform_activates_adjacent_platforms_when_activating, value == ActivatesAdjacent);
                    staticData.static_flags = csmacros.SET_FLAG32(staticData.static_flags, _platform_deactivates_adjacent_platforms_when_activating, value == DeactivatesAdjacent);
                });
            }
        }

        [CreateProperty]
        public int WhenDeactivating
        {
            get
            {
                return AdjacentChoice(PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_WHEN_DEACTIVATING(StaticFlags),
                                      PLATFORM_DEACTIVATES_ADJACENT_PLATFORMS_WHEN_DEACTIVATING(StaticFlags));
            }
            set
            {
                EditPlatform(staticData =>
                {
                    staticData.static_flags = csmacros.SET_FLAG32(staticData.static_flags, _platform_activates_adjacent_platforms_when_deactivating, value == ActivatesAdjacent);
                    staticData.static_flags = csmacros.SET_FLAG32(staticData.static_flags, _platform_deactivates_adjacent_platforms_when_deactivating, value == DeactivatesAdjacent);
                });
            }
        }

        [CreateProperty]
        public bool ActivatesAdjacentAtEachLevel
        {
            get
            {
                return PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_AT_EACH_LEVEL(StaticFlags);
            }
            set
            {
                EditFlag(_platform_activates_adjacent_platforms_at_each_level, value);
            }
        }

        // Skips the platform that activated this one, when this one changes the adjacent platforms
        [CreateProperty]
        public bool DoesNotActivateParent
        {
            get
            {
                return PLATFORM_DOES_NOT_ACTIVATE_PARENT(StaticFlags);
            }
            set
            {
                EditFlag(_platform_does_not_activate_parent, value);
            }
        }

        [CreateProperty]
        public bool IsDoesNotActivateParentEditable
        {
            get
            {
                return WhenActivating != AdjacentUnaffected || WhenDeactivating != AdjacentUnaffected || ActivatesAdjacentAtEachLevel;
            }
        }

        // The polygon's floor and ceiling lights
        [CreateProperty]
        public bool ActivatesLight
        {
            get
            {
                return PLATFORM_ACTIVATES_LIGHT(StaticFlags);
            }
            set
            {
                EditFlag(_platform_activates_light, value);
            }
        }

        [CreateProperty]
        public bool DeactivatesLight
        {
            get
            {
                return PLATFORM_DEACTIVATES_LIGHT(StaticFlags);
            }
            set
            {
                EditFlag(_platform_deactivates_light, value);
            }
        }

        // Marathon 1's flooding platforms (Aleph One reads Marathon 1's locked platforms as these)
        [CreateProperty]
        public bool FloodsM1
        {
            get
            {
                return PLATFORM_FLOODS_M1(StaticFlags);
            }
            set
            {
                EditFlag(_platform_floods_m1, value);
            }
        }

        [CreateProperty]
        public bool IsFloodsM1Editable
        {
            get
            {
                return Level.loaded_wad.data_version == editor.MARATHON_ONE_DATA_VERSION;
            }
        }

        // The simulation's state, which isn't saved
        [CreateProperty]
        public bool SimulationActive
        {
            get
            {
                return Entity.IsRuntimeActive;
            }
            set
            {
                Entity.SetRuntimeActive(value);
            }
        }

        // The moving surface's current extremes, from the platform (initialized from the static data)
        private short CurrentLowest
        {
            get
            {
                return Direction == FromCeiling ? Entity.NativeObject.minimum_ceiling_height : Entity.NativeObject.minimum_floor_height;
            }
        }

        private short CurrentHighest
        {
            get
            {
                return Direction == FromFloor ? Entity.NativeObject.maximum_floor_height : Entity.NativeObject.maximum_ceiling_height;
            }
        }

        public void PrepareForDestruction()
        {
            foreach (var runtimePlatform in LevelEntity_Level.Instance.CeilingPlatforms.Values)
            {
                runtimePlatform.BeginRuntimeStyleBehavior();
            }

            foreach (var runtimePlatform in LevelEntity_Level.Instance.FloorPlatforms.Values)
            {
                runtimePlatform.BeginRuntimeStyleBehavior();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Root.Find<Button>("obstruct").clicked += Entity.ObstructRuntimeBehavior;

            Entity.OnInspectionStateChange += OnInspectionStateChange;
        }

        protected override void OnUnloading()
        {
            base.OnUnloading();

            Entity.OnInspectionStateChange -= OnInspectionStateChange;
        }

        private void OnInspectionStateChange(LevelEntity_Platform platform)
        {
            RefreshValuesInInspector();
        }

        private void EditFlag(int flag, bool value, bool changesShape = false)
        {
            EditPlatform(staticData => staticData.static_flags = csmacros.SET_FLAG32(staticData.static_flags, flag, value), changesShape);
        }

        private void EditPlatform(Action<static_platform_data> edit, bool changesShape = false)
        {
            PlatformEditing.Edit(Entity.NativeIndex, edit, changesShape);

            RefreshInspectorsOf(Entity);
        }

        private static int AdjacentChoice(bool activates, bool deactivates)
        {
            return activates ? ActivatesAdjacent : (deactivates ? DeactivatesAdjacent : AdjacentUnaffected);
        }

        private static string TypeChoice(short type)
        {
            return NameAndNumber(AlephOneNames.PlatformType(type), type);
        }
    }
}
