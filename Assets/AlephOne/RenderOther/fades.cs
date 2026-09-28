// Port of Aleph One: Source_Files/RenderOther/fades.h (fade and fade effect types only)
//
// Not ported: fading itself (fades.cpp), gamma, and the fade definition tables, which only affect the screen.
namespace AlephOne
{
    public static class fades
    {
        public const short NUMBER_OF_GAMMA_LEVELS = 8;
        public const short DEFAULT_GAMMA_LEVEL = 2;

        /* fade types */
        public const short _start_cinematic_fade_in = 0; /* force all colors to black immediately */
        public const short _cinematic_fade_in = 1; /* fade in from black */
        public const short _long_cinematic_fade_in = 2;
        public const short _cinematic_fade_out = 3; /* fade out from black */
        public const short _end_cinematic_fade_out = 4; /* force all colors from black immediately */

        public const short _fade_red = 5; /* bullets and fist */
        public const short _fade_big_red = 6; /* bigger bullets and fists */
        public const short _fade_bonus = 7; /* picking up items */
        public const short _fade_bright = 8; /* teleporting */
        public const short _fade_long_bright = 9; /* nuclear monster detonations */
        public const short _fade_yellow = 10; /* explosions */
        public const short _fade_big_yellow = 11; /* big explosions */
        public const short _fade_purple = 12; /* ? */
        public const short _fade_cyan = 13; /* fighter staves and projectiles */
        public const short _fade_white = 14; /* absorbed */
        public const short _fade_big_white = 15; /* rocket (probably) absorbed */
        public const short _fade_orange = 16; /* flamethrower */
        public const short _fade_long_orange = 17; /* marathon lava */
        public const short _fade_green = 18; /* hunter projectile */
        public const short _fade_long_green = 19; /* alien green goo */
        public const short _fade_static = 20; /* compiler projectile */
        public const short _fade_negative = 21; /* minor fusion projectile */
        public const short _fade_big_negative = 22; /* major fusion projectile */
        public const short _fade_flicker_negative = 23; /* hummer projectile */
        public const short _fade_dodge_purple = 24; /* alien weapon */
        public const short _fade_burn_cyan = 25; /* armageddon beast electricity */
        public const short _fade_dodge_yellow = 26; /* armageddon beast projectile */
        public const short _fade_burn_green = 27; /* hunter projectile */

        public const short _fade_tint_green = 28; /* under goo */
        public const short _fade_tint_blue = 29; /* under water */
        public const short _fade_tint_orange = 30; /* under lava */
        public const short _fade_tint_gross = 31; /* under sewage */
        public const short _fade_tint_jjaro = 32; /* under JjaroGoo */ // LP addition

        public const short NUMBER_OF_FADE_TYPES = 33;

        // LP change: rearranged to get order: water, lava, sewage, jjaro, pfhor
        /* effect types */
        public const short _effect_under_water = 0;
        public const short _effect_under_lava = 1;
        public const short _effect_under_sewage = 2;
        public const short _effect_under_jjaro = 3;
        public const short _effect_under_goo = 4;
        public const short NUMBER_OF_FADE_EFFECT_TYPES = 5;
    }
}
