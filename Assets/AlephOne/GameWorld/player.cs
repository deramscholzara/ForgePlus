// Port of Aleph One: Source_Files/GameWorld/player.h, player.cpp (only the player shapes, the constants
// they use, and SIZEOF_physics_constants)
//
// Not ported: player_data and everything that runs players in a game, and MML parsing of player
// settings.
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
    }

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

        /* ---------- globals (player.cpp) */

        private static readonly player_shape_definitions player_shapes = new player_shape_definitions
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
    }
}
