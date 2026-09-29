// Port of Aleph One: Source_Files/GameWorld/physics_models.h
using Unity.Scripting.LifecycleManagement;
using static AlephOne.cstypes;
using static AlephOne.world;

namespace AlephOne
{
    /* ---------- structures */

    public class physics_constants
    {
        public int maximum_forward_velocity, maximum_backward_velocity, maximum_perpendicular_velocity;
        public int acceleration, deceleration, airborne_deceleration; /* forward, backward and perpendicular */
        public int gravitational_acceleration, climbing_acceleration, terminal_velocity;
        public int external_deceleration;

        public int angular_acceleration, angular_deceleration, maximum_angular_velocity, angular_recentering_velocity;
        public int fast_angular_velocity, fast_angular_maximum; /* for head movements */
        public int maximum_elevation; /* positive and negative */
        public int external_angular_deceleration;

        /* step_length is distance between adjacent nodes in the actor’s phase */
        public int step_delta, step_amplitude;
        public int radius, height, dead_height, camera_height, splash_height;

        public int half_camera_separation;

        public physics_constants Clone()
        {
            return (physics_constants) MemberwiseClone();
        }
    }

    [NoAutoStaticsCleanup]
    public static class physics_models
    {
        /* ---------- constants */

        /* models */
        public const short _model_game_walking = 0;
        public const short _model_game_running = 1;
        public const short NUMBER_OF_PHYSICS_MODELS = 2;

        /* ---------- globals */

        public static readonly physics_constants[] original_physics_models = new physics_constants[NUMBER_OF_PHYSICS_MODELS]
        {
            /* game walking */
            new physics_constants
            {
                maximum_forward_velocity = FIXED_ONE/14, maximum_backward_velocity = FIXED_ONE/17, maximum_perpendicular_velocity = FIXED_ONE/20, /* max forward, backward and perpendicular velocity */
                acceleration = FIXED_ONE/200, deceleration = FIXED_ONE/100, airborne_deceleration = FIXED_ONE/180, /* acceleration, deceleration, airborne deceleration */
                gravitational_acceleration = FIXED_ONE/400, climbing_acceleration = FIXED_ONE/300, terminal_velocity = FIXED_ONE/7, /* gravity, normal acceleration, terminal velocity */
                external_deceleration = FIXED_ONE/200, /* external deceleration */

                angular_acceleration = (5*FIXED_ONE)/8, angular_deceleration = (5*FIXED_ONE)/4, maximum_angular_velocity = 6*FIXED_ONE, angular_recentering_velocity = (3*FIXED_ONE)/4, /* angular acceleration, deceleration, max */
                fast_angular_velocity = QUARTER_CIRCLE*FIXED_ONE/6, fast_angular_maximum = QUARTER_CIRCLE*FIXED_ONE, /* fast angular v, max */
                maximum_elevation = QUARTER_CIRCLE*FIXED_ONE/3, /* maximum elevation */
                external_angular_deceleration = FIXED_ONE/3, /* external angular deceleration */

                step_delta = FIXED_ONE/20, step_amplitude = FIXED_ONE/10, /* step delta, step amplitude */
                radius = FIXED_ONE/4, height = (4*FIXED_ONE)/5, dead_height = FIXED_ONE/4, camera_height = (1*FIXED_ONE)/5, /* radius, height, dead height, viewpoint height */
                splash_height = FIXED_ONE/2, /* splash height */
                half_camera_separation = FIXED_ONE/32 /* camera separation */
            },

            /* game running */
            new physics_constants
            {
                maximum_forward_velocity = FIXED_ONE/8, maximum_backward_velocity = FIXED_ONE/12, maximum_perpendicular_velocity = FIXED_ONE/13, /* max forward, backward and perpendicular velocity */
                acceleration = FIXED_ONE/100, deceleration = FIXED_ONE/50, airborne_deceleration = FIXED_ONE/180, /* acceleration, deceleration, airborne deceleration */
                gravitational_acceleration = FIXED_ONE/400, climbing_acceleration = FIXED_ONE/200, terminal_velocity = FIXED_ONE/7, /* gravity, normal acceleration, terminal velocity */
                external_deceleration = FIXED_ONE/200, /* external deceleration */

                angular_acceleration = (5*FIXED_ONE)/4, angular_deceleration = (5*FIXED_ONE)/2, maximum_angular_velocity = 10*FIXED_ONE, angular_recentering_velocity = (3*FIXED_ONE)/2, /* angular acceleration, deceleration, max */
                fast_angular_velocity = QUARTER_CIRCLE*FIXED_ONE/6, fast_angular_maximum = QUARTER_CIRCLE*FIXED_ONE, /* fast angular v, max */
                maximum_elevation = QUARTER_CIRCLE*FIXED_ONE/3, /* maximum elevation */
                external_angular_deceleration = FIXED_ONE/3, /* external angular deceleration */

                step_delta = FIXED_ONE/20, step_amplitude = FIXED_ONE/10, /* step delta, step amplitude */
                radius = FIXED_ONE/4, height = (4*FIXED_ONE)/5, dead_height = FIXED_ONE/4, camera_height = (1*FIXED_ONE)/5, /* radius, height, dead height, viewpoint height */
                splash_height = FIXED_ONE/2, /* splash height */
                half_camera_separation = FIXED_ONE/32 /* camera separation */
            },
        };
    }
}
