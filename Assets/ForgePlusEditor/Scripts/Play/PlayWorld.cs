using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Sound;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using UnityEngine;
using static AlephOne.cstypes;
using static AlephOne.devices;
using static AlephOne.map;
using static AlephOne.platforms;

namespace ForgePlus.Play
{
    // The level as Play mode runs it. Its map data (Map) is a copy of the edited level's, whose platforms' polygons,
    // lines and points move with them and whose control panels change, as Aleph One's own map does while a game runs, so
    // the edited level never changes. Its lights, media and platforms run as the editor's simulation of them does
    // (LevelEntity_Light, _Media and _Platform), from where each starts in the game, and their heights and states are
    // read from there. A control panel's texture is shown on the level's own side, and what only the editor shows
    // (player starts, goals and sound sources) is hidden, until Play mode ends.
    //
    // Not simulated: anything but the player in a platform's way.
    public class PlayWorld
    {
        // control_panel_settings' default (devices.cpp): how far a switch's light must be on to use it
        private const float LightedSwitchIntensity = 0.75f;
        private const float Marathon1LightedSwitchIntensity = 0.5f;

        private readonly Dictionary<short, ushort> originalPanelTextures = new Dictionary<short, ushort>();

        // The objects only the editor shows (player starts, goals and sound sources), hidden until Play mode ends
        private readonly List<GameObject> hiddenObjects = new List<GameObject>();

        public PlayWorld(LevelEntity_Level level)
        {
            Level = level;

            var edited = level.Level;
            Map = new MapLevel
            {
                static_world = edited.static_world,
                EndpointList = edited.EndpointList.ConvertAll(endpoint => endpoint.Clone()),
                LineList = edited.LineList.ConvertAll(line => line.Clone()),
                SideList = edited.SideList.ConvertAll(side => side.Clone()),
                PolygonList = edited.PolygonList.ConvertAll(polygon => polygon.Clone()),
                AmbientSoundImageList = edited.AmbientSoundImageList,
                RandomSoundImageList = edited.RandomSoundImageList,
                MapIndexList = edited.MapIndexList,
                MapAnnotationList = edited.MapAnnotationList,
                SavedObjectList = edited.SavedObjectList,
                LightList = edited.LightList,
                MediaList = edited.MediaList,
                PlatformList = edited.PlatformList.ConvertAll(platform => platform.Clone()),
                static_platforms = edited.static_platforms,
                object_placement_info = edited.object_placement_info,
                map_terminal_text = edited.map_terminal_text,
                loaded_wad = edited.loaded_wad,
            };
        }

        public LevelEntity_Level Level { get; }

        public MapLevel Map { get; }

        // A terminal's control panel was used (its permutation is the terminal)
        public event Action<short> OnTerminalUsed;

        // Everything starts as it does when the game enters the level: lights, media and platforms in their initial
        // states, and control panels showing theirs (initialize_control_panels_for_level in devices.cpp)
        public void Begin()
        {
            ResetSimulation();
            SyncPlatformHeights(null);

            foreach (var mapObject in Level.MapObjects.Values)
            {
                var type = mapObject.NativeObject.type;
                if ((type == _saved_player || type == _saved_goal || type == _saved_sound_source) && mapObject.gameObject.activeSelf)
                {
                    mapObject.gameObject.SetActive(false);
                    hiddenObjects.Add(mapObject.gameObject);
                }
            }

            for (short sideIndex = 0; sideIndex < Map.SideList.Count; sideIndex++)
            {
                var side = Map.SideList[sideIndex];
                if (!SIDE_IS_CONTROL_PANEL(side))
                {
                    continue;
                }

                var definition = get_control_panel_definition(side.control_panel_type);
                if (definition == null)
                {
                    SET_SIDE_CONTROL_PANEL(side, false);
                    continue;
                }

                var status = false;
                switch (definition._class)
                {
                    case _panel_is_tag_switch:
                        status = GET_CONTROL_PANEL_STATUS(side);
                        break;
                    case _panel_is_light_switch:
                        status = IsLightOn(side.control_panel_permutation);
                        break;
                    case _panel_is_platform_switch:
                        if (side.control_panel_permutation >= 0)
                        {
                            status = IsPlatformSwitchOn(side.control_panel_permutation);
                        }
                        else
                        {
                            SET_SIDE_CONTROL_PANEL(side, false);
                            continue;
                        }

                        break;
                }

                SET_CONTROL_PANEL_STATUS(side, status);
                ApplyPanelTexture(sideIndex);
            }
        }

        // The level's own sides get their textures back, and its lights, media and platforms start over, as the level
        // does when it's opened
        // A level that's gone (closed, or another opened, or Unity's Play mode ending) has nothing left to put back
        public void End()
        {
            LevelEntity_Platform.IsHeightChangeLegal = null;

            if (!Level)
            {
                originalPanelTextures.Clear();
                hiddenObjects.Clear();
                return;
            }

            foreach (var original in originalPanelTextures)
            {
                if (Level.Sides.TryGetValue(original.Key, out var side) && side)
                {
                    side.NativeObject.primary_texture.texture = original.Value;
                    if (side.PrimarySurface)
                    {
                        side.PrimarySurface.ApplyTexture();
                    }
                }
            }

            originalPanelTextures.Clear();

            foreach (var hiddenObject in hiddenObjects)
            {
                if (hiddenObject)
                {
                    hiddenObject.SetActive(true);
                }
            }

            hiddenObjects.Clear();

            ResetSimulation();
        }

        // Whether a platform's half can move to a height (in meters), with the player where it is: not if its polygon
        // would be too short for the player in it (legal_polygon_height_change in map.cpp)
        public void SetPlayer(PlayPlayer player)
        {
            LevelEntity_Platform.IsHeightChangeLegal = player == null ? null : (platform, height) =>
            {
                var platformData = Map.PlatformList[platform.NativeIndex];
                if (player.PolygonIndex != platformData.polygon_index)
                {
                    return true;
                }

                // From where its halves are now (which the map only catches up with each tick)
                var polygon = Map.PolygonList[platformData.polygon_index];
                var floor = Level.FloorPlatforms.TryGetValue(platform.NativeIndex, out var floorHalf) ? ToWorldDistance(floorHalf.CurrentHeightInWorldUnitIncrements) : polygon.floor_height;
                var ceiling = Level.CeilingPlatforms.TryGetValue(platform.NativeIndex, out var ceilingHalf) ? ToWorldDistance(ceilingHalf.CurrentHeightInWorldUnitIncrements) : polygon.ceiling_height;
                var gap = ceiling - floor;

                var newHeight = ToWorldDistance(height * GeometryUtilities.WorldUnitIncrementsPerMeter);
                if (platform.LinkedSurface == LevelEntity_Platform.LinkedSurfaces.Floor)
                {
                    floor = newHeight;
                }
                else
                {
                    ceiling = newHeight;
                }

                // Only a change that closes it in on the player (further) is stopped
                return ceiling - floor >= player.Height || ceiling - floor >= gap;
            };
        }

        // Each platform's polygon (and the lines and points around it) takes the heights its halves have moved to
        // (update_platforms in platforms.cpp), carrying the player along on a floor it stands on
        public void SyncPlatformHeights(PlayPlayer player)
        {
            for (short platformIndex = 0; platformIndex < Map.PlatformList.Count; platformIndex++)
            {
                var platform = Map.PlatformList[platformIndex];
                if (platform.polygon_index < 0 || platform.polygon_index >= Map.PolygonList.Count)
                {
                    continue;
                }

                var polygon = Map.PolygonList[platform.polygon_index];
                var floor = Level.FloorPlatforms.TryGetValue(platformIndex, out var floorHalf) ? ToWorldDistance(floorHalf.CurrentHeightInWorldUnitIncrements) : polygon.floor_height;
                var ceiling = Level.CeilingPlatforms.TryGetValue(platformIndex, out var ceilingHalf) ? ToWorldDistance(ceilingHalf.CurrentHeightInWorldUnitIncrements) : polygon.ceiling_height;

                if (floor == polygon.floor_height && ceiling == polygon.ceiling_height)
                {
                    continue;
                }

                player?.OnPolygonHeightChanged(platform.polygon_index, polygon.floor_height, floor);

                polygon.floor_height = floor;
                polygon.ceiling_height = ceiling;
                adjust_platform_endpoint_and_line_heights(Map, platformIndex);
            }
        }

        // The height of a polygon's media as it is now (INT16_MIN for none)
        public short MediaHeight(short polygonIndex)
        {
            var mediaIndex = Map.PolygonList[polygonIndex].media_index;
            if (mediaIndex == NONE || !Level.Medias.TryGetValue(mediaIndex, out var media))
            {
                return INT16_MIN;
            }

            return ToWorldDistance(media.CurrentHeight * GeometryUtilities.WorldUnitIncrementsPerMeter);
        }

        // changed_polygon (marathon.cpp), for a player: what the polygon it now stands on triggers
        public void OnPlayerEnteredPolygon(short polygonIndex)
        {
            if (polygonIndex == NONE)
            {
                return;
            }

            var polygon = Map.PolygonList[polygonIndex];
            switch (polygon.type)
            {
                case _polygon_is_light_on_trigger:
                case _polygon_is_light_off_trigger:
                    SetLightStatus(polygon.permutation, polygon.type == _polygon_is_light_on_trigger);
                    break;

                case _polygon_is_platform:
                    // platform_was_entered (platforms.cpp)
                    var flags = Map.PlatformList[polygon.permutation].static_flags;
                    if (!PLATFORM_IS_DOOR(flags) && PLATFORM_IS_PLAYER_CONTROLLABLE(flags))
                    {
                        TryChangePlatformState(polygon.permutation, true);
                    }

                    break;

                case _polygon_is_platform_on_trigger:
                case _polygon_is_platform_off_trigger:
                    if (polygon.permutation >= 0 && polygon.permutation < Map.PolygonList.Count)
                    {
                        TryChangePlatformState(Map.PolygonList[polygon.permutation].permutation, polygon.type == _polygon_is_platform_on_trigger);
                    }

                    break;
            }
        }

        // switch_can_be_toggled (devices.cpp), by a player: a lighted switch only while its light is bright enough, and
        // (with the action key, as the game allows) not one only projectiles can hit, which Play mode lets the player use
        // from anywhere in place of shooting it
        public bool CanTogglePanel(short sideIndex, bool withActionKey)
        {
            var side = Map.SideList[sideIndex];
            var definition = get_control_panel_definition(side.control_panel_type);
            if (definition == null)
            {
                return false;
            }

            var canToggle = true;
            if ((side.flags & _side_is_lighted_switch) != 0)
            {
                canToggle = LightIntensity(side.primary_lightsource_index) > LightedSwitchIntensity;
            }
            else if ((side.flags & _side_is_m1_lighted_switch) != 0)
            {
                canToggle = LightIntensity(side.primary_lightsource_index) > Marathon1LightedSwitchIntensity;
            }

            if (withActionKey && (side.flags & _side_switch_can_only_be_hit_by_projectiles) != 0)
            {
                canToggle = false;
            }

            return canToggle;
        }

        // A control panel the player tried to use, but can't (play_control_panel_sound in devices.cpp)
        public void PlayUnusableSound(short sideIndex)
        {
            PlayPanelSound(sideIndex, _unusuable_sound);
        }

        // change_panel_state (devices.cpp), for the player, who's taken to have any item a switch needs, and to need
        // nothing a recharger gives
        public void UsePanel(short sideIndex)
        {
            var side = Map.SideList[sideIndex];
            var definition = get_control_panel_definition(side.control_panel_type);
            if (definition == null)
            {
                return;
            }

            // A switch that's destroyed as it's used is no longer one
            if ((side.flags & _side_switch_can_be_destroyed) != 0)
            {
                SET_SIDE_CONTROL_PANEL(side, false);
            }

            var state = GET_CONTROL_PANEL_STATUS(side);

            switch (definition._class)
            {
                case _panel_is_computer_terminal:
                    OnTerminalUsed?.Invoke(side.control_panel_permutation);
                    break;

                case _panel_is_tag_switch:
                    // A chip can only be inserted, not taken back out
                    if (definition.item == NONE || !state)
                    {
                        state = !state;

                        var makesSound = SetTaggedLightStatuses(side.control_panel_permutation, state);
                        makesSound |= TryChangeTaggedPlatformStates(side.control_panel_permutation, state);
                        makesSound |= side.control_panel_permutation == 0;

                        if (makesSound)
                        {
                            SET_CONTROL_PANEL_STATUS(side, state);
                            ApplyPanelTexture(sideIndex);
                            PlayPanelSound(sideIndex, state ? _activating_sound : _deactivating_sound);
                        }
                    }

                    break;

                case _panel_is_light_switch:
                    SetLightStatus(side.control_panel_permutation, !state);
                    break;

                case _panel_is_platform_switch:
                    if (side.control_panel_permutation >= 0 && side.control_panel_permutation < Map.PolygonList.Count)
                    {
                        TryChangePlatformState(Map.PolygonList[side.control_panel_permutation].permutation, !state);
                    }

                    break;

                case _panel_is_pattern_buffer:
                    PlayPanelSound(sideIndex, _activating_sound);
                    break;
            }
        }

        // A terminal's tag group (computer_interface.cpp): its tagged lights and platforms turn on
        public void ActivateTag(short tag)
        {
            SetTaggedLightStatuses(tag, true);
            TryChangeTaggedPlatformStates(tag, true);
        }

        // assume_correct_switch_position (devices.cpp): light and platform switches show their light's or platform's
        // state, as it changes (however it was changed), with their sounds
        public void SyncSwitches()
        {
            for (short sideIndex = 0; sideIndex < Map.SideList.Count; sideIndex++)
            {
                var side = Map.SideList[sideIndex];
                if (!SIDE_IS_CONTROL_PANEL(side))
                {
                    continue;
                }

                var definition = get_control_panel_definition(side.control_panel_type);
                if (definition == null)
                {
                    continue;
                }

                bool status;
                switch (definition._class)
                {
                    case _panel_is_light_switch:
                        status = IsLightOn(side.control_panel_permutation);
                        break;
                    case _panel_is_platform_switch:
                        status = IsPlatformSwitchOn(side.control_panel_permutation);
                        break;
                    default:
                        continue;
                }

                if (status != GET_CONTROL_PANEL_STATUS(side))
                {
                    SET_CONTROL_PANEL_STATUS(side, status);
                    ApplyPanelTexture(sideIndex);
                    PlayPanelSound(sideIndex, status ? _activating_sound : _deactivating_sound);
                }
            }
        }

        // try_and_change_platform_state (platforms.cpp): a platform can be deactivated unless it can't be externally,
        // and is changed as the game changes it (LevelEntity_Platform.SetRuntimeState, with what that does to its lights
        // and adjacent platforms). Returns whether it's now in that state.
        public bool TryChangePlatformState(short platformIndex, bool state)
        {
            var platform = LevelEntity_Platform.GetSelectablePlatform(Level, platformIndex);
            if (!platform)
            {
                return false;
            }

            if (!state && platform.IsRuntimeActive && PLATFORM_CANNOT_BE_EXTERNALLY_DEACTIVATED(platform.NativeObject.static_flags))
            {
                return false;
            }

            return platform.SetRuntimeState(state, NONE) == state;
        }

        public static short ToWorldDistance(float worldUnitIncrements)
        {
            return (short) Mathf.Clamp(Mathf.RoundToInt(worldUnitIncrements), short.MinValue, short.MaxValue);
        }

        private void ResetSimulation()
        {
            foreach (var light in Level.Lights.Values)
            {
                light.BeginRuntimeStyleBehavior();
            }

            foreach (var media in Level.Medias.Values)
            {
                media.BeginRuntimeStyleBehavior();
            }

            // Not those destroyed with their surfaces (as an edit rebuilds them)
            foreach (var platform in Level.FloorPlatforms.Values)
            {
                if (platform)
                {
                    platform.BeginRuntimeStyleBehavior();
                }
            }

            foreach (var platform in Level.CeilingPlatforms.Values)
            {
                if (platform)
                {
                    platform.BeginRuntimeStyleBehavior();
                }
            }
        }

        // set_light_status (lightsource.cpp)
        private bool SetLightStatus(short lightIndex, bool status)
        {
            return Level.Lights.TryGetValue(lightIndex, out var light) && light.SetRuntimeStatus(status);
        }

        // set_tagged_light_statuses (lightsource.cpp): tag 0 is no tag
        private bool SetTaggedLightStatuses(short tag, bool status)
        {
            var changed = false;
            if (tag == 0)
            {
                return changed;
            }

            foreach (var light in Level.Lights.Values)
            {
                if (light.NativeObject.tag == tag && light.SetRuntimeStatus(status))
                {
                    changed = true;
                }
            }

            return changed;
        }

        // try_and_change_tagged_platform_states (platforms.cpp): tag 0 is no tag
        private bool TryChangeTaggedPlatformStates(short tag, bool state)
        {
            var changed = false;
            if (tag == 0)
            {
                return changed;
            }

            for (short platformIndex = 0; platformIndex < Map.PlatformList.Count; platformIndex++)
            {
                if (Map.PlatformList[platformIndex].tag == tag && TryChangePlatformState(platformIndex, state))
                {
                    changed = true;
                }
            }

            return changed;
        }

        private bool IsLightOn(short lightIndex)
        {
            return Level.Lights.TryGetValue(lightIndex, out var light) && light.IsRuntimeActive;
        }

        // A platform switch's permutation is its platform's polygon (platform_is_on in platforms.cpp)
        private bool IsPlatformSwitchOn(short polygonIndex)
        {
            if (polygonIndex < 0 || polygonIndex >= Map.PolygonList.Count)
            {
                return false;
            }

            var platform = LevelEntity_Platform.GetSelectablePlatform(Level, Map.PolygonList[polygonIndex].permutation);

            return platform && platform.IsRuntimeActive;
        }

        private float LightIntensity(short lightIndex)
        {
            return Level.Lights.TryGetValue(lightIndex, out var light) ? light.CurrentLinearIntensity : 0f;
        }

        // set_control_panel_texture (devices.cpp), shown on the level's own side
        private void ApplyPanelTexture(short sideIndex)
        {
            var side = Map.SideList[sideIndex];
            var definition = get_control_panel_definition(side.control_panel_type);
            if (definition == null || !Level.Sides.TryGetValue(sideIndex, out var shownSide))
            {
                return;
            }

            var texture = AlephOneExtensions.BuildShapeDescriptor(definition.collection,
                GET_CONTROL_PANEL_STATUS(side) ? definition.active_shape : definition.inactive_shape);

            var shownTexture = shownSide.NativeObject.primary_texture;
            if (shownTexture.texture == texture)
            {
                return;
            }

            if (!originalPanelTextures.ContainsKey(sideIndex))
            {
                originalPanelTextures[sideIndex] = shownTexture.texture;
            }

            shownTexture.texture = texture;
            if (shownSide.PrimarySurface)
            {
                shownSide.PrimarySurface.ApplyTexture();
            }
        }

        // play_control_panel_sound (devices.cpp)
        private void PlayPanelSound(short sideIndex, short soundIndex)
        {
            var definition = get_control_panel_definition(Map.SideList[sideIndex].control_panel_type);
            var playback = LevelSoundPlayback.Instance;
            if (definition == null || !playback || soundIndex < 0 || soundIndex >= NUMBER_OF_CONTROL_PANEL_SOUNDS)
            {
                return;
            }

            playback.PlaySideSound(sideIndex, definition.sounds[soundIndex], definition.sound_frequency);
        }
    }
}
