// Port of Aleph One: Source_Files/Sound/SoundManager.h, SoundManager.cpp (sound definition lookups, and what decides
// which ambient sounds play and how loud, and random sounds' permutations and pitches)
//
// Not ported: playing sound (the SoundManager class's players, channels and OpenAL), which ForgePlus does with what
// these decide; sound patches and replacements (sounds_patches, SoundReplacements), so a sound is always the sounds
// file's; and MML parsing (parse_mml_sounds, reset_mml_sounds), so the ambient and random tables are always the
// originals.
using System;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.sound_definitions;
using static AlephOne.SoundManagerEnums;

namespace AlephOne
{
    public class ambient_sound_data
    {
        public ushort flags;
        public short sound_index;
        public SoundManager.SoundVolumes variables;
    }

    [NoAutoStaticsCleanup]
    public static class SoundManager
    {
        public const int MAXIMUM_AMBIENT_SOUND_CHANNELS = 4;
        public const int MAXIMUM_PROCESSED_AMBIENT_SOUNDS = 5;

        // MAXIMUM_SOUND_VOLUME is 256, NUMBER_OF_SOUND_VOLUME_LEVELS is 8
        public const int MAXIMUM_AMBIENT_SOUND_VOLUME = 3 * MAXIMUM_SOUND_VOLUME / 2;

        public const int ABORT_AMPLITUDE_THRESHHOLD = (MAXIMUM_SOUND_VOLUME / 6);

        public struct SoundVolumes
        {
            public short volume, left_volume, right_volume;
        }

        public static ambient_sound_definition get_ambient_sound_definition(short ambient_sound_index)
        {
            return csmacros.GetMemberWithBounds(ambient_sound_definitions, ambient_sound_index, NUMBER_OF_AMBIENT_SOUND_DEFINITIONS);
        }

        public static random_sound_definition get_random_sound_definition(short random_sound_index)
        {
            return csmacros.GetMemberWithBounds(random_sound_definitions, random_sound_index, NUMBER_OF_RANDOM_SOUND_DEFINITIONS);
        }

        public static sound_behavior_definition get_sound_behavior_definition(short sound_behavior_index)
        {
            return csmacros.GetMemberWithBounds(sound_behavior_definitions, sound_behavior_index, NUMBER_OF_SOUND_BEHAVIOR_DEFINITIONS);
        }

        // short SoundManager::RandomSoundIndexToSoundIndex(short random_sound_index)
        public static short RandomSoundIndexToSoundIndex(short random_sound_index)
        {
            random_sound_definition definition = get_random_sound_definition(random_sound_index);
            return definition != null ? definition.sound_index : cstypes.NONE;
        }

        // ForgePlus: from the 16-bit source, as with Aleph One's default preferences (_16bit_sound_flag), falling back
        // to the 8-bit source when the 16-bit one has no permutations of it
        public static SoundDefinition GetSoundDefinition(SoundFile sound_file, short sound_index)
        {
            short sound_source = sound_file.SourceCount() > _16bit_22k_source ? _16bit_22k_source : _8bit_22k_source;

            SoundDefinition sound_definition = sound_file.GetSoundDefinition(sound_source, sound_index);
            if (sound_source == _16bit_22k_source &&
                sound_definition != null &&
                sound_definition.permutations == 0)
            {
                sound_definition = sound_file.GetSoundDefinition(_8bit_22k_source, sound_index);
            }

            return sound_definition;
        }

        public static short distance_to_volume(SoundDefinition definition, short distance, ushort flags)
        {
            sound_behavior_definition behavior = get_sound_behavior_definition(definition.behavior_index);
            // LP change: idiot-proofing
            if (behavior == null) return 0; // Silence

            depth_curve_definition depth_curve;
            short volume;

            if (((flags & _sound_was_obstructed) != 0 && (definition.flags & _sound_cannot_be_obstructed) == 0) ||
                ((flags & _sound_was_media_obstructed) != 0 && (definition.flags & _sound_cannot_be_media_obstructed) == 0))
            {
                depth_curve = behavior.obstructed_curve;
            }
            else
            {
                depth_curve = behavior.unobstructed_curve;
            }

            if (distance <= depth_curve.maximum_volume_distance)
            {
                volume = depth_curve.maximum_volume;
            }
            else
            {
                if (distance > depth_curve.minimum_volume_distance)
                {
                    volume = depth_curve.minimum_volume;
                }
                else
                {
                    volume = (short) (depth_curve.minimum_volume - ((depth_curve.minimum_volume - depth_curve.maximum_volume) * (depth_curve.minimum_volume_distance - distance)) /
                        (depth_curve.minimum_volume_distance - depth_curve.maximum_volume_distance));
                }
            }

            if ((flags & _sound_was_media_muffled) != 0 && (definition.flags & _sound_cannot_be_media_obstructed) == 0)
            {
                volume >>= 1;
            }

            return volume;
        }

        // Adds a source of the ambient sound (none for one heard everywhere, like a polygon's) to the ambient sounds:
        // sources of one sound add up, as loud as it is at each (its absolute volume, and how far away and obstructed it is)
        // ForgePlus: with the sounds file, and _sound_obstructed_proc, passed in
        public static void AddOneAmbientSoundSource(SoundFile sound_file, ambient_sound_data[] ambient_sounds, world_location3d? source,
            world_location3d listener, short ambient_sound_index, short absolute_volume, Func<world_location3d, ushort> sound_obstructed_proc)
        {
            if (ambient_sound_index == cstypes.NONE) return;

            // LP change; make NONE in case this sound definition is invalid
            ambient_sound_definition SoundDef = get_ambient_sound_definition(ambient_sound_index);
            short sound_index = (SoundDef != null) ? SoundDef.sound_index : cstypes.NONE;

            if (sound_index == cstypes.NONE) return;

            SoundDefinition definition = GetSoundDefinition(sound_file, sound_index);

            // LP change: idiot-proofing
            if (definition == null || definition.sound_code == cstypes.NONE) return;

            sound_behavior_definition behavior = get_sound_behavior_definition(definition.behavior_index);
            // LP change: idiot-proofing
            if (behavior == null) return; // Silence

            ambient_sound_data ambient = null;
            short distance = 0;
            short i;

            if (source.HasValue)
            {
                distance = world.distance3d(listener.point, source.Value.point);
            }

            for (i = 0; i < MAXIMUM_PROCESSED_AMBIENT_SOUNDS; ++i)
            {
                ambient = ambient_sounds[i];

                if (map.SLOT_IS_USED(ambient.flags))
                {
                    if (ambient.sound_index == sound_index) break;
                }
                else
                {
                    map.MARK_SLOT_AS_USED(ref ambient.flags);

                    ambient.sound_index = sound_index;
                    ambient.variables.volume = ambient.variables.left_volume = ambient.variables.right_volume = 0;
                    break;
                }
            }

            if (i == MAXIMUM_PROCESSED_AMBIENT_SOUNDS) return;
            if (source.HasValue && distance >= behavior.unobstructed_curve.minimum_volume_distance) return;

            short volume, left_volume, right_volume;

            if (source.HasValue)
            {
                // LP change: made this long-distance friendly
                int dx = (int) listener.point.x - (int) source.Value.point.x;
                int dy = (int) listener.point.y - (int) source.Value.point.y;

                volume = distance_to_volume(definition, distance, sound_obstructed_proc(source.Value));
                volume = (short) ((absolute_volume * volume) >> MAXIMUM_SOUND_VOLUME_BITS);

                if (dx != 0 || dy != 0)
                {
                    AngleAndVolumeToStereoVolume((short) (world.arctangent(dx, dy) - listener.yaw), volume, out right_volume, out left_volume);
                }
                else
                {
                    left_volume = right_volume = volume;
                }
            }
            else
            {
                volume = left_volume = right_volume = absolute_volume;
            }

            int maximum_volume = csmacros.MAX(MAXIMUM_AMBIENT_SOUND_VOLUME, volume);
            int maximum_left_volume = csmacros.MAX(MAXIMUM_AMBIENT_SOUND_VOLUME, left_volume);
            int maximum_right_volume = csmacros.MAX(MAXIMUM_AMBIENT_SOUND_VOLUME, right_volume);

            ambient.variables.volume = (short) csmacros.MIN(ambient.variables.volume + volume, maximum_volume);
            ambient.variables.left_volume = (short) csmacros.MIN(ambient.variables.left_volume + left_volume, maximum_left_volume);
            ambient.variables.right_volume = (short) csmacros.MIN(ambient.variables.right_volume + right_volume, maximum_right_volume);
        }

        // ForgePlus: the part of UpdateAmbientSoundSources that decides which ambient sounds play (up to
        // MAXIMUM_AMBIENT_SOUND_CHANNELS of them), and how loud, into ambient_sounds (MAXIMUM_PROCESSED_AMBIENT_SOUNDS
        // of them); starting, updating and stopping their players is ForgePlus's
        public static void UpdateAmbientSoundSources(ambient_sound_data[] ambient_sounds, Action<ambient_sound_data[]> sound_add_ambient_sources_proc)
        {
            // reset all local copies
            for (short i = 0; i < MAXIMUM_PROCESSED_AMBIENT_SOUNDS; i++)
            {
                ambient_sounds[i].flags = 0;
                ambient_sounds[i].sound_index = cstypes.NONE;
            }

            // accumulate up to MAXIMUM_PROCESSED_AMBIENT_SOUNDS worth of sounds
            sound_add_ambient_sources_proc(ambient_sounds);

            // remove all zero volume sounds
            for (short i = 0; i < MAXIMUM_PROCESSED_AMBIENT_SOUNDS; i++)
            {
                ambient_sound_data ambient = ambient_sounds[i];
                if (map.SLOT_IS_USED(ambient.flags) && ambient.variables.volume == 0)
                    map.MARK_SLOT_AS_FREE(ref ambient.flags);
            }

            {
                ambient_sound_data lowest_priority;
                short count;

                do
                {
                    lowest_priority = null;
                    count = 0;

                    for (short i = 0; i < MAXIMUM_PROCESSED_AMBIENT_SOUNDS; i++)
                    {
                        ambient_sound_data ambient = ambient_sounds[i];
                        if (map.SLOT_IS_USED(ambient.flags))
                        {
                            if (lowest_priority == null || lowest_priority.variables.volume > ambient.variables.volume + ABORT_AMPLITUDE_THRESHHOLD)
                            {
                                lowest_priority = ambient;
                            }

                            count++;
                        }
                    }

                    if (count > MAXIMUM_AMBIENT_SOUND_CHANNELS)
                    {
                        csalerts.assert(lowest_priority != null);
                        map.MARK_SLOT_AS_FREE(ref lowest_priority.flags);
                        count--;
                    }
                } while (count > MAXIMUM_AMBIENT_SOUND_CHANNELS);
            }
        }

        // How loud a sound from the source is at the listener (by how far away and obstructed it is), and on each side
        // (CalculateInitialSoundVariables, with a source). ForgePlus: with the sounds file, and _sound_obstructed_proc,
        // passed in
        public static SoundVolumes CalculateSoundVariables(SoundFile sound_file, short sound_index, world_location3d source,
            world_location3d listener, Func<world_location3d, ushort> sound_obstructed_proc)
        {
            var variables = new SoundVolumes();

            SoundDefinition definition = GetSoundDefinition(sound_file, sound_index);
            if (definition == null) return variables;

            short distance = world.distance3d(source.point, listener.point);

            // LP change: made this long-distance friendly
            int dx = (int) listener.point.x - (int) source.point.x;
            int dy = (int) listener.point.y - (int) source.point.y;

            // calculate the relative volume due to the given depth curve
            variables.volume = distance_to_volume(definition, distance, sound_obstructed_proc(source));

            if (dx != 0 || dy != 0)
            {
                // set volume, left_volume, right_volume
                AngleAndVolumeToStereoVolume((short) (world.arctangent(dx, dy) - listener.yaw), variables.volume, out variables.right_volume, out variables.left_volume);
            }
            else
            {
                variables.left_volume = variables.right_volume = variables.volume;
            }

            return variables;
        }

        // The pitch a sound plays at (a multiple of its own), from the pitch it's asked to play at
        public static float CalculatePitchModifier(SoundDefinition definition, int pitch_modifier)
        {
            if (definition != null && (definition.flags & _sound_cannot_change_pitch) == 0)
            {
                if ((definition.flags & _sound_resists_pitch_changes) == 0)
                {
                    pitch_modifier += ((cstypes.FIXED_ONE - pitch_modifier) >> 1);
                }
            }
            else
            {
                pitch_modifier = cstypes.FIXED_ONE;
            }

            return pitch_modifier * 1f / _normal_frequency;
        }

        // ForgePlus: always stereo (any channel type but mono)
        public static void AngleAndVolumeToStereoVolume(short delta, short volume, out short right_volume, out short left_volume)
        {
            short fraction = (short) (delta & ((1 << (world.ANGULAR_BITS - 2)) - 1));
            short maximum_volume = (short) (volume + (volume >> 1));
            short minimum_volume = (short) (volume >> 2);
            short middle_volume = (short) (volume - minimum_volume);

            switch (world.NORMALIZE_ANGLE(delta) >> (world.ANGULAR_BITS - 2))
            {
                case 0: // rear right quarter [v,vmax] [v,vmin]
                    left_volume = (short) (middle_volume + ((fraction * (maximum_volume - middle_volume)) >> (world.ANGULAR_BITS - 2)));
                    right_volume = (short) (middle_volume + ((fraction * (minimum_volume - middle_volume)) >> (world.ANGULAR_BITS - 2)));
                    break;

                case 1: // front right quarter [vmax,vmid] [vmin,vmid]
                    left_volume = (short) (maximum_volume + ((fraction * (volume - maximum_volume)) >> (world.ANGULAR_BITS - 2)));
                    right_volume = (short) (minimum_volume + ((fraction * (volume - minimum_volume)) >> (world.ANGULAR_BITS - 2)));
                    break;

                case 2: // front left quarter [vmid,vmin] [vmid,vmax]
                    left_volume = (short) (volume + ((fraction * (minimum_volume - volume)) >> (world.ANGULAR_BITS - 2)));
                    right_volume = (short) (volume + ((fraction * (maximum_volume - volume)) >> (world.ANGULAR_BITS - 2)));
                    break;

                case 3: // rear left quarter [vmin,v] [vmax,v]
                    left_volume = (short) (minimum_volume + ((fraction * (middle_volume - minimum_volume)) >> (world.ANGULAR_BITS - 2)));
                    right_volume = (short) (maximum_volume + ((fraction * (middle_volume - maximum_volume)) >> (world.ANGULAR_BITS - 2)));
                    break;

                default:
                    csalerts.assert(false);
                    left_volume = right_volume = volume;
                    break;
            }
        }

        // Each permutation plays once before any plays again (_more_sounds_flag, which Aleph One's preferences have on)
        // ForgePlus: with the definition, and local_random, passed in
        public static short GetRandomSoundPermutation(SoundDefinition definition, Func<ushort> local_random)
        {
            if (definition == null) return 0;

            short permutation;

            if (!(definition.permutations > 0)) return 0;

            if ((definition.permutations_played & ((1 << definition.permutations) - 1)) == ((1 << definition.permutations) - 1))
                definition.permutations_played = 0;
            permutation = (short) (local_random() % definition.permutations);
            while ((definition.permutations_played & (1 << permutation)) != 0)
                if ((permutation += 1) >= definition.permutations)
                    permutation = 0;
            definition.permutations_played |= (ushort) (1 << permutation);

            return permutation;
        }
    }
}
