using AlephOne;
using System;
using System.Collections.Generic;
using System.Text;
using static AlephOne.computer_interface;
using static AlephOne.csmacros;
using static AlephOne.FilmProfileGlobals;

namespace ForgePlus.UI
{
    // Editing a terminal a group at a time as Bungie's terminal source wrote it (style codes $B/$b, $I/$i, $U/$u and
    // $C0 to $C7 in its text), stored as Bungie's compiler stored it (computer_interface.cpp: preprocess_text)
    public static class TerminalSource
    {
        // A group as it's edited: its own text and style changes (indexed from its text's start)
        public class Group
        {
            public short Type;
            public short Flags;
            public short Permutation;
            public short MaximumLineCount;
            public readonly List<byte> Text = new List<byte>();
            public readonly List<text_face_data> Faces = new List<text_face_data>();

            // Whether its lines are counted again when it's stored (an unchanged group keeps its saved count)
            public bool IsChanged;
        }

        public enum StyleKind
        {
            Plain,
            Face,
            Color,
        }

        // A terminal's groups, each with its own copy of its text and style changes
        public static List<Group> GetGroups(terminal_text_t terminal)
        {
            var groups = new List<Group>();

            for (var groupIndex = 0; groupIndex < terminal.groupings.Count; groupIndex++)
            {
                var grouping = terminal.groupings[groupIndex];
                var group = new Group
                {
                    Type = grouping.type,
                    Flags = grouping.flags,
                    Permutation = grouping.permutation,
                    MaximumLineCount = grouping.maximum_line_count,
                };

                var start = grouping.start_index;
                var end = Math.Min(start + grouping.length, terminal.text.Count);
                for (var index = start; index < end; index++)
                {
                    group.Text.Add(terminal.text[index]);
                }

                foreach (var change in terminal.font_changes)
                {
                    if (IsInGroup(terminal, groupIndex, change.index))
                    {
                        group.Faces.Add(new text_face_data { index = (short) (change.index - start), face = change.face, color = change.color });
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        // Stores the groups in the terminal, as preprocess_text does: their texts one after another, each ended by a NUL
        // (which ends its lines), and their style changes indexed from the whole text's start. Returns false (leaving the
        // terminal as it was) if they don't fit in a terminal.
        public static bool SetGroups(terminal_text_t terminal, List<Group> groups)
        {
            var text = new List<byte>();
            var faces = new List<text_face_data>();
            var groupings = new List<terminal_groupings>();

            foreach (var group in groups)
            {
                var start = text.Count;

                text.AddRange(group.Text);
                foreach (var face in group.Faces)
                {
                    faces.Add(new text_face_data { index = (short) (start + face.index), face = face.face, color = face.color });
                }

                groupings.Add(new terminal_groupings
                {
                    flags = group.Flags,
                    type = group.Type,
                    permutation = group.Permutation,
                    start_index = (short) start,
                    length = (short) group.Text.Count,
                    maximum_line_count = group.MaximumLineCount,
                });

                // A group's start and length are 16-bit
                if (text.Count > short.MaxValue)
                {
                    return false;
                }

                text.Add(0);
            }

            // So is the whole terminal's length
            var packedLength = SIZEOF_static_preprocessed_terminal_data + groupings.Count * SIZEOF_terminal_groupings +
                               faces.Count * SIZEOF_text_face_data + text.Count;
            if (packedLength > ushort.MaxValue)
            {
                return false;
            }

            terminal.text.Clear();
            terminal.text.AddRange(text);
            terminal.font_changes.Clear();
            terminal.font_changes.AddRange(faces);
            terminal.groupings.Clear();
            terminal.groupings.AddRange(groupings);

            for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                if (groups[groupIndex].IsChanged)
                {
                    groupings[groupIndex].maximum_line_count = CountMaximumLines(terminal, groupings[groupIndex]);
                    groups[groupIndex].MaximumLineCount = groupings[groupIndex].maximum_line_count;
                    groups[groupIndex].IsChanged = false;
                }
            }

            return true;
        }

        // The group's text as source: its characters (its line ends as line breaks), with a code for each style change,
        // from plain text in color 0 (where each group starts)
        public static string GetSource(Group group)
        {
            return GetSource(group, -1, -1, out _, out _);
        }

        // With where two of its characters are in the source: the first after its codes, and the second before them
        // (so text selected from one to the other doesn't include the codes around it)
        private static string GetSource(Group group, int firstIndex, int lastIndex, out int firstPosition, out int lastPosition)
        {
            var builder = new StringBuilder(group.Text.Count);
            short face = _plain_text;
            short color = 0;
            var faceIndex = 0;

            firstPosition = lastPosition = 0;

            for (var index = 0; index <= group.Text.Count; index++)
            {
                if (index == lastIndex)
                {
                    lastPosition = builder.Length;
                }

                while (faceIndex < group.Faces.Count && group.Faces[faceIndex].index <= index)
                {
                    AppendCodes(builder, ref face, ref color, group.Faces[faceIndex].face, group.Faces[faceIndex].color);
                    faceIndex++;
                }

                if (index == firstIndex)
                {
                    firstPosition = builder.Length;
                }

                if (index == group.Text.Count)
                {
                    break;
                }

                // A NUL (which some groups' texts end with) isn't text: storing the group ends its text with one
                var character = group.Text[index];
                if (character != 0)
                {
                    builder.Append(character == MAC_LINE_END ? '\n' : (char) csstrings.mac_roman_to_unicode(character));
                }
            }

            return builder.ToString();
        }

        // The group's text and style changes from source, as pre_build_groups reads a group's (each code is taken out of
        // the text, and marks a style change where it was)
        public static void SetSource(Group group, string source)
        {
            var baseText = ToMacRoman(source);
            short currentFace = _plain_text;
            short colorIndex = 0;

            group.Text.Clear();
            group.Faces.Clear();
            group.IsChanged = true;

            var index = 0;
            while (index < baseText.Count)
            {
                if (baseText[index] == '$')
                {
                    var changedFont = true;
                    var moveDownBy = 2;

                    switch ((char) At(baseText, index + 1))
                    {
                        case 'B': /* Bold on! */
                            currentFace |= _bold_text;
                            break;
                        case 'b': /* bold off */
                            currentFace &= ~_bold_text;
                            break;
                        case 'I': /* Italic on */
                            currentFace |= _italic_text;
                            break;
                        case 'i': /* Italic off */
                            currentFace &= ~_italic_text;
                            break;
                        case 'U': /* Underline on */
                            currentFace |= _underline_text;
                            break;
                        case 'u': /* Underline off */
                            currentFace &= ~_underline_text;
                            break;
                        case 'C':
                            var digit = At(baseText, index + 2);
                            if (digit >= '0' && digit <= '7')
                            {
                                colorIndex = (short) (digit - '0');
                                moveDownBy = 3;
                            }
                            break;
                        default:
                            /* Pass it on through, unchanged */
                            changedFont = false;
                            break;
                    }

                    if (changedFont)
                    {
                        group.Faces.Add(new text_face_data { index = (short) group.Text.Count, face = currentFace, color = colorIndex });
                        index += moveDownBy;
                        continue;
                    }
                }

                group.Text.Add(baseText[index]);
                index++;
            }
        }

        // The source with the style applied as a rich text editor applies it, and the selection moved with its text.
        // A face (or plain) toggles on the selected characters, and the codes are written again so none changes
        // nothing; a color (or a face with nothing selected) is a code where the selection starts.
        public static string ApplyStyle(string source, ref int start, ref int end, StyleKind kind, short value)
        {
            if (start > end)
            {
                (start, end) = (end, start);
            }

            if (kind == StyleKind.Color || start == end)
            {
                GetStyleAt(source, start, out var faceAtStart, out _);

                var code = new StringBuilder();
                if (kind == StyleKind.Color)
                {
                    code.Append("$C").Append(value);
                }
                else
                {
                    AppendFaceCodes(code, faceAtStart, kind == StyleKind.Plain ? _plain_text : (short) (faceAtStart ^ value));
                }

                var result = source.Insert(start, code.ToString());
                start += code.Length;
                end += code.Length;

                return result;
            }

            var group = new Group();
            SetSource(group, source);

            var firstIndex = TextIndexAt(source, start);
            var lastIndex = TextIndexAt(source, end);

            // Each character's style, from the style changes at or before it
            var faces = new short[group.Text.Count];
            var colors = new short[group.Text.Count];
            short face = _plain_text;
            short color = 0;
            var changeIndex = 0;
            for (var index = 0; index < group.Text.Count; index++)
            {
                while (changeIndex < group.Faces.Count && group.Faces[changeIndex].index <= index)
                {
                    face = group.Faces[changeIndex].face;
                    color = group.Faces[changeIndex].color;
                    changeIndex++;
                }

                faces[index] = face;
                colors[index] = color;
            }

            var allHaveFace = true;
            for (var index = firstIndex; index < lastIndex; index++)
            {
                allHaveFace &= (faces[index] & value) != 0;
            }

            for (var index = firstIndex; index < lastIndex; index++)
            {
                faces[index] = kind == StyleKind.Plain ? _plain_text :
                               allHaveFace ? (short) (faces[index] & ~value) :
                               (short) (faces[index] | value);
            }

            // A style change wherever a character's style differs from the one before it
            group.Faces.Clear();
            face = _plain_text;
            color = 0;
            for (var index = 0; index < group.Text.Count; index++)
            {
                if (faces[index] != face || colors[index] != color)
                {
                    face = faces[index];
                    color = colors[index];
                    group.Faces.Add(new text_face_data { index = (short) index, face = face, color = color });
                }
            }

            return GetSource(group, firstIndex, lastIndex, out start, out end);
        }

        // Which of the text's characters is at a place in its source
        private static int TextIndexAt(string source, int position)
        {
            return GroupBefore(source, position).Text.Count;
        }

        // The face and color in effect at a place in source
        private static void GetStyleAt(string source, int position, out short face, out short color)
        {
            var group = GroupBefore(source, position);

            face = _plain_text;
            color = 0;
            if (group.Faces.Count > 0)
            {
                face = group.Faces[group.Faces.Count - 1].face;
                color = group.Faces[group.Faces.Count - 1].color;
            }
        }

        // The source up to a place in it, read as a group's
        private static Group GroupBefore(string source, int position)
        {
            var group = new Group();
            SetSource(group, source.Substring(0, Math.Min(position, source.Length)));

            return group;
        }

        private static void AppendCodes(StringBuilder builder, ref short face, ref short color, short newFace, short newColor)
        {
            AppendFaceCodes(builder, face, newFace);
            face = newFace;

            // A color beyond the codes' can't be written (and isn't one the games draw)
            if (newColor != color && newColor >= 0 && newColor < TerminalText.ColorCount)
            {
                builder.Append("$C").Append(newColor);
                color = newColor;
            }
        }

        private static void AppendFaceCodes(StringBuilder builder, short face, short newFace)
        {
            AppendFaceCode(builder, face, newFace, _bold_text, "$B", "$b");
            AppendFaceCode(builder, face, newFace, _italic_text, "$I", "$i");
            AppendFaceCode(builder, face, newFace, _underline_text, "$U", "$u");
        }

        private static void AppendFaceCode(StringBuilder builder, short face, short newFace, short flag, string on, string off)
        {
            if ((face & flag) != (newFace & flag))
            {
                builder.Append((newFace & flag) != 0 ? on : off);
            }
        }

        // A style change at the end of a group's text is the group's (it's where the NUL ending the text is), unless
        // another group's text starts there
        private static bool IsInGroup(terminal_text_t terminal, int groupIndex, short index)
        {
            var grouping = terminal.groupings[groupIndex];
            var end = grouping.start_index + grouping.length;

            if (index >= grouping.start_index && index < end)
            {
                return true;
            }

            return index == end && !terminal.groupings.Exists(other => other.start_index <= index && index < other.start_index + other.length);
        }

        // As preprocess_text's calculate_maximum_lines_for_groups counts them (the original games scroll through that
        // many lines, where Aleph One counts them itself): for its 640 by 480 terminal, wrapping as the original games do
        private static short CountMaximumLines(terminal_text_t terminal, terminal_groupings group)
        {
            switch (group.type)
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
                    return 1; // any click or keypress gets us out.
                case _checkpoint_group:
                case _pict_group:
                {
                    /* The only thing we care about is the width. */
                    var textBounds = new Rect();
                    calculate_bounds_for_text_box(group.flags, textBounds);
                    return CountLines(terminal, group, RECTANGLE_WIDTH(textBounds));
                }
                case _information_group:
                {
                    short width = 640;
                    width -= 2 * (72 - BORDER_INSET); /* 1 inch in from each side */
                    return CountLines(terminal, group, width);
                }
                default:
                    return 0; // should never get to one of these groups.
            }
        }

        private static short CountLines(terminal_text_t terminal, terminal_groupings group, short width)
        {
            var betterWordWrap = film_profile.better_terminal_word_wrap;
            film_profile.better_terminal_word_wrap = false;

            try
            {
                return count_total_lines(terminal.text, width, group.start_index, (short) (group.start_index + group.length));
            }
            finally
            {
                film_profile.better_terminal_word_wrap = betterWordWrap;
            }
        }

        // Line breaks as the games' line ends, and characters Mac Roman doesn't have as '?' (as Aleph One converts them)
        private static List<byte> ToMacRoman(string source)
        {
            var text = new List<byte>(source.Length);

            for (var index = 0; index < source.Length; index++)
            {
                var character = source[index];
                if (character == '\r')
                {
                    if (index + 1 < source.Length && source[index + 1] == '\n')
                    {
                        continue;
                    }

                    character = '\n';
                }

                if (character == '\n')
                {
                    text.Add(MAC_LINE_END);
                }
                else if (character != 0)
                {
                    text.Add(csstrings.unicode_to_mac_roman(character));
                }
            }

            return text;
        }

        private static byte At(List<byte> text, int index)
        {
            return index < text.Count ? text[index] : (byte) 0;
        }
    }
}
