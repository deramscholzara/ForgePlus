using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_Light : Inspector_Base<LevelEntity_Light>
    {
        public Inspector_Light(LevelEntity_Light light) : base(light)
        {
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

        [CreateProperty]
        public string Tag
        {
            get
            {
                return Light.tag.ToString();
            }
        }

        [CreateProperty]
        public string Type
        {
            get
            {
                return AlephOneNames.LightType(Light.type);
            }
        }

        [CreateProperty]
        public string Phase
        {
            get
            {
                return Light.phase.ToString();
            }
        }

        [CreateProperty]
        public bool StartsActive
        {
            get
            {
                return lightsource.LIGHT_IS_INITIALLY_ACTIVE(Light);
            }
        }

        [CreateProperty]
        public bool SlavedIntensities
        {
            get
            {
                return csmacros.TEST_FLAG16(Light.flags, lightsource._light_has_slaved_intensities);
            }
        }

        [CreateProperty]
        public bool CycleAllStates
        {
            get
            {
                return lightsource.LIGHT_IS_STATELESS(Light);
            }
        }

        [CreateProperty]
        public string BecomingActiveFunction
        {
            get
            {
                return Function(Light.becoming_active);
            }
        }

        [CreateProperty]
        public string BecomingActivePeriod
        {
            get
            {
                return Light.becoming_active.period.ToString();
            }
        }

        [CreateProperty]
        public string BecomingActiveDeltaPeriod
        {
            get
            {
                return Light.becoming_active.delta_period.ToString();
            }
        }

        [CreateProperty]
        public string BecomingActiveIntensity
        {
            get
            {
                return Intensity(Light.becoming_active.intensity);
            }
        }

        [CreateProperty]
        public string BecomingActiveDeltaIntensity
        {
            get
            {
                return Intensity(Light.becoming_active.delta_intensity);
            }
        }

        [CreateProperty]
        public string PrimaryActiveFunction
        {
            get
            {
                return Function(Light.primary_active);
            }
        }

        [CreateProperty]
        public string PrimaryActivePeriod
        {
            get
            {
                return Light.primary_active.period.ToString();
            }
        }

        [CreateProperty]
        public string PrimaryActiveDeltaPeriod
        {
            get
            {
                return Light.primary_active.delta_period.ToString();
            }
        }

        [CreateProperty]
        public string PrimaryActiveIntensity
        {
            get
            {
                return Intensity(Light.primary_active.intensity);
            }
        }

        [CreateProperty]
        public string PrimaryActiveDeltaIntensity
        {
            get
            {
                return Intensity(Light.primary_active.delta_intensity);
            }
        }

        [CreateProperty]
        public string SecondaryActiveFunction
        {
            get
            {
                return Function(Light.secondary_active);
            }
        }

        [CreateProperty]
        public string SecondaryActivePeriod
        {
            get
            {
                return Light.secondary_active.period.ToString();
            }
        }

        [CreateProperty]
        public string SecondaryActiveDeltaPeriod
        {
            get
            {
                return Light.secondary_active.delta_period.ToString();
            }
        }

        [CreateProperty]
        public string SecondaryActiveIntensity
        {
            get
            {
                return Intensity(Light.secondary_active.intensity);
            }
        }

        [CreateProperty]
        public string SecondaryActiveDeltaIntensity
        {
            get
            {
                return Intensity(Light.secondary_active.delta_intensity);
            }
        }

        [CreateProperty]
        public string BecomingInactiveFunction
        {
            get
            {
                return Function(Light.becoming_inactive);
            }
        }

        [CreateProperty]
        public string BecomingInactivePeriod
        {
            get
            {
                return Light.becoming_inactive.period.ToString();
            }
        }

        [CreateProperty]
        public string BecomingInactiveDeltaPeriod
        {
            get
            {
                return Light.becoming_inactive.delta_period.ToString();
            }
        }

        [CreateProperty]
        public string BecomingInactiveIntensity
        {
            get
            {
                return Intensity(Light.becoming_inactive.intensity);
            }
        }

        [CreateProperty]
        public string BecomingInactiveDeltaIntensity
        {
            get
            {
                return Intensity(Light.becoming_inactive.delta_intensity);
            }
        }

        [CreateProperty]
        public string PrimaryInactiveFunction
        {
            get
            {
                return Function(Light.primary_inactive);
            }
        }

        [CreateProperty]
        public string PrimaryInactivePeriod
        {
            get
            {
                return Light.primary_inactive.period.ToString();
            }
        }

        [CreateProperty]
        public string PrimaryInactiveDeltaPeriod
        {
            get
            {
                return Light.primary_inactive.delta_period.ToString();
            }
        }

        [CreateProperty]
        public string PrimaryInactiveIntensity
        {
            get
            {
                return Intensity(Light.primary_inactive.intensity);
            }
        }

        [CreateProperty]
        public string PrimaryInactiveDeltaIntensity
        {
            get
            {
                return Intensity(Light.primary_inactive.delta_intensity);
            }
        }

        [CreateProperty]
        public string SecondaryInactiveFunction
        {
            get
            {
                return Function(Light.secondary_inactive);
            }
        }

        [CreateProperty]
        public string SecondaryInactivePeriod
        {
            get
            {
                return Light.secondary_inactive.period.ToString();
            }
        }

        [CreateProperty]
        public string SecondaryInactiveDeltaPeriod
        {
            get
            {
                return Light.secondary_inactive.delta_period.ToString();
            }
        }

        [CreateProperty]
        public string SecondaryInactiveIntensity
        {
            get
            {
                return Intensity(Light.secondary_inactive.intensity);
            }
        }

        [CreateProperty]
        public string SecondaryInactiveDeltaIntensity
        {
            get
            {
                return Intensity(Light.secondary_inactive.delta_intensity);
            }
        }

        private static string Function(lighting_function_specification stateFunction)
        {
            return AlephOneNames.LightingFunction(stateFunction.function);
        }

        private static string Intensity(int fixedIntensity)
        {
            return AlephOneExtensions.FixedToFloat(fixedIntensity).ToString();
        }
    }
}
