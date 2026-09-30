// Port of Aleph One: Source_Files/RenderOther/screen_drawing.h, screen_drawing.cpp (the interface's rectangles and colors)
//
// Not ported: drawing, fonts, and the MML parsers that change the rectangles and colors.
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csalerts;

namespace AlephOne
{
    public class screen_rectangle
    {
        public short top, left;
        public short bottom, right;

        public screen_rectangle(short top, short left, short bottom, short right)
        {
            this.top = top;
            this.left = left;
            this.bottom = bottom;
            this.right = right;
        }
    }

    [NoAutoStaticsCleanup]
    public static class screen_drawing
    {
        /* Rectangles for the interface, etc.. */
        /* rectangle id's */

        /* game window rectangles */
        public const short _player_name_rect = 0;
        public const short _oxygen_rect = 1;
        public const short _shield_rect = 2;
        public const short _motion_sensor_rect = 3;
        public const short _microphone_rect = 4;
        public const short _inventory_rect = 5;
        public const short _weapon_display_rect = 6;

        /* interface rectangles */
        public const short START_OF_MENU_INTERFACE_RECTS = 7;
        public const short _new_game_button_rect = 7;
        public const short _load_game_button_rect = 8;
        public const short _gather_button_rect = 9;
        public const short _join_button_rect = 10;
        public const short _prefs_button_rect = 11;
        public const short _replay_last_button_rect = 12;
        public const short _save_last_button_rect = 13;
        public const short _replay_saved_button_rect = 14;
        public const short _credits_button_rect = 15;
        public const short _quit_button_rect = 16;
        public const short _center_button_rect = 17;
        public const short _singleton_game_button_rect = 18;
        public const short _about_alephone_rect = 19;
        public const short END_OF_MENU_INTERFACE_RECTS = 20;

        /* Marathon compatibility rectangles */
        public const short _terminal_screen_rect = 20;
        public const short _terminal_header_rect = 21;
        public const short _terminal_footer_rect = 22;
        public const short _terminal_full_text_rect = 23;
        public const short _terminal_left_rect = 24;
        public const short _terminal_right_rect = 25;
        public const short _terminal_logon_graphic_rect = 26;
        public const short _terminal_logon_title_rect = 27;
        public const short _terminal_logon_location_rect = 28;
        public const short _respawn_indicator_rect = 29;
        public const short _blinker_rect = 30;

        public const short NUMBER_OF_INTERFACE_RECTANGLES = 31;

        /* Colors for drawing.. */
        public const short _energy_weapon_full_color = 0;
        public const short _energy_weapon_empty_color = 1;
        public const short _black_color = 2;
        public const short _inventory_text_color = 3;
        public const short _inventory_header_background_color = 4;
        public const short _inventory_background_color = 5;
        public const short PLAYER_COLOR_BASE_INDEX = 6;

        public const short _white_color = 14;
        public const short _invalid_weapon_color = 15;
        public const short _computer_border_background_text_color = 16;
        public const short _computer_border_text_color = 17;
        public const short _computer_interface_text_color = 18;
        public const short _computer_interface_color_purple = 19;
        public const short _computer_interface_color_red = 20;
        public const short _computer_interface_color_pink = 21;
        public const short _computer_interface_color_aqua = 22;
        public const short _computer_interface_color_yellow = 23;
        public const short _computer_interface_color_brown = 24;
        public const short _computer_interface_color_blue = 25;
        public const short NUMBER_OF_INTERFACE_COLORS = 26;

        private static readonly screen_rectangle[] interface_rectangles = new screen_rectangle[NUMBER_OF_INTERFACE_RECTANGLES]
        {
            new screen_rectangle(326, 300, 338, 473),
            new screen_rectangle(464, 398, 475, 578),
            new screen_rectangle(464, 181, 475, 361),
            new screen_rectangle(338, 17, 0, 0),
            new screen_rectangle(0, 0, 0, 0),
            new screen_rectangle(352, 204, 454, 384),
            new screen_rectangle(352, 384, 454, 596),
            new screen_rectangle(179, 101, 210, 268),
            new screen_rectangle(221, 25, 253, 238),
            new screen_rectangle(263, 11, 294, 223),
            new screen_rectangle(301, 38, 333, 236),
            new screen_rectangle(304, 421, 331, 563),
            new screen_rectangle(386, 231, 413, 406),
            new screen_rectangle(345, 363, 372, 516),
            new screen_rectangle(344, 83, 374, 271),
            new screen_rectangle(206, 246, 347, 382),
            // {264, 522, 291, 588}, // inf's bounds
            // {263, 497, 294, 565}, // m2's bounds
            new screen_rectangle(263, 500, 294, 585), // adjusted to work with both m2 and inf
            new screen_rectangle(0, 0, 0, 0),
            new screen_rectangle(0, 0, 0, 0),
            new screen_rectangle(0, 0, 0, 0),
            new screen_rectangle(0, 0, 320, 640),
            new screen_rectangle(0, 0, 18, 640),
            new screen_rectangle(302, 0, 320, 640),
            new screen_rectangle(27, 72, 293, 568),
            new screen_rectangle(27, 9, 293, 316),
            new screen_rectangle(27, 324, 293, 631),
            new screen_rectangle(27, 9, 293, 631),
            new screen_rectangle(0, 0, 0, 0),
            new screen_rectangle(0, 0, 0, 0),
            new screen_rectangle(0, 0, 0, 0),
            new screen_rectangle(0, 0, 0, 0),
        };

        // LP change: hardcoding the interface and player colors,
        // so as to banish the 'clut' resources
        public const int NumInterfaceColors = 26;
        private static readonly rgb_color[] InterfaceColors = new rgb_color[NumInterfaceColors]
        {
            new rgb_color(0, 65535, 0),
            new rgb_color(0, 5140, 0),
            new rgb_color(0, 0, 0),

            new rgb_color(0, 65535, 0),
            new rgb_color(0, 12956, 0),
            new rgb_color(0, 5100, 0),

            new rgb_color(9216, 24320, 41728),
            new rgb_color(65535, 0, 0),
            new rgb_color(45056, 0, 24064),
            new rgb_color(65535, 65535, 0),
            new rgb_color(60000, 60000, 60000),
            new rgb_color(62976, 22528, 0),
            new rgb_color(3072, 0, 65535),
            new rgb_color(0, 65535, 0),

            new rgb_color(65535, 65535, 65535),
            new rgb_color(0, 5140, 0),

            new rgb_color(10000, 0, 0),
            new rgb_color(65535, 0, 0),

            new rgb_color(0, 65535, 0),
            new rgb_color(65535, 65535, 65535),
            new rgb_color(65535, 0, 0),
            new rgb_color(0, 40000, 0),
            new rgb_color(0, 45232, 51657),
            new rgb_color(65535, 59367, 0),
            new rgb_color(45000, 0, 0),
            new rgb_color(3084, 0, 65535),
        };

        public static screen_rectangle get_interface_rectangle(short index)
        {
            assert(index >= 0 && index < NUMBER_OF_INTERFACE_RECTANGLES);
            return interface_rectangles[index];
        }

        public static rgb_color get_interface_color(short index)
        {
            assert(index >= 0 && index < NumInterfaceColors);
            return InterfaceColors[index];
        }

        public static void _get_interface_color(int color_index, SDL_Color color)
        {
            assert((uint) color_index < NumInterfaceColors);

            rgb_color c = InterfaceColors[color_index];
            color.r = (byte) (c.red >> 8);
            color.g = (byte) (c.green >> 8);
            color.b = (byte) (c.blue >> 8);
            color.a = 0xff;
        }
    }
}
