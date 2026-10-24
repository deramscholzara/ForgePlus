// Port of Aleph One: Source_Files/GameWorld/physics.cpp (the physics constants, and physics_update, which ForgePlus's Play mode
// moves its player with)
//
// Not ported: the rest of player movement (instantiate_physics_variables, which ForgePlus does in its own terms, and the
// virtual aim of the mouse), and dead players.
using System;
using System.Collections.Generic;
using static AlephOne.csalerts;
using static AlephOne.csmacros;
using static AlephOne.cstypes;
using static AlephOne.Packing;
using static AlephOne.physics_models;
using static AlephOne.player;
using static AlephOne.world;

namespace AlephOne
{
    public static class physics
    {
        public static void unpack_physics_constants(StreamPointer S, IList<physics_constants> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                physics_constants ObjPtr = Objects[k];

                StreamToValue(S, out ObjPtr.maximum_forward_velocity);
                StreamToValue(S, out ObjPtr.maximum_backward_velocity);
                StreamToValue(S, out ObjPtr.maximum_perpendicular_velocity);
                StreamToValue(S, out ObjPtr.acceleration);
                StreamToValue(S, out ObjPtr.deceleration);
                StreamToValue(S, out ObjPtr.airborne_deceleration);
                StreamToValue(S, out ObjPtr.gravitational_acceleration);
                StreamToValue(S, out ObjPtr.climbing_acceleration);
                StreamToValue(S, out ObjPtr.terminal_velocity);
                StreamToValue(S, out ObjPtr.external_deceleration);

                StreamToValue(S, out ObjPtr.angular_acceleration);
                StreamToValue(S, out ObjPtr.angular_deceleration);
                StreamToValue(S, out ObjPtr.maximum_angular_velocity);
                StreamToValue(S, out ObjPtr.angular_recentering_velocity);
                StreamToValue(S, out ObjPtr.fast_angular_velocity);
                StreamToValue(S, out ObjPtr.fast_angular_maximum);
                StreamToValue(S, out ObjPtr.maximum_elevation);
                StreamToValue(S, out ObjPtr.external_angular_deceleration);

                StreamToValue(S, out ObjPtr.step_delta);
                StreamToValue(S, out ObjPtr.step_amplitude);
                StreamToValue(S, out ObjPtr.radius);
                StreamToValue(S, out ObjPtr.height);
                StreamToValue(S, out ObjPtr.dead_height);
                StreamToValue(S, out ObjPtr.camera_height);
                StreamToValue(S, out ObjPtr.splash_height);

                StreamToValue(S, out ObjPtr.half_camera_separation);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_physics_constants));
        }

        public static void unpack_m1_physics_constants(StreamPointer S, IList<physics_constants> physics_models, int Count)
        {
            const int SIZEOF_old_physics_entry = 100;
            S.Skip(SIZEOF_old_physics_entry); // first is "editor" record

            // Count is a size_t
            for (uint k = 0; k < unchecked((uint) (Count - 1)); k++)
            {
                physics_constants ObjPtr = physics_models[(int) k];

                StreamToValue(S, out ObjPtr.maximum_forward_velocity);
                StreamToValue(S, out ObjPtr.maximum_backward_velocity);
                StreamToValue(S, out ObjPtr.maximum_perpendicular_velocity);
                StreamToValue(S, out ObjPtr.acceleration);
                StreamToValue(S, out ObjPtr.deceleration);
                StreamToValue(S, out ObjPtr.airborne_deceleration);
                StreamToValue(S, out ObjPtr.gravitational_acceleration);
                StreamToValue(S, out ObjPtr.climbing_acceleration);
                StreamToValue(S, out ObjPtr.terminal_velocity);
                StreamToValue(S, out ObjPtr.external_deceleration);

                StreamToValue(S, out ObjPtr.angular_acceleration);
                StreamToValue(S, out ObjPtr.angular_deceleration);
                StreamToValue(S, out ObjPtr.maximum_angular_velocity);
                StreamToValue(S, out ObjPtr.angular_recentering_velocity);
                StreamToValue(S, out ObjPtr.fast_angular_velocity);
                StreamToValue(S, out ObjPtr.fast_angular_maximum);
                StreamToValue(S, out ObjPtr.maximum_elevation);
                StreamToValue(S, out ObjPtr.external_angular_deceleration);

                StreamToValue(S, out ObjPtr.step_delta);
                StreamToValue(S, out ObjPtr.step_amplitude);
                StreamToValue(S, out ObjPtr.radius);
                StreamToValue(S, out ObjPtr.height);
                StreamToValue(S, out ObjPtr.dead_height);
                StreamToValue(S, out ObjPtr.camera_height);
                ObjPtr.splash_height = 0;

                StreamToValue(S, out ObjPtr.half_camera_separation);
            }
        }

        public static void pack_physics_constants(StreamPointer S, IList<physics_constants> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                physics_constants ObjPtr = Objects[k];

                ValueToStream(S, ObjPtr.maximum_forward_velocity);
                ValueToStream(S, ObjPtr.maximum_backward_velocity);
                ValueToStream(S, ObjPtr.maximum_perpendicular_velocity);
                ValueToStream(S, ObjPtr.acceleration);
                ValueToStream(S, ObjPtr.deceleration);
                ValueToStream(S, ObjPtr.airborne_deceleration);
                ValueToStream(S, ObjPtr.gravitational_acceleration);
                ValueToStream(S, ObjPtr.climbing_acceleration);
                ValueToStream(S, ObjPtr.terminal_velocity);
                ValueToStream(S, ObjPtr.external_deceleration);

                ValueToStream(S, ObjPtr.angular_acceleration);
                ValueToStream(S, ObjPtr.angular_deceleration);
                ValueToStream(S, ObjPtr.maximum_angular_velocity);
                ValueToStream(S, ObjPtr.angular_recentering_velocity);
                ValueToStream(S, ObjPtr.fast_angular_velocity);
                ValueToStream(S, ObjPtr.fast_angular_maximum);
                ValueToStream(S, ObjPtr.maximum_elevation);
                ValueToStream(S, ObjPtr.external_angular_deceleration);

                ValueToStream(S, ObjPtr.step_delta);
                ValueToStream(S, ObjPtr.step_amplitude);
                ValueToStream(S, ObjPtr.radius);
                ValueToStream(S, ObjPtr.height);
                ValueToStream(S, ObjPtr.dead_height);
                ValueToStream(S, ObjPtr.camera_height);
                ValueToStream(S, ObjPtr.splash_height);

                ValueToStream(S, ObjPtr.half_camera_separation);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_physics_constants));
        }

        public static void init_physics_constants(physics_constants[] physics_models)
        {
            // memcpy(physics_models, original_physics_models, sizeof(physics_models));
            for (int k = 0; k < NUMBER_OF_PHYSICS_MODELS; k++)
            {
                physics_models[k] = original_physics_models[k].Clone();
            }
        }

        // LP addition: get number of physics models (restricted sense)
        public static int get_number_of_physics_models() { return NUMBER_OF_PHYSICS_MODELS; }

        /* ---------- player movement (ForgePlus: for Play mode) */

        private const int COEFFICIENT_OF_ABSORBTION = 2;
        private const int CLOSE_ENOUGH_TO_FLOOR = (WORLD_ONE / 16) << (FIXED_FRACTIONAL_BITS - WORLD_FRACTIONAL_BITS); // WORLD_TO_FIXED(WORLD_ONE/16)

        private const int AIRBORNE_HEIGHT = (WORLD_ONE / 16) << (FIXED_FRACTIONAL_BITS - WORLD_FRACTIONAL_BITS); // WORLD_TO_FIXED(WORLD_ONE/16)

        private const uint FLAGS_WHICH_PREVENT_RECENTERING = (_turning | _looking | _sidestepping | _looking_vertically | _look_dont_turn | _sidestep_dont_turn);

        /*static*/
        public static physics_constants get_physics_constants_for_model(PhysicsModel model, uint action_flags)
        {
            // ForgePlus: the game's (earth gravity) model, the only one Aleph One has
            return model.physics_models[(action_flags & _run_dont_walk) != 0 ? _model_game_running : _model_game_walking];
        }

        /* separate physics_constant structures are passed in for running/walking modes */
        // ForgePlus: for a living player only (Aleph One's player is only read for whether it's dead), without the
        // virtual aim of the local player's mouse (ForgePlus turns the player itself, between ticks), and with the level
        // (for its low gravity) and whether the marine can grenade climb (its monster definition's flag) passed in.
        public static void physics_update(MapLevel level, physics_constants constants, physics_variables variables,
            uint action_flags, bool can_grenade_climb)
        {
            fixed_point3d new_position;
            short sine, cosine;
            int delta_z;
            int delta; /* used as a scratch 'change' variable */

            // Not ported: dead players immediately loose all bodily control
            delta_z = variables.position.z - variables.floor_height;

            /* process modifier keys (sidestepping and looking) into normal actions */
            if ((action_flags & _turning) != 0 && (action_flags & _sidestep_dont_turn) != 0 && (action_flags & _absolute_yaw_mode) == 0)
            {
                if ((action_flags & _turning_left) != 0) action_flags |= _sidestepping_left;
                if ((action_flags & _turning_right) != 0) action_flags |= _sidestepping_right;
                action_flags &= ~_turning;
            }
            if ((action_flags & _moving) != 0 && (action_flags & _look_dont_turn) != 0 && (action_flags & _absolute_position_mode) == 0)
            {
                if ((action_flags & _moving_forward) != 0) action_flags |= _looking_up;
                if ((action_flags & _moving_backward) != 0) action_flags |= _looking_down;
                action_flags &= ~_moving;
                action_flags &= ~_absolute_pitch_mode;
            }

            /* handle turning left or right; if we've exceeded our maximum velocity lock out user actions
                until we return to a legal range */
            // Not ported: absolute yaw mode
            {
                if (variables.angular_velocity < -constants.maximum_angular_velocity || variables.angular_velocity > constants.maximum_angular_velocity) action_flags &= ~_turning;
                switch (action_flags & _turning)
                {
                    case _turning_left:
                        delta = variables.angular_velocity > 0 ? constants.angular_acceleration + constants.angular_deceleration : constants.angular_acceleration;
                        variables.angular_velocity = FLOOR(variables.angular_velocity - delta, -constants.maximum_angular_velocity);
                        break;
                    case _turning_right:
                        delta = variables.angular_velocity < 0 ? constants.angular_acceleration + constants.angular_deceleration : constants.angular_acceleration;
                        variables.angular_velocity = CEILING(variables.angular_velocity + delta, constants.maximum_angular_velocity);
                        break;

                    default: /* slow down */
                        variables.angular_velocity = (variables.angular_velocity >= 0) ?
                            FLOOR(variables.angular_velocity - constants.angular_deceleration, 0) :
                            CEILING(variables.angular_velocity + constants.angular_deceleration, 0);
                        break;
                }

                /* handling looking left/right */
                switch (action_flags & _looking)
                {
                    case _looking_left:
                        variables.head_direction = FLOOR(variables.head_direction - constants.fast_angular_velocity, -constants.fast_angular_maximum);
                        break;
                    case _looking_right:
                        variables.head_direction = CEILING(variables.head_direction + constants.fast_angular_velocity, constants.fast_angular_maximum);
                        break;
                    case _looking: /* do nothing if both keys are down */
                        break;

                    default: /* recenter head */
                        variables.head_direction = (variables.head_direction >= 0) ?
                            FLOOR(variables.head_direction - constants.fast_angular_velocity, 0) :
                            CEILING(variables.head_direction + constants.fast_angular_velocity, 0);
                        break;
                }
            }

            // Not ported: absolute pitch mode
            {
                /* if the user touched the recenter key, set the recenter flag and override all up/down
                    keypresses with our own */
                if ((action_flags & _looking_center) != 0) variables.flags |= _RECENTERING_BIT;
                if ((variables.flags & _RECENTERING_BIT) != 0)
                {
                    action_flags &= ~_looking_vertically;
                    action_flags |= variables.elevation < 0 ? _looking_up : _looking_down;
                }

                /* handle looking up and down; if we're moving at our terminal velocity forward or backward,
                    without any side-to-side motion, recenter our head vertically */

                if ((action_flags & FLAGS_WHICH_PREVENT_RECENTERING) == 0) /* can't recenter if any of these are true */
                {
                    if (((action_flags & _moving_forward) != 0 && (variables.velocity == constants.maximum_forward_velocity)) ||
                        ((action_flags & _moving_backward) != 0 && (variables.velocity == -constants.maximum_backward_velocity)))
                    {
                        if (variables.elevation < 0)
                        {
                            variables.elevation = CEILING(variables.elevation + constants.angular_recentering_velocity, 0);
                        }
                        else
                        {
                            variables.elevation = FLOOR(variables.elevation - constants.angular_recentering_velocity, 0);
                        }
                    }
                }

                switch (action_flags & _looking_vertically)
                {
                    case _looking_down:
                        delta = variables.vertical_angular_velocity > 0 ? constants.angular_acceleration + constants.angular_deceleration : constants.angular_acceleration;
                        variables.vertical_angular_velocity = FLOOR(variables.vertical_angular_velocity - delta, -constants.maximum_angular_velocity);
                        break;
                    case _looking_up:
                        delta = variables.vertical_angular_velocity < 0 ? constants.angular_acceleration + constants.angular_deceleration : constants.angular_acceleration;
                        variables.vertical_angular_velocity = CEILING(variables.vertical_angular_velocity + delta, constants.maximum_angular_velocity);
                        break;

                    default: /* if no key is being held down, decelerate; if the player is moving try and return to phi==0 */
                        variables.vertical_angular_velocity = (variables.vertical_angular_velocity >= 0) ?
                            FLOOR(variables.vertical_angular_velocity - constants.angular_deceleration, 0) :
                            CEILING(variables.vertical_angular_velocity + constants.angular_deceleration, 0);
                        break;
                }
            }

            /* if we're on the ground (or rising up from it), allow movement; if we're flying through
                the air, don't let the player adjust his velocity in any way */
            if (delta_z <= 0 || (variables.flags & _HEAD_BELOW_MEDIA_BIT) != 0)
            {
                // Not ported: absolute position mode
                {
                    /* handle moving forward or backward; if we've exceeded our maximum velocity lock out user actions
                        until we return to a legal range */
                    if (variables.velocity < -constants.maximum_backward_velocity || variables.velocity > constants.maximum_forward_velocity) action_flags &= ~_moving;
                    switch (action_flags & _moving)
                    {
                        case _moving_forward:
                            delta = variables.velocity < 0 ? constants.deceleration + constants.acceleration : constants.acceleration;
                            variables.velocity = CEILING(variables.velocity + delta, constants.maximum_forward_velocity);
                            break;
                        case _moving_backward:
                            delta = variables.velocity > 0 ? constants.deceleration + constants.acceleration : constants.acceleration;
                            variables.velocity = FLOOR(variables.velocity - delta, -constants.maximum_backward_velocity);
                            break;

                        default: /* slow down */
                            variables.velocity = (variables.velocity >= 0) ?
                                FLOOR(variables.velocity - constants.deceleration, 0) :
                                CEILING(variables.velocity + constants.deceleration, 0);
                            break;
                    }
                }

                /* handle sidestepping left or right; if we've exceeded our maximum velocity lock out user actions
                    until we return to a legal range */
                if (variables.perpendicular_velocity < -constants.maximum_perpendicular_velocity || variables.perpendicular_velocity > constants.maximum_perpendicular_velocity) action_flags &= ~_sidestepping;
                switch (action_flags & _sidestepping)
                {
                    case _sidestepping_left:
                        delta = variables.perpendicular_velocity > 0 ? constants.acceleration + constants.deceleration : constants.acceleration;
                        variables.perpendicular_velocity = FLOOR(variables.perpendicular_velocity - delta, -constants.maximum_perpendicular_velocity);
                        break;
                    case _sidestepping_right:
                        delta = variables.perpendicular_velocity < 0 ? constants.acceleration + constants.deceleration : constants.acceleration;
                        variables.perpendicular_velocity = CEILING(variables.perpendicular_velocity + delta, constants.maximum_perpendicular_velocity);
                        break;

                    default: /* slow down */
                        variables.perpendicular_velocity = (variables.perpendicular_velocity >= 0) ?
                            FLOOR(variables.perpendicular_velocity - constants.deceleration, 0) :
                            CEILING(variables.perpendicular_velocity + constants.deceleration, 0);
                        break;
                }
            }

            /* change vertical_velocity based on difference between player height and surface height
                (if we are standing on an object, like a body, take that into account, too: this
                means a player could actually use bodies as ramps to reach ledges he couldn't
                otherwise jump to).  we should think about absorbing forward (or perpendicular)
                velocity to compensate for an increase in vertical velocity, which would slow down
                a player climbing stairs, etc. */
            if (delta_z < 0)
            {
                variables.external_velocity.k = CEILING(variables.external_velocity.k + constants.climbing_acceleration, constants.terminal_velocity);
            }
            if (delta_z > 0)
            {
                int gravity = constants.gravitational_acceleration;
                int terminal_velocity = constants.terminal_velocity;

                if ((level.static_world.environment_flags & map._environment_low_gravity) != 0) gravity >>= 1;
                if ((variables.flags & _FEET_BELOW_MEDIA_BIT) != 0) { gravity >>= 1; terminal_velocity >>= 1; }

                variables.external_velocity.k = FLOOR(variables.external_velocity.k - gravity, -terminal_velocity);
            }

            if ((action_flags & _swim) != 0 && (variables.flags & _HEAD_BELOW_MEDIA_BIT) != 0 && variables.external_velocity.k < 10 * constants.climbing_acceleration)
            {
                variables.external_velocity.k += constants.climbing_acceleration;
            }

            // Apply vertical angular velocity
            variables.elevation += variables.vertical_angular_velocity;

            // Clamp high-precision physical pitch to physics model limits
            variables.elevation = PIN(variables.elevation, -constants.maximum_elevation, constants.maximum_elevation);

            // If we're explicitly recentering and have reached or passed 0 pitch, stop at 0
            if ((variables.flags & _RECENTERING_BIT) != 0)
            {
                if ((variables.elevation <= 0 && (action_flags & _looking_down) != 0) || (variables.elevation >= 0 && (action_flags & _looking_up) != 0))
                {
                    variables.elevation = variables.vertical_angular_velocity = 0;
                    variables.flags &= unchecked((ushort) ~_RECENTERING_BIT);
                }
            }

            /* change the player's heading based on his angular velocities */
            variables.last_direction = variables.direction;
            variables.direction += variables.angular_velocity;
            if (variables.direction < 0) variables.direction += INTEGER_TO_FIXED(FULL_CIRCLE);
            if (variables.direction >= INTEGER_TO_FIXED(FULL_CIRCLE)) variables.direction -= INTEGER_TO_FIXED(FULL_CIRCLE);

            /* change the player's x,y position based on his direction and velocities (parallel and perpendicular)  */
            new_position = variables.position;
            cosine = cosine_table[FIXED_INTEGERAL_PART(variables.direction)]; sine = sine_table[FIXED_INTEGERAL_PART(variables.direction)];
            new_position.x += (variables.velocity * cosine - variables.perpendicular_velocity * sine) >> TRIG_SHIFT;
            new_position.y += (variables.velocity * sine + variables.perpendicular_velocity * cosine) >> TRIG_SHIFT;

            /* set above/below floor flags, remember old flags */
            variables.old_flags = variables.flags;
            if (new_position.z < variables.floor_height) variables.flags |= _BELOW_GROUND_BIT; else variables.flags &= unchecked((ushort) ~_BELOW_GROUND_BIT);
            if (new_position.z > variables.floor_height) variables.flags |= _ABOVE_GROUND_BIT; else variables.flags &= unchecked((ushort) ~_ABOVE_GROUND_BIT);

            /* if we just landed on the ground, or we just came up through the ground, absorb some of
                the player's external_velocity.k (and in the case of hitting the ground, reflect it) */
            if (variables.external_velocity.k > 0 && (variables.old_flags & _BELOW_GROUND_BIT) != 0 && (variables.flags & _BELOW_GROUND_BIT) == 0)
            {
                variables.external_velocity.k /= 2 * COEFFICIENT_OF_ABSORBTION; /* slow down */
            }
            if (variables.external_velocity.k > 0 && new_position.z + variables.actual_height >= variables.ceiling_height)
            {
                variables.external_velocity.k /= -COEFFICIENT_OF_ABSORBTION; new_position.z = variables.ceiling_height - variables.actual_height;
            }
            if (variables.external_velocity.k < 0 && (variables.old_flags & _BELOW_GROUND_BIT) == 0 && (variables.flags & _ABOVE_GROUND_BIT) == 0)
            {
                variables.external_velocity.k /= -COEFFICIENT_OF_ABSORBTION;
            }

            int small_enough_velocity;
            if (can_grenade_climb)
            {
                int gravity = constants.gravitational_acceleration;
                if ((level.static_world.environment_flags & map._environment_low_gravity) != 0) gravity >>= 1;
                if ((variables.flags & _FEET_BELOW_MEDIA_BIT) != 0) gravity >>= 1;

                small_enough_velocity = gravity;
            }
            else
            {
                small_enough_velocity = constants.climbing_acceleration; // SMALL_ENOUGH_VELOCITY
            }
            if (Math.Abs(variables.external_velocity.k) < small_enough_velocity &&
                Math.Abs(variables.floor_height - new_position.z) < CLOSE_ENOUGH_TO_FLOOR)
            {
                variables.external_velocity.k = 0; new_position.z = variables.floor_height;
                variables.flags &= unchecked((ushort) ~(_BELOW_GROUND_BIT | _ABOVE_GROUND_BIT));
            }

            /* change the player's z position based on his vertical velocity (if we hit the ground coming down
                then bounce and absorb most of the blow */
            new_position.x += variables.external_velocity.i;
            new_position.y += variables.external_velocity.j;
            new_position.z += variables.external_velocity.k;

            {
                short dx = unchecked((short) variables.external_velocity.i), dy = unchecked((short) variables.external_velocity.j);
                int external_delta = (delta_z <= 0) ? constants.external_deceleration : (constants.external_deceleration >> 2);
                int magnitude = isqrt(unchecked((uint) (dx * dx + dy * dy)));

                if (magnitude != 0 && magnitude > Math.Abs(external_delta))
                {
                    variables.external_velocity.i -= (dx * external_delta) / magnitude;
                    variables.external_velocity.j -= (dy * external_delta) / magnitude;
                }
                else
                {
                    variables.external_velocity.i = variables.external_velocity.j = 0;
                }
            }

            /* lower the player's externally-induced angular velocity */
            variables.external_angular_velocity = (variables.external_angular_velocity >= 0) ?
                FLOOR(variables.external_angular_velocity - constants.external_angular_deceleration, 0) :
                CEILING(variables.external_angular_velocity + constants.external_angular_deceleration, 0);

            /* instantiate new position, save old position */
            variables.last_position = variables.position;
            variables.position = new_position;

            /* if the player is moving, adjust step_phase by step_delta (if the player isn't moving
                continue to adjust step_phase until it is zero)  if the player is in the air, don't
                update phase until he lands. */
            variables.flags &= unchecked((ushort) ~_STEP_PERIOD_BIT);
            if (constants.maximum_forward_velocity != 0)
                variables.step_amplitude = (MAX(Math.Abs(variables.velocity), Math.Abs(variables.perpendicular_velocity)) * FIXED_ONE) / constants.maximum_forward_velocity;
            else // CB: "Missed Island" physics would produce a division by 0
                variables.step_amplitude = MAX(Math.Abs(variables.velocity), Math.Abs(variables.perpendicular_velocity)) * FIXED_ONE;
            if (delta_z >= 0)
            {
                if (variables.velocity != 0 || variables.perpendicular_velocity != 0)
                {
                    if ((variables.step_phase += constants.step_delta) >= FIXED_ONE)
                    {
                        variables.step_phase -= FIXED_ONE;
                        variables.flags |= _STEP_PERIOD_BIT;
                    }
                }
                else
                {
                    if (variables.step_phase != 0)
                    {
                        if (variables.step_phase > FIXED_ONE_HALF)
                        {
                            if ((variables.step_phase += constants.step_delta) >= FIXED_ONE) variables.step_phase = 0;
                        }
                        else
                        {
                            if ((variables.step_phase -= constants.step_delta) < 0) variables.step_phase = 0;
                        }
                    }
                }
            }

            if (delta_z >= AIRBORNE_HEIGHT)
            {
                variables.action = _player_airborne;
            }
            else
            {
                if (variables.angular_velocity != 0 || variables.velocity != 0 || variables.perpendicular_velocity != 0)
                {
                    variables.action = (action_flags & _run_dont_walk) != 0 ? _player_running : _player_walking;
                }
                else
                {
                    variables.action = (variables.external_velocity.i != 0 || variables.external_velocity.j != 0 || variables.external_velocity.k != 0) ? _player_sliding : _player_stationary;
                }
            }
        }
    }
}
