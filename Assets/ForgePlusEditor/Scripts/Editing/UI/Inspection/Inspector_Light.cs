using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using ForgePlus.Palette;
using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    // A light's static data (lightsource.h: static_light_data), which the level's light runs from: an edit restarts the
    // light, so the level shows it
    public class Inspector_Light : Inspector_Base<LevelEntity_Light>
    {
        public Inspector_Light(LevelEntity_Light light) : base(light)
        {
            BecomingActive = new LightStateView(this, data => data.becoming_active);
            PrimaryActive = new LightStateView(this, data => data.primary_active);
            SecondaryActive = new LightStateView(this, data => data.secondary_active);
            BecomingInactive = new LightStateView(this, data => data.becoming_inactive);
            PrimaryInactive = new LightStateView(this, data => data.primary_inactive);
            SecondaryInactive = new LightStateView(this, data => data.secondary_inactive);
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Light";
            }
        }

        private static_light_data Light
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

        // What tag switches and terminals switch it by
        [CreateProperty]
        public int Tag
        {
            get
            {
                return Light.tag;
            }
            set
            {
                EditLight(light => light.tag = ClampToShort(value));
            }
        }

        // Mostly a preset of the six states: choosing one offers to reset them to the type's defaults
        [CreateProperty]
        public string Type
        {
            get
            {
                return AlephOneNames.LightType(Light.type);
            }
            set
            {
                if (TryFindChoice(ShortRange(0, lightsource.NUMBER_OF_LIGHT_TYPES), AlephOneNames.LightType, value, out var type) && type != Light.type)
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
                return ChoicesOf(ShortRange(0, lightsource.NUMBER_OF_LIGHT_TYPES), AlephOneNames.LightType);
            }
        }

        // How many ticks into its states it starts (so lights can be out of step with each other)
        [CreateProperty]
        public int Phase
        {
            get
            {
                return Light.phase;
            }
            set
            {
                EditLight(light => light.phase = ClampToNonNegativeShort(value));
            }
        }

        [CreateProperty]
        public bool StartsActive
        {
            get
            {
                return lightsource.LIGHT_IS_INITIALLY_ACTIVE(Light);
            }
            set
            {
                EditLight(light => lightsource.SET_LIGHT_IS_INITIALLY_ACTIVE(light, value));
            }
        }

        [CreateProperty]
        public bool SlavedIntensities
        {
            get
            {
                return csmacros.TEST_FLAG16(Light.flags, lightsource._light_has_slaved_intensities);
            }
            set
            {
                EditLight(light => light.flags = csmacros.SET_FLAG16(light.flags, lightsource._light_has_slaved_intensities, value));
            }
        }

        // Runs through all six states in turn (rather than staying active or inactive)
        [CreateProperty]
        public bool CycleAllStates
        {
            get
            {
                return lightsource.LIGHT_IS_STATELESS(Light);
            }
            set
            {
                EditLight(light => lightsource.SET_LIGHT_IS_STATELESS(light, value));
            }
        }

        public LightStateView BecomingActive { get; }

        public LightStateView PrimaryActive { get; }

        public LightStateView SecondaryActive { get; }

        public LightStateView BecomingInactive { get; }

        public LightStateView PrimaryInactive { get; }

        public LightStateView SecondaryInactive { get; }

        // Changes the light, then restarts it (so the level shows the change) and its palette swatch
        private void EditLight(Action<static_light_data> edit)
        {
            Edit(light =>
            {
                edit(light.NativeObject);
                light.BeginRuntimeStyleBehavior();
            });

            PaletteManager.Instance.RefreshSwatches();
        }

        // Its type changes alone, unless its states are reset to the type's defaults (lightsource.cpp:
        // get_defaults_for_light_type)
        private async void SetType(short type)
        {
            EditLight(light => light.type = type);

            var defaults = lightsource.get_defaults_for_light_type(type);
            if (defaults == null)
            {
                return;
            }

            var option = await DialogManager.Instance.DisplayQueuedDialog(
                Strings.Get(Strings.Lights, "Inspector.Light.ResetStates.Title", Entity.NativeIndex, AlephOneNames.LightType(type)),
                new[] { "Reset" },
                new[] { Strings.Get(Strings.Lights, "Inspector.Light.ResetStates.Confirm") });

            if (option != "Reset")
            {
                return;
            }

            EditLight(light =>
            {
                CopyState(defaults.becoming_active, light.becoming_active);
                CopyState(defaults.primary_active, light.primary_active);
                CopyState(defaults.secondary_active, light.secondary_active);
                CopyState(defaults.becoming_inactive, light.becoming_inactive);
                CopyState(defaults.primary_inactive, light.primary_inactive);
                CopyState(defaults.secondary_inactive, light.secondary_inactive);
            });
        }

        private static void CopyState(lighting_function_specification source, lighting_function_specification destination)
        {
            destination.function = source.function;
            destination.period = source.period;
            destination.delta_period = source.delta_period;
            destination.intensity = source.intensity;
            destination.delta_intensity = source.delta_intensity;
        }

        // One of the light's six states, which the inspector's rows in the scope of its name are bound to
        public class LightStateView : IDataSourceViewHashProvider
        {
            private readonly Inspector_Light inspector;
            private readonly Func<static_light_data, lighting_function_specification> stateOf;

            public LightStateView(Inspector_Light inspector, Func<static_light_data, lighting_function_specification> stateOf)
            {
                this.inspector = inspector;
                this.stateOf = stateOf;
            }

            [CreateProperty]
            public string Function
            {
                get
                {
                    return AlephOneNames.LightingFunction(State.function);
                }
                set
                {
                    if (TryFindChoice(ShortRange(0, lightsource.NUMBER_OF_LIGHTING_FUNCTIONS), AlephOneNames.LightingFunction, value, out var function))
                    {
                        EditState(state => state.function = function);
                    }
                }
            }

            [CreateProperty]
            public List<string> FunctionChoices
            {
                get
                {
                    return ChoicesOf(ShortRange(0, lightsource.NUMBER_OF_LIGHTING_FUNCTIONS), AlephOneNames.LightingFunction);
                }
            }

            // Random and fluorescent are Aleph One's (added for Marathon 1's lights)
            [CreateProperty]
            public List<string> FunctionAlephOneOnlyChoices
            {
                get
                {
                    return new List<string>
                    {
                        AlephOneNames.LightingFunction(lightsource._random_lighting_function),
                        AlephOneNames.LightingFunction(lightsource._fluorescent_lighting_function),
                    };
                }
            }

            [CreateProperty]
            public int Period
            {
                get
                {
                    return State.period;
                }
                set
                {
                    EditState(state => state.period = ClampToNonNegativeShort(value));
                }
            }

            [CreateProperty]
            public int DeltaPeriod
            {
                get
                {
                    return State.delta_period;
                }
                set
                {
                    EditState(state => state.delta_period = ClampToNonNegativeShort(value));
                }
            }

            [CreateProperty]
            public float Intensity
            {
                get
                {
                    return DisplayedIntensity(State.intensity);
                }
                set
                {
                    EditState(state => state.intensity = FixedIntensity(value));
                }
            }

            [CreateProperty]
            public float DeltaIntensity
            {
                get
                {
                    return DisplayedIntensity(State.delta_intensity);
                }
                set
                {
                    EditState(state => state.delta_intensity = FixedIntensity(value));
                }
            }

            private lighting_function_specification State
            {
                get
                {
                    return stateOf(inspector.Light);
                }
            }

            public long GetViewHashCode()
            {
                return inspector.GetViewHashCode();
            }

            private void EditState(Action<lighting_function_specification> edit)
            {
                inspector.EditLight(light => edit(stateOf(light)));
            }
        }
    }
}
