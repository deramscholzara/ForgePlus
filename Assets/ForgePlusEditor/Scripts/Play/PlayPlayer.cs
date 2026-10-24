using AlephOne;
using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using static AlephOne.cstypes;
using static AlephOne.map;
using static AlephOne.physics;
using static AlephOne.player;
using static AlephOne.world;

namespace ForgePlus.Play
{
    // Play mode's player: moved a tick at a time by Aleph One's physics (physics_update), then kept out of walls and
    // moved across the map as instantiate_physics_variables (physics.cpp) does, in the play world's map (whose platforms
    // move). Turned and tilted by the mouse directly between ticks, as Aleph One's mouse aim is. Not ported: being
    // pushed out of objects (legal_player_move), and dying.
    public class PlayPlayer
    {
        private readonly PlayWorld world;
        private readonly PhysicsModel model;
        private readonly bool canGrenadeClimb;
        private readonly physics_variables variables = new physics_variables();

        // The player's feet (its legs object's location), and the camera, as of the last two ticks
        private world_point3d location;
        private world_point3d cameraLocation;
        private world_point3d lastCameraLocation;

        private short supportingPolygonIndex = NONE;
        private short lastSupportingPolygonIndex = NONE;

        public PlayPlayer(PlayWorld world, PhysicsModel model, map_object start)
        {
            this.world = world;
            this.model = model;
            canGrenadeClimb = (model.monster_definitions[monsters._monster_marine].flags & monster_definitions._monster_can_grenade_climb) != 0;

            // Standing where it starts, facing its way
            var floorHeight = world.Map.PolygonList[start.polygon_index].floor_height;
            Place(start.polygon_index, new world_point3d(start.location.x, start.location.y, (short) (floorHeight + start.location.z)), start.facing);
        }

        // The polygon the player's feet are in (its legs object's)
        public short PolygonIndex { get; private set; }

        // FIXED_TO_WORLD(actual_height)
        public short Height
        {
            get
            {
                return FIXED_TO_WORLD(variables.actual_height);
            }
        }

        public world_point3d Location
        {
            get
            {
                return location;
            }
        }

        // Where its reach is measured from: the middle of the player (line_is_within_range in devices.cpp)
        public world_point3d ReachOrigin
        {
            get
            {
                return new world_point3d(location.x, location.y, (short) (location.z + (Height >> 1)));
            }
        }

        // Where it faces (its head's direction with its body's), in angle units
        public short Facing
        {
            get
            {
                return NORMALIZE_ANGLE(FIXED_INTEGERAL_PART(variables.direction + variables.head_direction));
            }
        }

        // Its view in Unity's space (meters, and degrees as a camera's euler angles), between the last two ticks
        public Vector3 GetCameraPosition(float tickFraction)
        {
            var from = ToUnity(lastCameraLocation);
            var to = ToUnity(cameraLocation);

            return Vector3.Lerp(from, to, tickFraction);
        }

        public Quaternion CameraRotation
        {
            get
            {
                var yaw = (variables.direction + variables.head_direction) / (float) FIXED_ONE * 360f / FULL_CIRCLE;
                var pitch = variables.elevation / (float) FIXED_ONE * 360f / FULL_CIRCLE;

                // Aleph One's angles turn counterclockwise from +x across the map, which is Unity's +x turned toward -z
                return Quaternion.Euler(-pitch, 90f + yaw, 0f);
            }
        }

        // The mouse turns and tilts the player directly (within its physics model's limit on looking up and down)
        public void Turn(float yawDegrees, float pitchDegrees)
        {
            var constants = get_physics_constants_for_model(model, 0);
            var fullCircle = INTEGER_TO_FIXED(FULL_CIRCLE);

            variables.direction = (int) ((variables.direction + yawDegrees * fullCircle / 360f) % fullCircle);
            if (variables.direction < 0)
            {
                variables.direction += fullCircle;
            }

            variables.elevation = Mathf.Clamp((int) (variables.elevation + pitchDegrees * fullCircle / 360f),
                -constants.maximum_elevation, constants.maximum_elevation);
        }

        // Standing still on the floor of the polygon it's in, since the last tick (as update_player_teleport, in
        // player.cpp, requires of a player to teleport it): neither moved nor turned
        public bool IsStandingStill
        {
            get
            {
                return variables.position.x == variables.last_position.x &&
                       variables.position.y == variables.last_position.y &&
                       variables.position.z == variables.last_position.z &&
                       variables.last_direction == variables.direction &&
                       location.z == world.Map.PolygonList[PolygonIndex].floor_height;
            }
        }

        // A teleport within the level (update_player_teleport in player.cpp): the player stands in the middle of the
        // polygon, on its floor, facing the same way, and starts over from there
        public void TeleportTo(short polygonIndex)
        {
            var polygon = world.Map.PolygonList[polygonIndex];
            Place(polygonIndex, new world_point3d(polygon.center.x, polygon.center.y, polygon.floor_height), Facing);
        }

        // initialize_player_physics_variables (physics.cpp): standing still at the location, facing the given way, with
        // its head level
        private void Place(short polygonIndex, world_point3d newLocation, short facing)
        {
            PolygonIndex = polygonIndex;
            location = newLocation;
            supportingPolygonIndex = NONE;
            lastSupportingPolygonIndex = NONE;

            var constants = get_physics_constants_for_model(model, 0);
            variables.head_direction = 0;
            variables.adjusted_yaw = variables.direction = INTEGER_TO_FIXED(facing);
            variables.adjusted_pitch = variables.elevation = 0;
            variables.angular_velocity = variables.vertical_angular_velocity = 0;
            variables.velocity = variables.perpendicular_velocity = 0;
            variables.position.x = WORLD_TO_FIXED(location.x);
            variables.position.y = WORLD_TO_FIXED(location.y);
            variables.position.z = WORLD_TO_FIXED(location.z);
            variables.last_position = variables.position;
            variables.last_direction = variables.direction;
            variables.external_angular_velocity = 0;
            variables.external_velocity = new fixed_vector3d();
            variables.actual_height = constants.height;
            variables.step_phase = 0;
            variables.step_amplitude = 0;
            variables.action = _player_stationary;
            variables.old_flags = variables.flags = 0;

            Instantiate(constants, firstTime: true);
            lastCameraLocation = cameraLocation;
        }

        // update_player_physics_variables (physics.cpp): one tick
        public void Tick(uint actionFlags)
        {
            var constants = get_physics_constants_for_model(model, actionFlags);

            lastCameraLocation = cameraLocation;

            physics_update(world.Map, constants, variables, actionFlags, canGrenadeClimb);
            Instantiate(constants, firstTime: false);
        }

        // adjust_player_for_polygon_height_change (physics.cpp): a floor that moves under the player carries it along
        public void OnPolygonHeightChanged(short polygonIndex, short oldFloorHeight, short newFloorHeight)
        {
            if (supportingPolygonIndex == polygonIndex && FIXED_TO_WORLD(variables.position.z) <= oldFloorHeight)
            {
                variables.floor_height = variables.position.z = WORLD_TO_FIXED(newFloorHeight);

                // film_profile.fix_sliding_on_platforms
                if (variables.external_velocity.k < 0)
                {
                    variables.external_velocity.k = 0;
                }
            }
        }

        // instantiate_physics_variables (physics.cpp), without objects to bump into, or a player that can die
        private void Instantiate(physics_constants constants, bool firstTime)
        {
            var map = world.Map;
            var oldPolygonIndex = PolygonIndex;

            /* convert to world coordinates before doing collision detection */
            var newLocation = new world_point3d(FIXED_TO_WORLD(variables.position.x), FIXED_TO_WORLD(variables.position.y), FIXED_TO_WORLD(variables.position.z));

            if (!firstTime && lastSupportingPolygonIndex != supportingPolygonIndex)
            {
                world.OnPlayerEnteredPolygon(supportingPolygonIndex);
            }

            lastSupportingPolygonIndex = firstTime ? NONE : supportingPolygonIndex;

            /* check for 2d collisions with walls and knock the player back out of the wall */
            var clipped = keep_line_segment_out_of_walls(map, PolygonIndex, location, ref newLocation,
                WORLD_ONE / 3, FIXED_TO_WORLD(variables.actual_height), out var adjustedFloorHeight, out var adjustedCeilingHeight,
                out supportingPolygonIndex);

            /* translate_map_object will handle crossing polygon boundaries */
            if (TranslateMapObject(ref newLocation))
            {
                if (oldPolygonIndex == PolygonIndex) clipped = true; /* oops; trans_map_obj destructively changed our position */
            }

            /* if our move got clipped, copy the new coordinate back into the physics variables */
            if (clipped)
            {
                variables.position.x = WORLD_TO_FIXED(newLocation.x);
                variables.position.y = WORLD_TO_FIXED(newLocation.y);
                variables.position.z = WORLD_TO_FIXED(newLocation.z);
            }

            /* shadow position in player structure, build camera location */
            int stepHeight = (constants.step_amplitude * sine_table[variables.step_phase >> (FIXED_FRACTIONAL_BITS - ANGULAR_BITS + 1)]) >> TRIG_SHIFT;
            stepHeight = (stepHeight * variables.step_amplitude) >> FIXED_FRACTIONAL_BITS;

            cameraLocation = newLocation;
            location = newLocation;
            cameraLocation.z = (short) (cameraLocation.z + FIXED_TO_WORLD(stepHeight + variables.actual_height - constants.camera_height));

            /* initialize floor_height and ceiling_height for next call to physics_update() */
            variables.floor_height = WORLD_TO_FIXED(adjustedFloorHeight);
            variables.ceiling_height = WORLD_TO_FIXED(adjustedCeilingHeight);

            var mediaHeight = world.MediaHeight(PolygonIndex);
            if (location.z < mediaHeight) variables.flags |= _FEET_BELOW_MEDIA_BIT; else variables.flags &= unchecked((ushort) ~_FEET_BELOW_MEDIA_BIT);
            if (cameraLocation.z < mediaHeight) variables.flags |= _HEAD_BELOW_MEDIA_BIT; else variables.flags &= unchecked((ushort) ~_HEAD_BELOW_MEDIA_BIT);
        }

        // translate_map_object (map.cpp), for the player's legs: which polygon the new location is in, found by crossing
        // lines from the old one. Out of the map, it's put back in the middle of the polygon it was in. Returns whether it
        // changed polygons (or was put back).
        private bool TranslateMapObject(ref world_point3d newLocation)
        {
            var map = world.Map;
            var oldPolygonIndex = PolygonIndex;
            var newPolygonIndex = oldPolygonIndex;
            var changedPolygons = false;
            short lineIndex;

            do
            {
                lineIndex = find_line_crossed_leaving_polygon(map, newPolygonIndex, location.xy(), newLocation.xy());
                if (lineIndex != NONE) newPolygonIndex = find_adjacent_polygon(map, newPolygonIndex, lineIndex);
                if (newPolygonIndex == NONE)
                {
                    var center = map.PolygonList[oldPolygonIndex].center;
                    newLocation.x = center.x;
                    newLocation.y = center.y;

                    newPolygonIndex = oldPolygonIndex;
                    changedPolygons = true; /* tell the caller we switched polygons, even though we didn't */
                    break;
                }
            }
            while (lineIndex != NONE);

            if (oldPolygonIndex != newPolygonIndex)
            {
                changedPolygons = true;
            }

            PolygonIndex = newPolygonIndex;
            location = newLocation;

            return changedPolygons;
        }

        private static Vector3 ToUnity(world_point3d point)
        {
            return new Vector3(point.x, point.z, -point.y) / GeometryUtilities.WorldUnitIncrementsPerMeter;
        }
    }
}
