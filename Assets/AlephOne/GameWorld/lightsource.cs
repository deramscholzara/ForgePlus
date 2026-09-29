// Port of Aleph One: Source_Files/GameWorld/lightsource.h, lightsource.cpp (map data)
//
// Not ported (runtime only): updating lights (update_lights, change_light_state, rephase_light, the
// lighting functions, get_light_intensity) and switching them (set_light_status,
// set_tagged_light_statuses, get_light_status).
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csalerts;
using static AlephOne.csmacros;
using static AlephOne.cstypes;
using static AlephOne.map;
using static AlephOne.Packing;

namespace AlephOne
{
    /* ---------- static light data */

    /* as intensities, transition functions are given the primary periods of the active and inactive
        state, plus the intensity at the time of transition */
    public class lighting_function_specification /* 7*2 == 14 bytes */
    {
        public short function;

        public short period, delta_period;
        public int intensity, delta_intensity; // _fixed

        public lighting_function_specification() { }

        public lighting_function_specification(short function, short period, short delta_period, int intensity, int delta_intensity)
        {
            this.function = function;
            this.period = period;
            this.delta_period = delta_period;
            this.intensity = intensity;
            this.delta_intensity = delta_intensity;
        }

        public lighting_function_specification Clone() { return (lighting_function_specification) MemberwiseClone(); }
    }

    public class static_light_data /* size platform-specific */
    {
        public short type;
        public ushort flags;
        public short phase; // initializer, so lights may start out-of-phase with each other

        public lighting_function_specification primary_active = new lighting_function_specification(),
            secondary_active = new lighting_function_specification(),
            becoming_active = new lighting_function_specification();
        public lighting_function_specification primary_inactive = new lighting_function_specification(),
            secondary_inactive = new lighting_function_specification(),
            becoming_inactive = new lighting_function_specification();

        public short tag;

        public short[] unused = new short[4];

        public static_light_data() { }

        public static_light_data(short type, ushort flags, short phase,
            lighting_function_specification primary_active, lighting_function_specification secondary_active,
            lighting_function_specification becoming_active, lighting_function_specification primary_inactive,
            lighting_function_specification secondary_inactive, lighting_function_specification becoming_inactive)
        {
            this.type = type;
            this.flags = flags;
            this.phase = phase;
            this.primary_active = primary_active;
            this.secondary_active = secondary_active;
            this.becoming_active = becoming_active;
            this.primary_inactive = primary_inactive;
            this.secondary_inactive = secondary_inactive;
            this.becoming_inactive = becoming_inactive;
        }

        public static_light_data Clone()
        {
            var copy = (static_light_data) MemberwiseClone();
            copy.primary_active = primary_active.Clone();
            copy.secondary_active = secondary_active.Clone();
            copy.becoming_active = becoming_active.Clone();
            copy.primary_inactive = primary_inactive.Clone();
            copy.secondary_inactive = secondary_inactive.Clone();
            copy.becoming_inactive = becoming_inactive.Clone();
            copy.unused = (short[]) unused.Clone();
            return copy;
        }
    }

    /* ---------- dynamic light data */

    public class light_data /* 14*2 + 100 == 128 bytes */
    {
        public ushort flags;
        public short state;

        // result of lighting function
        public int intensity; // _fixed

        // data recalculated each function changed; passed to lighting_function each update
        public short phase, period;
        public int initial_intensity, final_intensity; // _fixed

        public short[] unused = new short[4];

        public static_light_data static_data = new static_light_data();

        public light_data Clone()
        {
            var copy = (light_data) MemberwiseClone();
            copy.unused = (short[]) unused.Clone();
            copy.static_data = static_data.Clone();
            return copy;
        }
    }

    /* Borrowed from the old lightsource.h, to allow Marathon II to open/use Marathon I maps */
    public class old_light_data
    {
        public ushort flags;

        public short type;
        public short mode; /* on, off, etc. */
        public short phase;

        public int minimum_intensity, maximum_intensity; // _fixed
        public short period; /* on, in ticks (turning on and off periods are always the same for a given light type,
            or else are some function of this period) */

        public int intensity; /* current intensity */ // _fixed

        public short[] unused = new short[5];
    }

    [NoAutoStaticsCleanup]
    public static class lightsource
    {
        /* ---------- constants */

        /* default light types */
        public const short _normal_light = 0;
        public const short _strobe_light = 1;
        public const short _media_light = 2;
        public const short NUMBER_OF_LIGHT_TYPES = 3;

        /* states */
        public const short _light_becoming_active = 0;
        public const short _light_primary_active = 1;
        public const short _light_secondary_active = 2;
        public const short _light_becoming_inactive = 3;
        public const short _light_primary_inactive = 4;
        public const short _light_secondary_inactive = 5;

        /* lighting functions */
        public const short _constant_lighting_function = 0; // maintain final intensity for period
        public const short _linear_lighting_function = 1; // linear transition between initial and final intensity over period
        public const short _smooth_lighting_function = 2; // sine transition between initial and final intensity over period
        public const short _flicker_lighting_function = 3; // intensity in [smooth_intensity(t),final_intensity]
        public const short _random_lighting_function = 4; // random intensity between initial and final,
        public const short _fluorescent_lighting_function = 5; // random on/off
        public const short NUMBER_OF_LIGHTING_FUNCTIONS = 6;

        /* static flags */
        public const int _light_is_initially_active = 0;
        public const int _light_has_slaved_intensities = 1;
        public const int _light_is_stateless = 2;
        public const int NUMBER_OF_STATIC_LIGHT_FLAGS = 3; /* <=16 */

        public static bool LIGHT_IS_INITIALLY_ACTIVE(static_light_data s) { return TEST_FLAG16((s).flags, _light_is_initially_active); }
        public static bool LIGHT_IS_STATELESS(static_light_data s) { return TEST_FLAG16((s).flags, _light_is_stateless); }

        public static void SET_LIGHT_IS_INITIALLY_ACTIVE(static_light_data s, bool v) { s.flags = SET_FLAG16((s).flags, _light_is_initially_active, (v)); }
        public static void SET_LIGHT_IS_STATELESS(static_light_data s, bool v) { s.flags = SET_FLAG16((s).flags, _light_is_stateless, (v)); }

        public const int SIZEOF_static_light_data = 100;

        public const int SIZEOF_light_data = 128;

        /* --------- Marathon 1 light definitions */

        /* old light types */
        public const short _light_is_normal = 0;
        public const short _light_is_rheostat = 1;
        public const short _light_is_flourescent = 2;
        public const short _light_is_strobe = 3;
        public const short _light_flickers = 4;
        public const short _light_pulsates = 5;
        public const short _light_is_annoying = 6;
        public const short _light_is_energy_efficient = 7;
        public const short NUMBER_OF_OLD_LIGHTS = 8;

        /* old light modes */
        public const short _light_mode_turning_on = 0;
        public const short _light_mode_on = 1;
        public const short _light_mode_turning_off = 2;
        public const short _light_mode_off = 3;
        public const short _light_mode_toggle = 4;

        public const int SIZEOF_old_light_data = 32;

        /* ---------- structures */

        private class light_definition
        {
            // it remains unclear where these sounds should come from
            public short on_sound, off_sound;
            public static_light_data defaults;

            public light_definition(short on_sound, short off_sound, static_light_data defaults)
            {
                this.on_sound = on_sound;
                this.off_sound = off_sound;
                this.defaults = defaults;
            }
        }

        /* ---------- globals */

        private static readonly light_definition[] light_definitions = new light_definition[NUMBER_OF_LIGHT_TYPES]
        {
            // _normal_light
            new light_definition(
                NONE, NONE, // on, off sound
                new static_light_data(
                    _normal_light, // type
                    (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0, // flags, phase
                    new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0), // primary_active
                    new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0), // secondary_active
                    new lighting_function_specification(_smooth_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0), // becoming_active
                    new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0), // primary_inactive
                    new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0), // secondary_inactive
                    new lighting_function_specification(_smooth_lighting_function, TICKS_PER_SECOND, 0, 0, 0) // becoming_inactive
                )
            ),

            // _strobe_light
            new light_definition(
                NONE, NONE, // on, off sound
                new static_light_data(
                    _normal_light, // type
                    (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0, // flags, phase
                    new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND / 2, 0, FIXED_ONE, 0), // primary_active
                    new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND / 2, 0, FIXED_ONE_HALF, 0), // secondary_active
                    new lighting_function_specification(_smooth_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE_HALF, 0), // becoming_active
                    new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0), // primary_inactive
                    new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0), // secondary_inactive
                    new lighting_function_specification(_smooth_lighting_function, TICKS_PER_SECOND, 0, 0, 0) // becoming_inactive
                )
            ),

            // _lava_light
            new light_definition(
                NONE, NONE, // on, off sound
                new static_light_data(
                    _normal_light, // type
                    (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0, // flags, phase
                    new lighting_function_specification(_smooth_lighting_function, 10 * TICKS_PER_SECOND, 0, FIXED_ONE, 0), // primary_active
                    new lighting_function_specification(_smooth_lighting_function, 10 * TICKS_PER_SECOND, 0, 0, 0), // secondary_active
                    new lighting_function_specification(_smooth_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE_HALF, 0), // becoming_active
                    new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0), // primary_inactive
                    new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0), // secondary_inactive
                    new lighting_function_specification(_smooth_lighting_function, TICKS_PER_SECOND, 0, 0, 0) // becoming_inactive
                )
            ),
        };

        /* ---------- code */

        public static light_data get_light_data(MapLevel level, int light_index)
        {
            light_data light = GetMemberWithBounds(level.LightList, light_index, level.LightList.Count);
            if (light == null) return null;
            if (!SLOT_IS_USED(light.flags)) return null;
            return light;
        }

        // LP change: moved down here because it uses light definitions
        private static light_definition get_light_definition(short type)
        {
            return GetMemberWithBounds(light_definitions, type, NUMBER_OF_LIGHT_TYPES);
        }

        public static short new_light(MapLevel level, static_light_data data)
        {
            int light_index;
            light_data light;

            // LP change: idiot-proofing
            if (data == null) return NONE;

            for (light_index = 0; light_index < (short) level.LightList.Count; ++light_index)
            {
                light = level.LightList[light_index];
                if (SLOT_IS_FREE(light.flags))
                {
                    light.static_data = data.Clone();
                    // light->flags= 0;
                    MARK_SLOT_AS_USED(ref light.flags);

                    light.intensity = 0;
                    // change_light_state(light_index, LIGHT_IS_INITIALLY_ACTIVE(data) ? _light_secondary_active : _light_secondary_inactive);
                    // light->intensity= light->final_intensity; ... rephase_light(light_index); light->intensity= lighting_function_dispatch(...);
                    break;
                }
            }

            if (light_index == (short) level.LightList.Count) light_index = NONE;

            return (short) light_index;
        }

        public static static_light_data get_defaults_for_light_type(short type)
        {
            light_definition definition = get_light_definition(type);
            // LP addition: idiot-proofing
            if (definition == null) return null;

            return definition.defaults;
        }

        public static void unpack_old_light_data(StreamPointer S, IList<old_light_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                old_light_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.flags);
                StreamToValue(S, out ObjPtr.type);
                StreamToValue(S, out ObjPtr.mode);
                StreamToValue(S, out ObjPtr.phase);
                StreamToValue(S, out ObjPtr.minimum_intensity);
                StreamToValue(S, out ObjPtr.maximum_intensity);
                StreamToValue(S, out ObjPtr.period);
                StreamToValue(S, out ObjPtr.intensity);
                S.Skip(5 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_old_light_data);
        }

        public static void pack_old_light_data(StreamPointer S, IList<old_light_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                old_light_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.flags);
                ValueToStream(S, ObjPtr.type);
                ValueToStream(S, ObjPtr.mode);
                ValueToStream(S, ObjPtr.phase);
                ValueToStream(S, ObjPtr.minimum_intensity);
                ValueToStream(S, ObjPtr.maximum_intensity);
                ValueToStream(S, ObjPtr.period);
                ValueToStream(S, ObjPtr.intensity);
                S.Skip(5 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_old_light_data);
        }

        private static void StreamToLightSpec(StreamPointer S, lighting_function_specification Object)
        {
            StreamToValue(S, out Object.function);
            StreamToValue(S, out Object.period);
            StreamToValue(S, out Object.delta_period);
            StreamToValue(S, out Object.intensity);
            StreamToValue(S, out Object.delta_intensity);
        }

        private static void LightSpecToStream(StreamPointer S, lighting_function_specification Object)
        {
            ValueToStream(S, Object.function);
            ValueToStream(S, Object.period);
            ValueToStream(S, Object.delta_period);
            ValueToStream(S, Object.intensity);
            ValueToStream(S, Object.delta_intensity);
        }

        public static void unpack_static_light_data(StreamPointer S, IList<static_light_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                static_light_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.type);
                StreamToValue(S, out ObjPtr.flags);
                StreamToValue(S, out ObjPtr.phase);
                StreamToLightSpec(S, ObjPtr.primary_active);
                StreamToLightSpec(S, ObjPtr.secondary_active);
                StreamToLightSpec(S, ObjPtr.becoming_active);
                StreamToLightSpec(S, ObjPtr.primary_inactive);
                StreamToLightSpec(S, ObjPtr.secondary_inactive);
                StreamToLightSpec(S, ObjPtr.becoming_inactive);
                StreamToValue(S, out ObjPtr.tag);
                S.Skip(4 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_static_light_data);
        }

        public static void pack_static_light_data(StreamPointer S, IList<static_light_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                static_light_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.type);
                ValueToStream(S, ObjPtr.flags);
                ValueToStream(S, ObjPtr.phase);
                LightSpecToStream(S, ObjPtr.primary_active);
                LightSpecToStream(S, ObjPtr.secondary_active);
                LightSpecToStream(S, ObjPtr.becoming_active);
                LightSpecToStream(S, ObjPtr.primary_inactive);
                LightSpecToStream(S, ObjPtr.secondary_inactive);
                LightSpecToStream(S, ObjPtr.becoming_inactive);
                ValueToStream(S, ObjPtr.tag);
                S.Skip(4 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_static_light_data);
        }

        public static void unpack_light_data(StreamPointer S, IList<light_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                light_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.flags);
                StreamToValue(S, out ObjPtr.state);
                StreamToValue(S, out ObjPtr.intensity);
                StreamToValue(S, out ObjPtr.phase);
                StreamToValue(S, out ObjPtr.period);
                StreamToValue(S, out ObjPtr.initial_intensity);
                StreamToValue(S, out ObjPtr.final_intensity);
                S.Skip(4 * 2);
                unpack_static_light_data(S, new[] { ObjPtr.static_data }, 1);
            }

            assert((S.Position - Stream) == Count * SIZEOF_light_data);
        }

        public static void pack_light_data(StreamPointer S, IList<light_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                light_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.flags);
                ValueToStream(S, ObjPtr.state);
                ValueToStream(S, ObjPtr.intensity);
                ValueToStream(S, ObjPtr.phase);
                ValueToStream(S, ObjPtr.period);
                ValueToStream(S, ObjPtr.initial_intensity);
                ValueToStream(S, ObjPtr.final_intensity);
                S.Skip(4 * 2);
                pack_static_light_data(S, new[] { ObjPtr.static_data }, 1);
            }

            assert((S.Position - Stream) == Count * SIZEOF_light_data);
        }

        private static void FixIntensity(lighting_function_specification LightState, old_light_data OldLight)
        {
            if (LightState.intensity > 0)
            {
                LightState.intensity = OldLight.maximum_intensity;
            }
            else
            {
                LightState.intensity = OldLight.minimum_intensity;
            }
        }

        private static readonly static_light_data[] old_light_definitions = new static_light_data[NUMBER_OF_OLD_LIGHTS]
        {
            // _light_is_normal
            new static_light_data(
                _normal_light,
                (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0,
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, 1, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, 1, 0, FIXED_ONE, 0)
            ),

            // _light_is_rheostat
            new static_light_data(
                _normal_light,
                (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0,
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_smooth_lighting_function, 3 * TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_smooth_lighting_function, 3 * TICKS_PER_SECOND, 0, 0, 0)
            ),

            // _light_is_flourescent
            new static_light_data(
                _normal_light,
                (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0,
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_fluorescent_lighting_function, 3 * TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, 1, 0, 0, 0)
            ),

            // _light_is_strobe
            new static_light_data(
                _normal_light,
                (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0,
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, 1, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, 1, 0, 0, 0)
            ),

            // _light_flickers
            new static_light_data(
                _normal_light,
                (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0,
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_flicker_lighting_function, 3 * TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, 1, 0, 0, 0)
            ),

            // _light_pulsates
            new static_light_data(
                _normal_light,
                (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0,
                new lighting_function_specification(_smooth_lighting_function, 2 * TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_smooth_lighting_function, 2 * TICKS_PER_SECOND - 1, 0, 0, 0),
                new lighting_function_specification(_smooth_lighting_function, 2 * TICKS_PER_SECOND - 1, 0, FIXED_ONE, 0),
                new lighting_function_specification(_smooth_lighting_function, 2 * TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_smooth_lighting_function, 2 * TICKS_PER_SECOND - 1, 0, FIXED_ONE, 0),
                new lighting_function_specification(_smooth_lighting_function, 2 * TICKS_PER_SECOND, 0, 0, 0)
            ),

            // _light_is_annoying
            new static_light_data(
                _normal_light,
                (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0,
                new lighting_function_specification(_random_lighting_function, 2, 1, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, 2, 0, 0, 0),
                new lighting_function_specification(_random_lighting_function, 1, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0)
            ),

            // _light_is_energy_efficient
            new static_light_data(
                _normal_light,
                (ushort) (FLAG(_light_is_initially_active) | FLAG(_light_has_slaved_intensities)), 0,
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_linear_lighting_function, 2 * TICKS_PER_SECOND, 0, FIXED_ONE, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_constant_lighting_function, TICKS_PER_SECOND, 0, 0, 0),
                new lighting_function_specification(_linear_lighting_function, 2 * TICKS_PER_SECOND, 0, 0, 0)
            )
        };

        public static void convert_old_light_data_to_new(static_light_data[] NewLights, old_light_data[] OldLights, int Count)
        {
            // LP: code taken from game_wad.c and somewhat modified

            for (int k = 0; k < Count; k++)
            {
                old_light_data OldLtPtr = OldLights[k];
                // obj_copy(*NewLtPtr, old_light_definitions[OldLtPtr->type]);
                static_light_data NewLtPtr = NewLights[k] = old_light_definitions[OldLtPtr.type].Clone();
                FixIntensity(NewLtPtr.primary_active, OldLtPtr);
                FixIntensity(NewLtPtr.secondary_active, OldLtPtr);
                FixIntensity(NewLtPtr.becoming_active, OldLtPtr);
                FixIntensity(NewLtPtr.primary_inactive, OldLtPtr);
                FixIntensity(NewLtPtr.secondary_inactive, OldLtPtr);
                FixIntensity(NewLtPtr.becoming_inactive, OldLtPtr);

                if (OldLtPtr.type == _light_is_strobe)
                {
                    NewLtPtr.primary_active.period = (short) (OldLtPtr.period / 4 + 1);
                    NewLtPtr.secondary_active.period = (short) (OldLtPtr.period / 4 + 1);
                    NewLtPtr.primary_inactive.period = (short) (OldLtPtr.period / 4 + 1);
                    NewLtPtr.secondary_inactive.period = (short) (OldLtPtr.period / 4 + 1);
                }

                switch (OldLtPtr.mode)
                {
                    case _light_mode_on:
                    case _light_mode_turning_on:
                        NewLtPtr.flags = (ushort) SET_FLAG(NewLtPtr.flags, FLAG(_light_is_initially_active), true);
                        break;
                    case _light_mode_off:
                    default:
                        NewLtPtr.flags = (ushort) SET_FLAG(NewLtPtr.flags, FLAG(_light_is_initially_active), false);
                        break;
                }
            }
        }
    }
}
