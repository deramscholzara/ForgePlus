// Port of Aleph One: Source_Files/RenderOther/computer_interface.h, computer_interface.cpp (the map's terminals,
// and how their text is laid out into lines)
//
// map_terminal_text is MapLevel.map_terminal_text. Measuring text needs the terminal font, which isn't ported:
// its metrics are char_width() and _get_font_line_height() (see the ForgePlus section at the end).
//
// Not ported (the running game and drawing): the player's terminal state and its packing, entering, leaving and
// reading terminals (keys, teleports, sounds, tags), rendering (borders, text, pictures, the checkpoint map,
// static), Marathon 1's terminals from 'term' resources (get_indexed_terminal_data's resource path and
// MarathonTerminalCompiler), and the date string.
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csalerts;
using static AlephOne.csmacros;
using static AlephOne.cstypes;
using static AlephOne.FilmProfileGlobals;
using static AlephOne.Packing;
using static AlephOne.screen_drawing;

namespace AlephOne
{
    public class terminal_groupings
    {
        public short flags; /* varies.. */
        public short type; /* _information_text, _checkpoint_text, _briefing_text, _movie, _sound_bite, _soundtrack */
        public short permutation; /* checkpoint id for chkpt, level id for _briefing, movie id for movie, sound id for sound, soundtrack id for soundtrack */
        public short start_index;
        public short length;
        public short maximum_line_count;

        public terminal_groupings Clone()
        {
            return (terminal_groupings) MemberwiseClone();
        }
    }

    public class text_face_data
    {
        public short index;
        public short face;
        public short color;

        public text_face_data Clone()
        {
            return (text_face_data) MemberwiseClone();
        }
    }

    /* Terminal data loaded from map (maintained by computer_interface.cpp) */
    public class terminal_text_t // Object describing one terminal
    {
        public ushort flags = 0;
        public short lines_per_page = 0;
        public List<terminal_groupings> groupings = new List<terminal_groupings>();
        public List<text_face_data> font_changes = new List<text_face_data>();
        public List<byte> text = new List<byte>();
    }

    [NoAutoStaticsCleanup]
    public static class computer_interface
    {
        /* ------------ structures */
        public const int SIZEOF_static_preprocessed_terminal_data = 10;
        public const int SIZEOF_terminal_groupings = 12;
        public const int SIZEOF_text_face_data = 6;

        public const int LABEL_INSET = 3;
        public const int BORDER_HEIGHT = 18;
        public const int BORDER_INSET = 9;
        public const int FUDGE_FACTOR = 1;

        public const byte MAC_LINE_END = 13;

        /* Maximum face changes per text grouping.. */
        public const int MAXIMUM_FACE_CHANGES_PER_TEXT_GROUPING = 128;

        public const ushort _text_is_encoded_flag = 0x0001;

        public const short _logon_group = 0;
        public const short _unfinished_group = 1;
        public const short _success_group = 2;
        public const short _failure_group = 3;
        public const short _information_group = 4;
        public const short _end_group = 5;
        public const short _interlevel_teleport_group = 6; // permutation is level to go to
        public const short _intralevel_teleport_group = 7; // permutation is polygon to go to.
        public const short _checkpoint_group = 8; // permutation is the goal to show
        public const short _sound_group = 9; // permutation is the sound id to play
        public const short _movie_group = 10; // permutation is the movie id to play
        public const short _track_group = 11; // permutation is the track to play
        public const short _pict_group = 12; // permutation is the pict to display
        public const short _logoff_group = 13;
        public const short _camera_group = 14; //  permutation is the object index
        public const short _static_group = 15; // permutation is the duration of static.
        public const short _tag_group = 16; // permutation is the tag to activate
        public const short NUMBER_OF_GROUP_TYPES = 17;

        // flags to indicate text styles for paragraphs
        public const short _plain_text = 0x00;
        public const short _bold_text = 0x01;
        public const short _italic_text = 0x02;
        public const short _underline_text = 0x04;

        /* terminal grouping flags */
        public const short _draw_object_on_right = 0x01; // for drawing checkpoints, picts, movies.
        public const short _center_object = 0x02;
        public const short _group_is_marathon_1 = 0x100;

        // ghs: for Lua
        public static short number_of_terminal_texts(MapLevel level) { return (short) level.map_terminal_text.Count; }

        // Emulation of MacOS functions
        public static void InsetRect(Rect r, int dx, int dy)
        {
            r.top += (short) dy;
            r.left += (short) dx;
            r.bottom -= (short) dy;
            r.right -= (short) dx;
        }

        public static void OffsetRect(Rect r, int dx, int dy)
        {
            r.top += (short) dy;
            r.left += (short) dx;
            r.bottom += (short) dy;
            r.right += (short) dx;
        }

        private static bool can_break_after(byte c)
        {
            // determined empirically on my PowerBook
            switch ((char) c)
            {
                case '&':
                case '*':
                case '+':
                case '-':
                case '\\':
                case '<':
                case '=':
                case '>':
                case '/':
                case '^':
                case '|':
                    return true;
                default:
                    return false;
            }
        }

        public static bool calculate_line(List<byte> base_text, short width, short start_index, short text_end_index, out short end_index)
        {
            bool done = false;
            end_index = start_index;

            if (text_at(base_text, start_index) != 0)
            {
                int index = start_index, running_width = 0;

                while (running_width < width && text_at(base_text, index) != 0 && text_at(base_text, index) != MAC_LINE_END)
                {
                    running_width += char_width(text_at(base_text, index), current_style);
                    index++;
                }

                // Now go backwards, looking for a place to split
                if (text_at(base_text, index) == MAC_LINE_END)
                    index++;
                else if (text_at(base_text, index) != 0)
                {
                    if (film_profile.better_terminal_word_wrap)
                    {
                        int break_point = index - 1;
                        while (break_point > start_index)
                        {
                            if (text_at(base_text, break_point) == ' ')
                            {
                                index = break_point + 1; // eat the space
                                break;
                            }
                            else if (break_point > start_index + 1 &&
                                     can_break_after(text_at(base_text, break_point - 1)))
                            {
                                index = break_point;
                                break;
                            }

                            --break_point;
                        }
                    }
                    else
                    {
                        int break_point = index;

                        while (break_point > start_index)
                        {
                            if (text_at(base_text, break_point) == ' ')
                                break; // Non printing
                            break_point--; // this needs to be in front of the test
                        }

                        if (break_point != start_index)
                            index = break_point + 1; // Space at the end of the line
                    }
                }

                end_index = (short) index;
            }
            else
                done = true;

            return done;
        }

        public static short count_total_lines(List<byte> base_text, short width, short start_index, short end_index)
        {
            short total_line_count = 0;
            short text_end_index = end_index;

            ushort old_style = current_style;
            current_style = GetInterfaceStyle();

            while (!calculate_line(base_text, width, start_index, text_end_index, out end_index))
            {
                total_line_count++;
                start_index = end_index;
            }

            current_style = old_style;

            return total_line_count;
        }

        public static short find_group_type(terminal_text_t data, short group_type)
        {
            int index;

            for (index = 0; index < data.groupings.Count; index++)
            {
                terminal_groupings group = get_indexed_grouping(data, (short) index);
                // LP change: just in case...
                if (group == null) return NONE;
                if (group.type == group_type) break;
            }

            return (short) index;
        }

        public static void calculate_bounds_for_text_box(short flags, Rect bounds)
        {
            if ((flags & _center_object) != 0)
            {
                // dprintf("splitting text not supported!");
                calculate_bounds_for_object(_draw_object_on_right, bounds, null);
            }
            else if ((flags & _draw_object_on_right) != 0)
            {
                calculate_bounds_for_object(0, bounds, null);
            }
            else
            {
                calculate_bounds_for_object(_draw_object_on_right, bounds, null);
            }
            if ((flags & _group_is_marathon_1) != 0)
                bounds.top += _get_font_line_height();
        }

        public static void calculate_bounds_for_object(short flags, Rect bounds, Rect source)
        {
            if (source != null && (flags & _center_object) != 0)
            {
                copy_rect(get_term_rectangle(_terminal_logon_graphic_rect), bounds);
                if (RECTANGLE_WIDTH(source) > RECTANGLE_WIDTH(bounds)
                    || RECTANGLE_HEIGHT(source) > RECTANGLE_HEIGHT(bounds))
                {
                    /* Just return the normal frame.  Aspect ratio will take care of us.. */
                }
                else
                {
                    InsetRect(bounds, (RECTANGLE_WIDTH(bounds) - RECTANGLE_WIDTH(source)) / 2,
                        (RECTANGLE_HEIGHT(bounds) - RECTANGLE_HEIGHT(source)) / 2);
                }
            }
            else if ((flags & _draw_object_on_right) != 0)
            {
                copy_rect(get_term_rectangle(_terminal_right_rect), bounds);
            }
            else
            {
                copy_rect(get_term_rectangle(_terminal_left_rect), bounds);
            }
        }

        public const int MAXIMUM_GROUPS_PER_TERMINAL = 15;

        public static void calculate_maximum_lines_for_groups(List<terminal_groupings> groups, short group_count, List<byte> text_base)
        {
            short index;

            for (index = 0; index < group_count; ++index)
            {
                switch (groups[index].type)
                {
                    case _logon_group:
                    case _logoff_group:
                    case _interlevel_teleport_group:
                    case _intralevel_teleport_group:
                    case _sound_group:
                    case _tag_group:
                    case _movie_group:
                    case _track_group:
                    case _camera_group:
                    case _static_group:
                    case _end_group:
                        groups[index].maximum_line_count = 1; // any click or keypress gets us out.
                        break;

                    case _unfinished_group:
                    case _success_group:
                    case _failure_group:
                        groups[index].maximum_line_count = 0; // should never get to one of these groups.
                        break;

                    case _checkpoint_group:
                    case _pict_group:
                    {
                        var text_bounds = new Rect();

                        /* The only thing we care about is the width. */
                        calculate_bounds_for_text_box(groups[index].flags, text_bounds);
                        groups[index].maximum_line_count = count_total_lines(text_base,
                            RECTANGLE_WIDTH(text_bounds), groups[index].start_index,
                            (short) (groups[index].start_index + groups[index].length));
                        break;
                    }

                    case _information_group:
                    {
                        Rect text_bounds = get_term_rectangle(_terminal_full_text_rect);

                        groups[index].maximum_line_count = count_total_lines(text_base,
                            RECTANGLE_WIDTH(text_bounds), groups[index].start_index,
                            (short) (groups[index].start_index + groups[index].length));
                        break;
                    }

                    default:
                        break;
                }
            }
        }

        public static terminal_groupings get_indexed_grouping(terminal_text_t data, short index)
        {
            if (index < 0 || index >= data.groupings.Count)
                return null;

            return data.groupings[index];
        }

        public static text_face_data get_indexed_font_changes(terminal_text_t data, short index)
        {
            if (index < 0 || index >= data.font_changes.Count)
                return null;

            return data.font_changes[index];
        }

        public static List<byte> get_text_base(terminal_text_t data)
        {
            return data.text;
        }

        public static short calculate_lines_per_page()
        {
            Rect bounds;
            short lines_per_page;

            if (!film_profile.calculate_terminal_lines_correctly)
            {
                bounds = get_term_rectangle(_terminal_screen_rect);
                lines_per_page = (short) ((RECTANGLE_HEIGHT(bounds) - 2 * BORDER_HEIGHT) / _get_font_line_height());
                lines_per_page -= FUDGE_FACTOR;
            }
            else
            {
                bounds = get_term_rectangle(_terminal_full_text_rect);
                lines_per_page = (short) (RECTANGLE_HEIGHT(bounds) / _get_font_line_height());
            }

            return lines_per_page;
        }

        public static Rect get_term_rectangle(short index)
        {
            var bounds = new Rect();
            screen_rectangle term_rect = get_interface_rectangle(_terminal_screen_rect);
            screen_rectangle target_rect = get_interface_rectangle(index);
            bounds.left = (short) (target_rect.left - term_rect.left);
            bounds.top = (short) (target_rect.top - term_rect.top);
            bounds.right = (short) (target_rect.right - term_rect.left);
            bounds.bottom = (short) (target_rect.bottom - term_rect.top);
            return bounds;
        }

        // Only the map's terminals: see the file header
        public static terminal_text_t get_indexed_terminal_data(MapLevel level, short id)
        {
            if (id < 0 || id >= level.map_terminal_text.Count)
            {
                return null;
            }

            terminal_text_t t = level.map_terminal_text[id];

            // Note that this will only decode the text once
            decode_text(t);
            return t;
        }

        private static void decode_text(terminal_text_t terminal_text)
        {
            if ((terminal_text.flags & _text_is_encoded_flag) != 0)
            {
                encode_text(terminal_text);
                terminal_text.flags &= unchecked((ushort) ~_text_is_encoded_flag);
            }
        }

        private static void encode_text(terminal_text_t terminal_text)
        {
            int length = terminal_text.text.Count;
            List<byte> p = terminal_text.text;
            int pi = 0;

            for (int i = 0; i < length / 4; i++)
            {
                pi += 2;
                p[pi] ^= 0xfe; pi++;
                p[pi] ^= 0xed; pi++;
            }
            for (int i = 0; i < length % 4; i++)
            {
                p[pi] ^= 0xfe; pi++;
            }

            terminal_text.flags |= _text_is_encoded_flag;
        }

        /*
         *  Calculate the length the loaded terminal data would take up on disk
         *  (for saving)
         */
        private static int packed_terminal_length(terminal_text_t t)
        {
            return SIZEOF_static_preprocessed_terminal_data
                 + t.groupings.Count * SIZEOF_terminal_groupings
                 + t.font_changes.Count * SIZEOF_text_face_data
                 + t.text.Count;
        }

        public static int calculate_packed_terminal_data_length(MapLevel level)
        {
            int total = 0;

            // Loop for all terminals
            foreach (terminal_text_t t in level.map_terminal_text)
            {
                total += packed_terminal_length(t);
            }

            return total;
        }

        /*
         *  Unpack terminal data from stream
         */
        public static void unpack_map_terminal_data(MapLevel level, StreamPointer p, int count)
        {
            // Clear existing terminals
            level.map_terminal_text.Clear();

            // Unpack all terminals
            while (count > 0)
            {
                // Create new terminal_text_t
                var data = new terminal_text_t();
                level.map_terminal_text.Add(data);

                // Read header
                int p_start = p.Position, p_header = p.Position;
                ushort total_length, grouping_count, font_changes_count;
                StreamToValue(p, out total_length);
                StreamToValue(p, out data.flags);
                StreamToValue(p, out data.lines_per_page);
                StreamToValue(p, out grouping_count);
                StreamToValue(p, out font_changes_count);
                assert((p.Position - p_start) == SIZEOF_static_preprocessed_terminal_data);

                // Reserve memory for groupings and font changes
                data.groupings.Capacity = grouping_count;
                data.font_changes.Capacity = font_changes_count;

                // Read groupings
                p_start = p.Position;
                for (int i = 0; i < grouping_count; i++)
                {
                    var g = new terminal_groupings();
                    StreamToValue(p, out g.flags);
                    StreamToValue(p, out g.type);
                    StreamToValue(p, out g.permutation);
                    StreamToValue(p, out g.start_index);
                    StreamToValue(p, out g.length);
                    StreamToValue(p, out g.maximum_line_count);
                    data.groupings.Add(g);
                }
                assert((p.Position - p_start) == SIZEOF_terminal_groupings * grouping_count);

                // Read font changes
                p_start = p.Position;
                for (int i = 0; i < font_changes_count; i++)
                {
                    var f = new text_face_data();
                    StreamToValue(p, out f.index);
                    StreamToValue(p, out f.face);
                    StreamToValue(p, out f.color);
                    data.font_changes.Add(f);
                }
                assert((p.Position - p_start) == SIZEOF_text_face_data * font_changes_count);

                // Read text (no conversion)
                int text_length = total_length - (p.Position - p_header);
                assert(text_length >= 0);
                var text = new byte[text_length];
                StreamToBytes(p, text, text.Length);
                data.text.AddRange(text);

                // Continue with next terminal
                count -= total_length;
            }
        }

        /*
         *  Pack terminal data to stream
         */
        public static void pack_map_terminal_data(MapLevel level, StreamPointer p, int count)
        {
            // Pack all terminals
            foreach (terminal_text_t t in level.map_terminal_text)
            {
                // Write header
                int p_start = p.Position;
                ushort total_length = (ushort) packed_terminal_length(t);
                ushort grouping_count = (ushort) t.groupings.Count;
                ushort font_changes_count = (ushort) t.font_changes.Count;
                ValueToStream(p, total_length);
                ValueToStream(p, t.flags);
                ValueToStream(p, t.lines_per_page);
                ValueToStream(p, grouping_count);
                ValueToStream(p, font_changes_count);
                assert((p.Position - p_start) == SIZEOF_static_preprocessed_terminal_data);

                // Write groupings
                p_start = p.Position;
                foreach (terminal_groupings g in t.groupings)
                {
                    ValueToStream(p, g.flags);
                    ValueToStream(p, g.type);
                    ValueToStream(p, g.permutation);
                    ValueToStream(p, g.start_index);
                    ValueToStream(p, g.length);
                    ValueToStream(p, g.maximum_line_count);
                }
                assert((p.Position - p_start) == SIZEOF_terminal_groupings * grouping_count);

                // Write font changes
                p_start = p.Position;
                foreach (text_face_data f in t.font_changes)
                {
                    ValueToStream(p, f.index);
                    ValueToStream(p, f.face);
                    ValueToStream(p, f.color);
                }
                assert((p.Position - p_start) == SIZEOF_text_face_data * font_changes_count);

                // Write text (no conversion)
                BytesToStream(p, t.text.ToArray(), t.text.Count);
            }
        }

        /* ---------- ForgePlus: the terminal font, and reading past the end of a terminal's text */

        // The style calculate_line measures with (Aleph One's current_style)
        public static ushort current_style = 0;

        // Aleph One's _computer_interface_font is Courier Prime 12 (screen_drawing.cpp's InterfaceFonts), whose
        // characters are all 7 pixels wide in every style, and whose lines are 12 pixels apart
        public const short terminal_font_character_width = 7;
        public const short terminal_font_line_height = 12;

        private static int char_width(byte c, ushort style)
        {
            return terminal_font_character_width;
        }

        public static short _get_font_line_height()
        {
            return terminal_font_line_height;
        }

        public static ushort GetInterfaceStyle()
        {
            return 0; // styleNormal
        }

        // base_text[index], with Aleph One's NUL past the end of the text (its text is followed by more data or
        // padding, where the port's list just ends)
        public static byte text_at(List<byte> base_text, int index)
        {
            return (uint) index < (uint) base_text.Count ? base_text[index] : (byte) 0;
        }

        private static void copy_rect(Rect source, Rect destination)
        {
            destination.top = source.top;
            destination.left = source.left;
            destination.bottom = source.bottom;
            destination.right = source.right;
        }
    }
}
