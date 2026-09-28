// Port of Aleph One: Source_Files/GameWorld/world.h, world.cpp (the distance functions)
//
// Not ported: trig tables, random numbers, and the point transforms of the running game.
using static AlephOne.cstypes;
using static AlephOne.FilmProfileGlobals;

namespace AlephOne
{
    // typedef int16 angle; typedef _fixed fixed_angle; typedef int16 world_distance;

    public struct world_point2d
    {
        public short x, y;

        public world_point2d(short x, short y)
        {
            this.x = x;
            this.y = y;
        }
    }

    // struct world_point3d : public world_point2d
    public struct world_point3d
    {
        public short x, y;
        public short z;

        public world_point3d(short x, short y, short z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public world_point2d xy()
        {
            return new world_point2d(x, y);
        }
    }

    public struct world_vector2d
    {
        public short i, j;
    }

    // struct world_vector3d : public world_vector2d
    public struct world_vector3d
    {
        public short i, j;
        public short k;
    }

    public static class world
    {
        public const int TRIG_SHIFT = 10;
        public const int TRIG_MAGNITUDE = (1 << TRIG_SHIFT);

        public const int ANGULAR_BITS = 9;
        public const short NUMBER_OF_ANGLES = (short) (1 << ANGULAR_BITS);
        public const short FULL_CIRCLE = NUMBER_OF_ANGLES;
        public const short QUARTER_CIRCLE = (short) (NUMBER_OF_ANGLES / 4);
        public const short HALF_CIRCLE = (short) (NUMBER_OF_ANGLES / 2);
        public const short THREE_QUARTER_CIRCLE = (short) ((NUMBER_OF_ANGLES * 3) / 4);
        public const short EIGHTH_CIRCLE = (short) (NUMBER_OF_ANGLES / 8);
        public const short SIXTEENTH_CIRCLE = (short) (NUMBER_OF_ANGLES / 16);

        public const int WORLD_FRACTIONAL_BITS = 10;
        public const short WORLD_ONE = (short) (1 << WORLD_FRACTIONAL_BITS);
        public const short WORLD_ONE_HALF = (short) (WORLD_ONE / 2);
        public const short WORLD_ONE_FOURTH = (short) (WORLD_ONE / 4);
        public const short WORLD_THREE_FOURTHS = (short) ((WORLD_ONE * 3) / 4);

        public const ushort DEFAULT_RANDOM_SEED = (ushort) 0xfded;

        public static short INTEGER_TO_WORLD(int s) { return (short) (s << WORLD_FRACTIONAL_BITS); }
        public static short WORLD_FRACTIONAL_PART(short d) { return (short) (d & (WORLD_ONE - 1)); }
        public static short WORLD_INTEGERAL_PART(short d) { return (short) (d >> WORLD_FRACTIONAL_BITS); }

        public static int WORLD_TO_FIXED(short w) { return w << (FIXED_FRACTIONAL_BITS - WORLD_FRACTIONAL_BITS); }
        public static short FIXED_TO_WORLD(int f) { return (short) (f >> (FIXED_FRACTIONAL_BITS - WORLD_FRACTIONAL_BITS)); }

        public static int FACING4(int a) { return NORMALIZE_ANGLE(a - EIGHTH_CIRCLE) >> (ANGULAR_BITS - 2); }
        public static int FACING5(int a) { return (NORMALIZE_ANGLE(a - FULL_CIRCLE / 10)) / ((NUMBER_OF_ANGLES / 5) + 1); }
        public static int FACING8(int a) { return NORMALIZE_ANGLE(a - SIXTEENTH_CIRCLE) >> (ANGULAR_BITS - 3); }

        public static int GUESS_HYPOTENUSE(int x, int y) { return (x) > (y) ? ((x) + ((y) >> 1)) : ((y) + ((x) >> 1)); }

        public static short NORMALIZE_ANGLE(int t) { return (short) (t & (short) (NUMBER_OF_ANGLES - 1)); }

        /* ---------- code (world.cpp) */

        public static short guess_distance2d(world_point2d p0, world_point2d p1)
        {
            int dx = (int) p0.x - p1.x;
            int dy = (int) p0.y - p1.y;
            int distance;

            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;
            distance = GUESS_HYPOTENUSE(dx, dy);

            return (short) (distance > INT16_MAX ? INT16_MAX : distance);
        }

        // Return round(distance) if distance < 65536, else nonsense value round(sqrt(distance^2 - 2^32)); output in [0, 65536]
        private static int m2_distance2d_int32(world_point2d p0, world_point2d p1)
        {
            int dx = 1 * p1.x - p0.x; // [-65535, 65535]
            int dy = 1 * p1.y - p0.y; // [-65535, 65535]
            long dist_squared = 1L * dx * dx + 1L * dy * dy; // [0, ~2^33]
            return isqrt(unchecked((uint) dist_squared));
        }

        private static short m2_distance2d(world_point2d p0, world_point2d p1)
        {
            return unchecked((short) (m2_distance2d_int32(p0, p1)));
        }

        private static short a1_distance2d(world_point2d p0, world_point2d p1)
        {
            int distance = m2_distance2d_int32(p0, p1);
            return (short) (distance > INT16_MAX ? INT16_MAX : distance);
        }

        public static short distance2d(world_point2d p0, world_point2d p1)
        {
            if (film_profile.long_distance_physics)
            {
                return a1_distance2d(p0, p1);
            }
            else
            {
                return m2_distance2d(p0, p1);
            }
        }

        public static int isqrt(uint x)
        {
            uint r, nr, m;

            r = 0;
            m = 0x40000000;

            do
            {
                nr = r + m;
                if (nr <= x)
                {
                    x -= nr;
                    r = nr + m;
                }
                r >>= 1;
                m >>= 2;
            }
            while (m != 0);

            if (x > r) r += 1;
            return (int) r;
        }
    }
}
