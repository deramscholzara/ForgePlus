// Port of Aleph One: Source_Files/GameWorld/player.h, player.cpp (only the player shapes and their MML, the constants
// they use, and SIZEOF_physics_constants)
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
