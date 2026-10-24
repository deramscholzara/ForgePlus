// Port of Aleph One: Source_Files/GameWorld/player.h, player.cpp (only the player shapes and their MML, the constants
// they use, SIZEOF_physics_constants, and the action flags and physics_variables that physics_update moves a player with)
//
// Not ported: player_data and everything that runs players in a game, and MML parsing of player
// settings.
using Unity.Scripting.LifecycleManagement;
using static AlephOne.shape_descriptors;
using static AlephOne.weapons;

namespace AlephOne
{
    // ZZZ: moved here so we can get/use in files other than player.cpp
    public class player_shape_definitions
    {
        public short collection;

        public short dying_hard, dying_soft;
        public short dead_hard, dead_soft;
        public short[] legs = new short[player.NUMBER_OF_PLAYER_ACTIONS]; /* stationary, walking, running, sliding, airborne */
        public short[] torsos = new short[PLAYER_TORSO_SHAPE_COUNT]; /* NONE, ..., double pistols */
        public short[] charging_torsos = new short[PLAYER_TORSO_SHAPE_COUNT]; /* NONE, ..., double pistols */
        public short[] firing_torsos = new short[PLAYER_TORSO_SHAPE_COUNT]; /* NONE, ..., double pistols */

        public player_shape_definitions Clone()
        {
            var copy = (player_shape_definitions) MemberwiseClone();
            copy.legs = (short[]) legs.Clone();
            copy.torsos = (short[]) torsos.Clone();
            copy.charging_torsos = (short[]) charging_torsos.Clone();
            copy.firing_torsos = (short[]) firing_torsos.Clone();
            return copy;
        }
    }

    public class physics_variables
    {
        public int head_direction;
        public int last_direction, direction, elevation, angular_velocity, vertical_angular_velocity;
        public int velocity, perpendicular_velocity; /* in and perpendicular to direction, respectively */
        public fixed_point3d last_position, position;
        public int actual_height;

        /* used by mask_in_absolute_positioning_information (because it is not really absolute) to
            keep track of where we're going */
        public int adjusted_pitch, adjusted_yaw;

        public fixed_vector3d external_velocity; /* from impacts; slowly absorbed */
        public int external_angular_velocity; /* from impacts; slowly absorbed */

        public int step_phase; /* step_phase is in [0,1) and is some function of the distance travelled
            (for bobbing the gun and the viewpoint) */
        public int step_amplitude; /* step amplitude is in [0,1) and is some function of velocity */

        public int floor_height; /* the height of the floor on the polygon where we ended up last time */
        public int ceiling_height; /* same as above, but ceiling height */
        public int media_height; /* media height */

        public short action; /* what the player's legs are doing, basically */
        public ushort old_flags, flags; /* stuff like _RECENTERING */
    }

    [NoAutoStaticsCleanup]
    public static class player
    {
        /* ---------- constants (player.h) */

        /* player actions; irrelevant if the player is dying or something */
        public const short _player_stationary = 0;
        public const short _player_walking = 1;
        public const short _player_running = 2;
        public const short _player_sliding = 3;
        public const short _player_airborne = 4;
        public const short NUMBER_OF_PLAYER_ACTIONS = 5;

        /* team colors */
        public const short _violet_team = 0;
        public const short _red_team = 1;
        public const short _tan_team = 2;
        public const short _light_blue_team = 3;
        public const short _yellow_team = 4;
        public const short _brown_team = 5;
        public const short _blue_team = 6;
        public const short _green_team = 7;
        public const short NUMBER_OF_TEAM_COLORS = 8;

        public const int SIZEOF_physics_constants = 104;

        /* ---------- action flags */

        /* action flag bit offsets */
        public const int _absolute_yaw_mode_bit = 0;
        public const int _turning_left_bit = 1;
        public const int _turning_right_bit = 2;
        public const int _sidestep_dont_turn_bit = 3;
        public const int _looking_left_bit = 4;
        public const int _looking_right_bit = 5;
        public const int _absolute_yaw_bit0 = 6;
        public const int _absolute_yaw_bit1 = 7;

        public const int _absolute_pitch_mode_bit = 8;
        public const int _looking_up_bit = 9;
        public const int _looking_down_bit = 10;
        public const int _looking_center_bit = 11;
        public const int _absolute_pitch_bit0 = 12;
        public const int _absolute_pitch_bit1 = 13;

        public const int _absolute_position_mode_bit = 14;
        public const int _moving_forward_bit = 15;
        public const int _moving_backward_bit = 16;
        public const int _run_dont_walk_bit = 17;
        public const int _look_dont_turn_bit = 18;
        public const int _absolute_position_bit0 = 19;
        public const int _absolute_position_bit1 = 20;
        public const int _absolute_position_bit2 = 21;

        public const int _sidestepping_left_bit = 22;
        public const int _sidestepping_right_bit = 23;
        public const int _left_trigger_state_bit = 24;
        public const int _right_trigger_state_bit = 25;
        public const int _action_trigger_state_bit = 26;
        public const int _cycle_weapons_forward_bit = 27;
        public const int _cycle_weapons_backward_bit = 28;
        public const int _toggle_map_bit = 29;
        public const int _microphone_button_bit = 30;
        public const int _swim_bit = 31;

        /* action_flags */
        public const uint _absolute_yaw_mode = 1u << _absolute_yaw_mode_bit;
        public const uint _turning_left = 1u << _turning_left_bit;
        public const uint _turning_right = 1u << _turning_right_bit;
        public const uint _sidestep_dont_turn = 1u << _sidestep_dont_turn_bit;
        public const uint _looking_left = 1u << _looking_left_bit;
        public const uint _looking_right = 1u << _looking_right_bit;

        public const uint _absolute_pitch_mode = 1u << _absolute_pitch_mode_bit;
        public const uint _looking_up = 1u << _looking_up_bit;
        public const uint _looking_down = 1u << _looking_down_bit;
        public const uint _looking_center = 1u << _looking_center_bit;
        public const uint _look_dont_turn = 1u << _look_dont_turn_bit;

        public const uint _absolute_position_mode = 1u << _absolute_position_mode_bit;
        public const uint _moving_forward = 1u << _moving_forward_bit;
        public const uint _moving_backward = 1u << _moving_backward_bit;
        public const uint _run_dont_walk = 1u << _run_dont_walk_bit;

        public const uint _sidestepping_left = 1u << _sidestepping_left_bit;
        public const uint _sidestepping_right = 1u << _sidestepping_right_bit;
        public const uint _left_trigger_state = 1u << _left_trigger_state_bit;
        public const uint _right_trigger_state = 1u << _right_trigger_state_bit;
        public const uint _action_trigger_state = 1u << _action_trigger_state_bit;
        public const uint _cycle_weapons_forward = 1u << _cycle_weapons_forward_bit;
        public const uint _cycle_weapons_backward = 1u << _cycle_weapons_backward_bit;
        public const uint _toggle_map = 1u << _toggle_map_bit;
        public const uint _microphone_button = 1u << _microphone_button_bit;
        public const uint _swim = 1u << _swim_bit;

        public const uint _turning = _turning_left | _turning_right;
        public const uint _looking = _looking_left | _looking_right;
        public const uint _moving = _moving_forward | _moving_backward;
        public const uint _sidestepping = _sidestepping_left | _sidestepping_right;
        public const uint _looking_vertically = _looking_up | _looking_down | _looking_center;

        /* player flag bits */
        public const ushort _RECENTERING_BIT = 0x8000;
        public const ushort _ABOVE_GROUND_BIT = 0x4000;
        public const ushort _BELOW_GROUND_BIT = 0x2000;
        public const ushort _FEET_BELOW_MEDIA_BIT = 0x1000;
        public const ushort _HEAD_BELOW_MEDIA_BIT = 0x0800;
        public const ushort _STEP_PERIOD_BIT = 0x0400;

        /* ---------- globals (player.cpp) */

        private static player_shape_definitions player_shapes = new player_shape_definitions
        {
            collection = 6, /* collection */

            dying_hard = 9, dying_soft = 8, /* dying hard, dying soft */
            dead_hard = 11, dead_soft = 10, /* dead hard, dead soft */
            legs = new short[NUMBER_OF_PLAYER_ACTIONS] {7, 0, 0, 24, 23}, /* legs: stationary, walking, running, sliding, airborne */
            // LP additions: SMG-wielding/firing shapes (just before last two)
            torsos = new short[PLAYER_TORSO_SHAPE_COUNT] {1, 3, 20, 26, 14, 12, 31, 16, 28, 33, 5, 18}, /* idle torsos: fists, magnum, fusion, assault, rocket, flamethrower, alien, shotgun, double pistol, double shotgun, da ball */
            charging_torsos = new short[PLAYER_TORSO_SHAPE_COUNT] {1, 3, 21, 26, 14, 12, 31, 16, 28, 33, 5, 18}, /* charging torsos: fists, magnum, fusion, assault, rocket, flamethrower, alien, shotgun, double pistol, double shotgun, ball */
            firing_torsos = new short[PLAYER_TORSO_SHAPE_COUNT] {2, 4, 22, 27, 15, 13, 32, 17, 28, 34, 6, 19}, /* firing torsos: fists, magnum, fusion, assault, rocket, flamethrower, alien, shotgun, double pistol, double shotgun, ball */
        };

        public static player_shape_definitions get_player_shape_definitions()
        {
            return player_shapes;
        }

        // The shapes as they were before MML changed them (null while MML hasn't)
        private static player_shape_definitions original_player_shapes;

        // ForgePlus: whether MML has changed the player's shapes since they were last reset
        public static bool player_shapes_changed_by_mml()
        {
            return original_player_shapes != null;
        }

        // Only the player's shapes are ported (see the file's header)
        public static void reset_mml_player()
        {
            if (original_player_shapes != null)
            {
                player_shapes = original_player_shapes;
                original_player_shapes = null;
            }
        }

        public static void parse_mml_player(InfoTree root)
        {
            // back up old values first
            if (original_player_shapes == null)
            {
                original_player_shapes = player_shapes;
                player_shapes = player_shapes.Clone();
            }

            // Not ported: the player's settings, initial items, damage responses and powerups

            foreach (var shp in root.children_named("shape"))
            {
                short type = 0;
                if (!shp.read_indexed("type", ref type, 5))
                    continue;

                if (type == 0)
                {
                    short subtype = 0;
                    if (!shp.read_indexed("subtype", ref subtype, 5))
                        continue;
                    switch (subtype)
                    {
                        case 0:
                            shp.read_indexed("value", ref player_shapes.collection, MAXIMUM_COLLECTIONS);
                            break;
                        case 1:
                            shp.read_indexed("value", ref player_shapes.dying_hard, MAXIMUM_SHAPES_PER_COLLECTION);
                            break;
                        case 2:
                            shp.read_indexed("value", ref player_shapes.dying_soft, MAXIMUM_SHAPES_PER_COLLECTION);
                            break;
                        case 3:
                            shp.read_indexed("value", ref player_shapes.dead_hard, MAXIMUM_SHAPES_PER_COLLECTION);
                            break;
                        case 4:
                            shp.read_indexed("value", ref player_shapes.dead_soft, MAXIMUM_SHAPES_PER_COLLECTION);
                            break;
                    }
                }
                else if (type == 1)
                {
                    short subtype = 0;
                    if (!shp.read_indexed("subtype", ref subtype, NUMBER_OF_PLAYER_ACTIONS))
                        continue;
                    shp.read_indexed("value", ref player_shapes.legs[subtype], MAXIMUM_SHAPES_PER_COLLECTION);
                }
                else if (type == 2)
                {
                    short subtype = 0;
                    if (!shp.read_indexed("subtype", ref subtype, PLAYER_TORSO_SHAPE_COUNT))
                        continue;
                    shp.read_indexed("value", ref player_shapes.torsos[subtype], MAXIMUM_SHAPES_PER_COLLECTION);
                }
                else if (type == 3)
                {
                    short subtype = 0;
                    if (!shp.read_indexed("subtype", ref subtype, PLAYER_TORSO_SHAPE_COUNT))
                        continue;
                    shp.read_indexed("value", ref player_shapes.charging_torsos[subtype], MAXIMUM_SHAPES_PER_COLLECTION);
                }
                else if (type == 4)
                {
                    short subtype = 0;
                    if (!shp.read_indexed("subtype", ref subtype, PLAYER_TORSO_SHAPE_COUNT))
                        continue;
                    shp.read_indexed("value", ref player_shapes.firing_torsos[subtype], MAXIMUM_SHAPES_PER_COLLECTION);
                }
            }
        }
    }
}
