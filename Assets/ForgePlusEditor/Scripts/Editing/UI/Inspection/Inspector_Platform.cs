using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.UI;
using RuntimeCore.Common;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    public class Inspector_Platform : Inspector_Base<LevelEntity_Platform>, IDestructionPreparable
    {
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

        private uint StaticFlags
        {
            get
            {
                return Entity.NativeObject.static_flags;
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
        public string Tag
        {
            get
            {
                return Entity.NativeObject.tag.ToString();
            }
        }

        [CreateProperty]
        public string Type
        {
            get
            {
                return AlephOneNames.PlatformType(Entity.NativeObject.type);
            }
        }

        [CreateProperty]
        public string Speed
        {
            get
            {
                return Entity.NativeObject.speed.ToString();
            }
        }

        [CreateProperty]
        public string Delay
        {
            get
            {
                return Entity.NativeObject.delay.ToString();
            }
        }

        [CreateProperty]
        public string MaximumHeight
        {
            get
            {
                return $"Floor: {Entity.NativeObject.maximum_floor_height}  Ceiling: {Entity.NativeObject.maximum_ceiling_height}";
            }
        }

        [CreateProperty]
        public string MinimumHeight
        {
            get
            {
                return $"Floor: {Entity.NativeObject.minimum_floor_height}  Ceiling: {Entity.NativeObject.minimum_ceiling_height}";
            }
        }

        // The simulation's state, which (unlike the flags) can be changed here
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

        [CreateProperty]
        public bool InitiallyActive
        {
            get
            {
                return platforms.PLATFORM_IS_INITIALLY_ACTIVE(StaticFlags);
            }
        }

        [CreateProperty]
        public bool InitiallyExtended
        {
            get
            {
                return platforms.PLATFORM_IS_INITIALLY_EXTENDED(StaticFlags);
            }
        }

        [CreateProperty]
        public bool IsLocked
        {
            get
            {
                return platforms.PLATFORM_IS_LOCKED(StaticFlags);
            }
        }

        [CreateProperty]
        public bool IsPlayerControllable
        {
            get
            {
                return platforms.PLATFORM_IS_PLAYER_CONTROLLABLE(StaticFlags);
            }
        }

        [CreateProperty]
        public bool IsMonsterControllable
        {
            get
            {
                return platforms.PLATFORM_IS_MONSTER_CONTROLLABLE(StaticFlags);
            }
        }

        [CreateProperty]
        public bool CausesDamage
        {
            get
            {
                return platforms.PLATFORM_CAUSES_DAMAGE(StaticFlags);
            }
        }

        [CreateProperty]
        public bool ReversesWhenObstructed
        {
            get
            {
                return platforms.PLATFORM_REVERSES_DIRECTION_WHEN_OBSTRUCTED(StaticFlags);
            }
        }

        [CreateProperty]
        public bool DeactivatesAtEachLevel
        {
            get
            {
                return platforms.PLATFORM_DEACTIVATES_AT_EACH_LEVEL(StaticFlags);
            }
        }

        [CreateProperty]
        public bool DeactivatesAtInitialLevel
        {
            get
            {
                return platforms.PLATFORM_DEACTIVATES_AT_INITIAL_LEVEL(StaticFlags);
            }
        }

        [CreateProperty]
        public bool ActivatesAdjacentOnActivation
        {
            get
            {
                return platforms.PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_WHEN_ACTIVATING(StaticFlags);
            }
        }

        [CreateProperty]
        public bool ActivatesAdjacentOnDeactivation
        {
            get
            {
                return platforms.PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_WHEN_DEACTIVATING(StaticFlags);
            }
        }

        [CreateProperty]
        public bool DeactivatesAdjacentOnActivation
        {
            get
            {
                return platforms.PLATFORM_DEACTIVATES_ADJACENT_PLATFORMS_WHEN_ACTIVATING(StaticFlags);
            }
        }

        [CreateProperty]
        public bool DeactivatesAdjacentOnDeactivation
        {
            get
            {
                return platforms.PLATFORM_DEACTIVATES_ADJACENT_PLATFORMS_WHEN_DEACTIVATING(StaticFlags);
            }
        }

        [CreateProperty]
        public bool ActivatesAdjacentAtEachLevel
        {
            get
            {
                return platforms.PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_AT_EACH_LEVEL(StaticFlags);
            }
        }

        [CreateProperty]
        public bool DelaysBeforeActivation
        {
            get
            {
                return platforms.PLATFORM_DELAYS_BEFORE_ACTIVATION(StaticFlags);
            }
        }

        [CreateProperty]
        public bool ActivatesOnlyOnce
        {
            get
            {
                return platforms.PLATFORM_ACTIVATES_ONLY_ONCE(StaticFlags);
            }
        }

        [CreateProperty]
        public bool ActivatesLight
        {
            get
            {
                return platforms.PLATFORM_ACTIVATES_LIGHT(StaticFlags);
            }
        }

        [CreateProperty]
        public bool DeactivatesLight
        {
            get
            {
                return platforms.PLATFORM_DEACTIVATES_LIGHT(StaticFlags);
            }
        }

        [CreateProperty]
        public bool CannotBeExternallyDeactivated
        {
            get
            {
                return platforms.PLATFORM_CANNOT_BE_EXTERNALLY_DEACTIVATED(StaticFlags);
            }
        }

        [CreateProperty]
        public bool ContractsSlower
        {
            get
            {
                return platforms.PLATFORM_CONTRACTS_SLOWER(StaticFlags);
            }
        }

        [CreateProperty]
        public bool UsesNativePolygonHeights
        {
            get
            {
                return platforms.PLATFORM_USES_NATIVE_POLYGON_HEIGHTS(StaticFlags);
            }
        }

        [CreateProperty]
        public bool ExtendsFloorToCeiling
        {
            get
            {
                return platforms.PLATFORM_EXTENDS_FLOOR_TO_CEILING(StaticFlags);
            }
        }

        [CreateProperty]
        public bool ComesFromFloor
        {
            get
            {
                return platforms.PLATFORM_COMES_FROM_FLOOR(StaticFlags);
            }
        }

        [CreateProperty]
        public bool ComesFromCeiling
        {
            get
            {
                return platforms.PLATFORM_COMES_FROM_CEILING(StaticFlags);
            }
        }

        [CreateProperty]
        public bool DoesNotActivateParent
        {
            get
            {
                return platforms.PLATFORM_DOES_NOT_ACTIVATE_PARENT(StaticFlags);
            }
        }

        [CreateProperty]
        public bool IsSecret
        {
            get
            {
                return platforms.PLATFORM_IS_SECRET(StaticFlags);
            }
        }

        [CreateProperty]
        public bool IsDoor
        {
            get
            {
                return platforms.PLATFORM_IS_DOOR(StaticFlags);
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

            var simulationActive = Root.Find<Toggle>(nameof(SimulationActive));
            simulationActive.SetEnabled(true);
            simulationActive.BindValue(this, nameof(SimulationActive));

            Root.Find<Button>("obstruct").clicked += Entity.ObstructRuntimeBehavior;

            Entity.OnInspectionStateChange += OnInspectionStateChange;
        }

        protected override void OnUnloading()
        {
            Entity.OnInspectionStateChange -= OnInspectionStateChange;
        }

        private void OnInspectionStateChange(LevelEntity_Platform platform)
        {
            RefreshValuesInInspector();
        }
    }
}
