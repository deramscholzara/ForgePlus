using AlephOne;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using static AlephOne.cstypes;
using static AlephOne.map;
using static AlephOne.media;
using static AlephOne.platforms;
using static AlephOne.SoundManagerEnums;
using static AlephOne.world;

namespace ForgePlus.Sound
{
    // Port of Aleph One's map.cpp sound functions (_sound_obstructed_proc, _sound_add_ambient_sources_proc,
    // handle_random_sound_image, play_polygon_sound) and platforms.cpp's adjust_platform_for_media, for a listener in the
    // open level. Lights, media and platforms are as ForgePlus simulates them, and runtime state is kept here, not saved.
    public class LevelSoundWorld
    {
        private readonly LevelEntity_Level levelEntity;
        private readonly Func<ushort> localRandom;

        // Ticks until each random sound image plays, from the level's phase until it's first heard
        private readonly Dictionary<short, short> randomSoundImagePhases = new Dictionary<short, short>();

        // Whether each platform's floor and ceiling were last below its polygon's media
        private readonly Dictionary<short, (bool Floor, bool Ceiling)> platformsBelowMedia = new Dictionary<short, (bool Floor, bool Ceiling)>();

        private bool arePlatformsPlaced;

        public LevelSoundWorld(LevelEntity_Level levelEntity, Func<ushort> localRandom)
        {
            this.levelEntity = levelEntity;
            this.localRandom = localRandom;
        }

        public world_location3d Listener { get; set; }

        private MapLevel Level
        {
            get
            {
                return levelEntity.Level;
            }
        }

        // stuff floating on top of media is above it
        public ushort sound_obstructed_proc(world_location3d source)
        {
            return sound_obstructed_proc(source, distinguish_obstruction_types: false);
        }

        public ushort sound_obstructed_proc(world_location3d source, bool distinguish_obstruction_types)
        {
            world_location3d listener = Listener;
            ushort flags = 0;
            const int under_media_source_threshold = WORLD_ONE_FOURTH; //we don't obstruct sources not that deep in media

            if (line_is_obstructed(Level, source.polygon_index, source.point.xy(),
                listener.polygon_index, listener.point.xy(), true))
            {
                flags |= (ushort) _sound_was_obstructed;
            }

            bool check_media_obstruction = distinguish_obstruction_types || (flags & _sound_was_obstructed) == 0;

            if (check_media_obstruction)
            {
                polygon_data source_polygon = get_polygon_data(Level, source.polygon_index);
                polygon_data listener_polygon = get_polygon_data(Level, listener.polygon_index);
                bool source_under_media = false, listener_under_media = false;

                // LP change: idiot-proofed the media handling
                if (source_polygon.media_index != NONE)
                {
                    if (TryGetMediaHeight(source_polygon.media_index, out var media_height))
                    {
                        if (source.point.z + under_media_source_threshold < media_height)
                        {
                            source_under_media = true;
                        }
                    }
                }

                if (listener_polygon.media_index != NONE)
                {
                    if (TryGetMediaHeight(listener_polygon.media_index, out var media_height))
                    {
                        if (listener.point.z < media_height)
                        {
                            listener_under_media = true;
                        }
                    }
                }

                if (source_under_media)
                {
                    if (!listener_under_media || source_polygon.media_index != listener_polygon.media_index)
                    {
                        flags |= (ushort) _sound_was_media_obstructed;
                    }
                    else
                    {
                        flags |= (ushort) _sound_was_media_muffled;
                    }
                }
                else
                {
                    if (listener_under_media)
                    {
                        flags |= (ushort) _sound_was_media_obstructed;
                    }
                }
            }

            return flags;
        }

        // for the listener (ForgePlus: with the sounds file to add them with, and the listener in a polygon)
        public void sound_add_ambient_sources_proc(ambient_sound_data[] data, SoundFile sound_file)
        {
            world_location3d listener = Listener;

            polygon_data listener_polygon = get_polygon_data(Level, listener.polygon_index);
            if (listener_polygon == null)
            {
                return;
            }

            short media_height = 0;
            bool has_media = listener_polygon.media_index != NONE && TryGetMediaHeight(listener_polygon.media_index, out media_height);
            int indexes = get_map_indexes(Level, listener_polygon.sound_source_indexes, 0);
            world_location3d source;
            bool under_media = false;
            short index;

            // add ambient sound image
            if (has_media && listener.point.z < media_height)
            {
                // if we're under media don't play the ambient sound image
                AddOneAmbientSoundSource(sound_file, data, null,
                    get_media_sound(Level, listener_polygon.media_index, _media_snd_ambient_under), MAXIMUM_SOUND_VOLUME);
                under_media = true;
            }
            else
            {
                // if we have an ambient sound image, play it
                if (listener_polygon.ambient_sound_image_index != NONE)
                {
                    ambient_sound_image_data image = get_ambient_sound_image_data(Level, listener_polygon.ambient_sound_image_index);

                    // LP change: returning NULL means this is invalid (ForgePlus: which is left as it is)
                    if (image != null)
                        AddOneAmbientSoundSource(sound_file, data, null, image.sound_index, image.volume);
                }

                // if we're over media, play that ambient sound image
                if (has_media && (media_height >= FloorHeight(listener.polygon_index) || !MEDIA_SOUND_OBSTRUCTED_BY_FLOOR(get_media_data(Level, listener_polygon.media_index))))
                {
                    source = listener;
                    source.point.z = media_height;
                    AddOneAmbientSoundSource(sound_file, data, source,
                        get_media_sound(Level, listener_polygon.media_index, _media_snd_ambient_over), MAXIMUM_SOUND_VOLUME);
                }
            }

            // add ambient sound image from platform
            if (listener_polygon.type == _polygon_is_platform)
            {
                if (PlatformIsMoving(listener_polygon.permutation))
                {
                    source = listener;
                    source.point.z = FloorHeight(listener.polygon_index);
                    AddOneAmbientSoundSource(sound_file, data, source,
                        get_platform_moving_sound(Level, listener_polygon.permutation), MAXIMUM_SOUND_VOLUME);
                }
            }

            // add ambient sound sources
            // do only if indexes were found
            if (indexes != NONE)
            {
                while (indexes < Level.MapIndexList.Count && (index = Level.MapIndexList[indexes++]) != NONE && index < Level.SavedObjectList.Count)
                {
                    map_object map_object = Level.SavedObjectList[index]; // gross, sorry
                    polygon_data polygon = get_polygon_data(Level, map_object.polygon_index);
                    if (polygon == null)
                    {
                        // ForgePlus: one out of the level is skipped (rather than crashing)
                        continue;
                    }

                    short object_media_height = 0;
                    bool object_has_media = polygon.media_index != NONE && TryGetMediaHeight(polygon.media_index, out object_media_height);
                    short sound_type = map_object.index;
                    short sound_volume = map_object.facing;
                    bool active = true;

                    if (sound_volume < 0)
                    {
                        sound_volume = (short) (LightIntensity((short) -sound_volume) >> 8);
                    }

                    // yaw, pitch are irrelevant
                    source = default;
                    source.point = map_object.location;
                    source.polygon_index = map_object.polygon_index;
                    if ((map_object.flags & _map_object_hanging_from_ceiling) != 0)
                    {
                        source.point.z += CeilingHeight(map_object.polygon_index);
                    }
                    else
                    {
                        if ((map_object.flags & _map_object_floats) != 0 && object_has_media)
                        {
                            source.point.z += object_media_height;
                        }
                        else
                        {
                            source.point.z += FloorHeight(map_object.polygon_index);
                        }
                    }

                    // adjust source if necessary (like, for a platform)
                    if ((map_object.flags & _map_object_is_platform_sound) != 0)
                    {
                        // ForgePlus: PLATFORM_IS_MOVING alone in the game, but a platform only moves while it's active
                        if (polygon.type == _polygon_is_platform && PlatformIsMoving(polygon.permutation))
                        {
                            sound_type = get_platform_moving_sound(Level, polygon.permutation);
                            source.point.z = listener.point.z; // always on our level
                        }
                        else
                        {
                            active = false;
                        }
                    }

                    // .index is environmental sound type, .facing is volume
                    // CB: added check for media != NULL because it sometimes crashed here when being underwater
                    if (active && (!under_media || (object_has_media && source.point.z < object_media_height && polygon.media_index == listener_polygon.media_index)))
                    {
                        AddOneAmbientSoundSource(sound_file, data, source, sound_type, sound_volume);
                    }
                }
            }
        }

        // Once a tick, for the listener's polygon. ForgePlus: played with direct_play_sound (sound, direction or NONE,
        // volume, pitch)
        public void handle_random_sound_image(Action<short, short, short, int> direct_play_sound)
        {
            polygon_data polygon = get_polygon_data(Level, Listener.polygon_index);

            if (polygon != null && polygon.random_sound_image_index != NONE)
            {
                random_sound_image_data image = get_random_sound_image_data(Level, polygon.random_sound_image_index);

                // LP change: returning NULL means this is invalid (ForgePlus: which is left as it is)
                if (image != null)
                {
                    if (!randomSoundImagePhases.TryGetValue(polygon.random_sound_image_index, out var phase))
                    {
                        phase = image.phase;
                    }

                    // play a random sound
                    if (phase == 0)
                    {
                        short volume = image.volume;
                        short direction = image.direction;
                        int pitch = image.pitch;

                        if (image.delta_volume != 0) volume += (short) (localRandom() % image.delta_volume);
                        if (image.delta_direction != 0) direction = NORMALIZE_ANGLE(direction + localRandom() % image.delta_direction);
                        if (image.delta_pitch != 0) pitch += localRandom() % image.delta_pitch;

                        direct_play_sound(SoundManager.RandomSoundIndexToSoundIndex(image.sound_index), (image.flags & _sound_image_is_non_directional) != 0 ? NONE : direction, volume, pitch);
                    }

                    // lower phase and reset if necessary
                    if ((phase -= 1) < 0)
                    {
                        phase = image.period;
                        if (image.delta_period != 0) phase += (short) (localRandom() % image.delta_period);
                    }

                    randomSoundImagePhases[polygon.random_sound_image_index] = phase;
                }
            }
        }

        // Each random sound image starts again from the level's phase
        public void ResetRandomSoundImages()
        {
            randomSoundImagePhases.Clear();
        }

        // Each frame: plays the media sounds of the platforms moving now from their polygons. The first time, as the
        // level starts, each platform's place is only noted.
        public void update_platforms_for_media(Action<short, world_location3d> play_sound)
        {
            for (short platform_index = 0; platform_index < Level.PlatformList.Count; platform_index++)
            {
                if (!arePlatformsPlaced)
                {
                    adjust_platform_for_media(platform_index, true);
                    continue;
                }

                if (!PlatformIsMoving(platform_index))
                {
                    continue;
                }

                short sound_index = adjust_platform_for_media(platform_index, false);
                if (sound_index != NONE)
                {
                    play_sound(sound_index, polygon_sound_source(Level.PlatformList[platform_index].polygon_index));
                }
            }

            arePlatformsPlaced = true;
        }

        // play_polygon_sound: where a polygon's sounds come from (its center, on its floor as it is now)
        public world_location3d polygon_sound_source(short polygon_index)
        {
            find_center_of_polygon(Level, polygon_index, out var center);

            return new world_location3d
            {
                point = new world_point3d(center.x, center.y, FloorHeight(polygon_index)),
                polygon_index = polygon_index,
            };
        }

        // Returns the media sound (or NONE) as the platform's floor or ceiling enters or leaves its polygon's media.
        // ForgePlus: whether they're below the media is kept here rather than in its dynamic flags.
        private short adjust_platform_for_media(short platform_index, bool initialize)
        {
            platform_data platform = get_platform_data(Level, platform_index);
            polygon_data polygon = platform != null ? get_polygon_data(Level, platform.polygon_index) : null;
            short sound_index = NONE;

            if (polygon != null && polygon.media_index != NONE)
            {
                // LP change: idiot-proofing
                if (TryGetMediaHeight(polygon.media_index, out var media_height))
                {
                    bool floor_below_media = FloorHeight(platform.polygon_index) < media_height;
                    bool ceiling_below_media = CeilingHeight(platform.polygon_index) < media_height;

                    if (!initialize && platformsBelowMedia.TryGetValue(platform_index, out var below))
                    {
                        short sound_code = NONE;

                        if ((below.Floor && !floor_below_media) ||
                            (below.Ceiling && !ceiling_below_media))
                        {
                            sound_code = _media_snd_platform_leaving;
                        }
                        if ((!below.Floor && floor_below_media) ||
                            (!below.Ceiling && ceiling_below_media))
                        {
                            sound_code = _media_snd_platform_entering;
                        }

                        if (sound_code != NONE)
                        {
                            sound_index = get_media_sound(Level, polygon.media_index, sound_code);
                        }
                    }

                    platformsBelowMedia[platform_index] = (floor_below_media, ceiling_below_media);
                }
            }

            return sound_index;
        }

        private void AddOneAmbientSoundSource(SoundFile sound_file, ambient_sound_data[] data, world_location3d? source, short ambient_sound_index, short absolute_volume)
        {
            SoundManager.AddOneAmbientSoundSource(sound_file, data, source, Listener, ambient_sound_index, absolute_volume, sound_obstructed_proc);
        }

        // A platform's current height, or as it's saved
        private short FloorHeight(short polygonIndex)
        {
            var polygon = get_polygon_data(Level, polygonIndex);

            if (polygon.type == _polygon_is_platform &&
                levelEntity.FloorPlatforms.TryGetValue(polygon.permutation, out var platform) &&
                platform)
            {
                return ToWorldDistance(platform.CurrentHeightInWorldUnitIncrements);
            }

            return polygon.floor_height;
        }

        private short CeilingHeight(short polygonIndex)
        {
            var polygon = get_polygon_data(Level, polygonIndex);

            if (polygon.type == _polygon_is_platform &&
                levelEntity.CeilingPlatforms.TryGetValue(polygon.permutation, out var platform) &&
                platform)
            {
                return ToWorldDistance(platform.CurrentHeightInWorldUnitIncrements);
            }

            return polygon.ceiling_height;
        }

        private bool PlatformIsMoving(short platformIndex)
        {
            return (levelEntity.FloorPlatforms.TryGetValue(platformIndex, out var floorPlatform) && floorPlatform && floorPlatform.IsRuntimeMoving) ||
                (levelEntity.CeilingPlatforms.TryGetValue(platformIndex, out var ceilingPlatform) && ceilingPlatform && ceilingPlatform.IsRuntimeMoving);
        }

        private bool TryGetMediaHeight(short mediaIndex, out short height)
        {
            if (get_media_data(Level, mediaIndex) != null && levelEntity.Medias.TryGetValue(mediaIndex, out var media))
            {
                height = ToWorldDistance(media.CurrentHeight * GeometryUtilities.WorldUnitIncrementsPerMeter);
                return true;
            }

            height = 0;
            return false;
        }

        // _fixed, from 0 (off) to FIXED_ONE (fully on), as the light is now (or 0 for no light)
        private int LightIntensity(short lightIndex)
        {
            return levelEntity.Lights.TryGetValue(lightIndex, out var light) ?
                Mathf.RoundToInt(Mathf.Clamp01(light.CurrentLinearIntensity) * FIXED_ONE) :
                0;
        }

        private static short ToWorldDistance(float worldUnitIncrements)
        {
            return (short) Mathf.Clamp(Mathf.RoundToInt(worldUnitIncrements), short.MinValue, short.MaxValue);
        }
    }
}
