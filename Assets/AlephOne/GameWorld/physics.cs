// Port of Aleph One: Source_Files/GameWorld/physics.cpp (the physics constants)
//
// Not ported: player movement.
using System.Collections.Generic;
using static AlephOne.csalerts;
using static AlephOne.Packing;
using static AlephOne.physics_models;
using static AlephOne.player;

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
    }
}
