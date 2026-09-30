using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using System.Collections.Generic;
using static AlephOne.computer_interface;
using static AlephOne.csmacros;
using static AlephOne.cstypes;
using static AlephOne.FilmProfileGlobals;
using static AlephOne.screen_drawing;
using Rect = AlephOne.Rect;

namespace ForgePlus.UI
{
    // Where Aleph One draws a terminal's pages (in terminal coordinates, 640 by 320), and what it draws on them:
    // the pages follow its paging (goto_terminal_group and the page keys), and the text its _draw_computer_text
    public static class TerminalLayout
    {
        // One screenful: a group, from one of its lines
        public class Page
        {
            public short GroupIndex;
            public terminal_groupings Group;
            public short FirstLine;

            // terminal_data->maximum_line, the lines the group scrolls through
            public short MaximumLine;

            public int PageOfGroup;
            public int PagesOfGroup;
        }

        public class Run
        {
            public string Text;
            public int Column;
            public short Face;
            public short Color;
        }

        public class Line
        {
            public int Number;
            public short StartIndex;
            public short EndIndex;
            public readonly List<Run> Runs = new List<Run>();
        }

        // A group's page is at each multiple of lines_per_page below its maximum line (and there's always one),
        // since paging down past the maximum line goes on to the next group
        public static List<Page> GetPages(terminal_text_t terminal)
        {
            var pages = new List<Page>();
            var linesPerPage = terminal.lines_per_page > 0 ? terminal.lines_per_page : (short) 1;

            for (short groupIndex = 0; groupIndex < terminal.groupings.Count; groupIndex++)
            {
                var group = terminal.groupings[groupIndex];
                var maximumLine = GetMaximumLine(terminal, group);
                var pageCount = HasLines(group.type) ? System.Math.Max(1, (maximumLine + linesPerPage - 1) / linesPerPage) : 1;

                for (var pageOfGroup = 0; pageOfGroup < pageCount; pageOfGroup++)
                {
                    pages.Add(new Page
                    {
                        GroupIndex = groupIndex,
                        Group = group,
                        FirstLine = (short) (pageOfGroup * linesPerPage),
                        MaximumLine = maximumLine,
                        PageOfGroup = pageOfGroup,
                        PagesOfGroup = pageCount,
                    });
                }
            }

            return pages;
        }

        // Whether the group's text scrolls, a page at a time
        public static bool HasLines(short groupType)
        {
            return groupType == _information_group || groupType == _pict_group || groupType == _checkpoint_group;
        }

        // Whether the group shows its text (the logon and logoff groups show one line of it)
        public static bool HasText(short groupType)
        {
            return HasLines(groupType) || groupType == _logon_group || groupType == _logoff_group;
        }

        // Whether Aleph One reads the group's permutation (the section markers and the end don't, nor does
        // information, which only shows its text)
        public static bool UsesPermutation(short groupType)
        {
            switch (groupType)
            {
                case _unfinished_group:
                case _success_group:
                case _failure_group:
                case _end_group:
                case _information_group:
                    return false;
                default:
                    return true;
            }
        }

        // The group type as terminal source writes it (#LOGON, #INTERLEVEL TELEPORT)
        public static string GetDirectiveName(short groupType)
        {
            return "#" + AlephOneNames.TerminalGroupType(groupType).ToUpperInvariant();
        }

        // The group as terminal source writes it (#LOGON 1600, #SOUND 12), with its permutation where it uses one
        public static string GetDirective(terminal_groupings group)
        {
            var directiveName = GetDirectiveName(group.type);

            return UsesPermutation(group.type) ? $"{directiveName} {group.permutation}" : directiveName;
        }

        // As goto_terminal_group calculates it for a single player
        private static short GetMaximumLine(terminal_text_t terminal, terminal_groupings group)
        {
            switch (group.type)
            {
                case _checkpoint_group:
                case _pict_group:
                {
                    var textBounds = new Rect();
                    calculate_bounds_for_text_box(group.flags, textBounds);
                    var maximumLine = count_total_lines(get_text_base(terminal), RECTANGLE_WIDTH(textBounds), group.start_index, (short) (group.start_index + group.length));

                    if (film_profile.page_up_past_full_width_term_pict && maximumLine == 0)
                    {
                        maximumLine = 1;
                    }

                    return maximumLine;
                }
                case _information_group:
                {
                    var bounds = get_term_rectangle(_terminal_full_text_rect);
                    return count_total_lines(get_text_base(terminal), RECTANGLE_WIDTH(bounds), group.start_index, (short) (group.start_index + group.length));
                }
                case _end_group:
                    return 1;
                case _unfinished_group:
                case _success_group:
                case _failure_group:
                    return 0;
                default:
                    return group.maximum_line_count;
            }
        }

        public static Rect GetTextBounds(terminal_groupings group)
        {
            if (group.type == _information_group)
            {
                return get_term_rectangle(_terminal_full_text_rect);
            }

            var bounds = new Rect();
            calculate_bounds_for_text_box(group.flags, bounds);

            return bounds;
        }

        // Where a group with these flags draws its object (its picture or map), without a picture to fit
        public static Rect GetObjectBounds(short flags)
        {
            var bounds = new Rect();
            calculate_bounds_for_object(flags, bounds, null);

            return bounds;
        }

        // Where display_picture draws a picture, and whether it's drawn unscaled from there (Aleph One's cinemascope hack)
        public static Rect GetPictureBounds(ScenarioPictures.Picture picture, short flags, out bool isUnscaled)
        {
            isUnscaled = false;

            var bounds = new Rect
            {
                right = (short) picture.Texture.width,
                bottom = (short) picture.Texture.height,
            };

            if (bounds.right != picture.HeaderWidth && bounds.right == 614)
            {
                isUnscaled = true;
                bounds.right = (short) picture.HeaderWidth;
            }

            var screenBounds = new Rect();
            calculate_bounds_for_object(flags, screenBounds, bounds);

            if (RECTANGLE_WIDTH(bounds) <= RECTANGLE_WIDTH(screenBounds) &&
                RECTANGLE_HEIGHT(bounds) <= RECTANGLE_HEIGHT(screenBounds))
            {
                /* It fits-> center it. */
                OffsetRect(bounds, screenBounds.left + (RECTANGLE_WIDTH(screenBounds) - RECTANGLE_WIDTH(bounds)) / 2,
                    screenBounds.top + (RECTANGLE_HEIGHT(screenBounds) - RECTANGLE_HEIGHT(bounds)) / 2);
            }
            else if (RECTANGLE_HEIGHT(bounds) - RECTANGLE_HEIGHT(screenBounds) >= RECTANGLE_WIDTH(bounds) - RECTANGLE_WIDTH(screenBounds))
            {
                /* Doesn't fit.  Make it, but preserve the aspect ratio */
                var adjustedWidth = RECTANGLE_HEIGHT(screenBounds) * RECTANGLE_WIDTH(bounds) / RECTANGLE_HEIGHT(bounds);
                bounds = screenBounds.Clone();
                InsetRect(bounds, (RECTANGLE_WIDTH(screenBounds) - adjustedWidth) / 2, 0);
            }
            else
            {
                /* Width is the predominant factor */
                var adjustedHeight = RECTANGLE_WIDTH(screenBounds) * RECTANGLE_HEIGHT(bounds) / RECTANGLE_WIDTH(bounds);
                bounds = screenBounds.Clone();
                InsetRect(bounds, 0, (RECTANGLE_HEIGHT(screenBounds) - adjustedHeight) / 2);
            }

            return bounds;
        }

        // Where draw_logon_text draws the logon or logoff line, below the logo's frame
        public static Rect GetLogonTextBounds(terminal_text_t terminal, terminal_groupings group, Rect pictureFrame)
        {
            var graphicBounds = get_term_rectangle(_terminal_logon_graphic_rect);
            var bounds = new Rect
            {
                top = pictureFrame.bottom,
                bottom = graphicBounds.bottom,
                left = graphicBounds.left,
                right = graphicBounds.right,
            };

            var printableLength = 0;
            for (var i = group.start_index; i < group.start_index + group.length; i++)
            {
                if (text_at(terminal.text, i) >= ' ')
                {
                    printableLength++;
                }
            }

            bounds.left += (short) ((RECTANGLE_WIDTH(bounds) - printableLength * terminal_font_character_width) / 2);

            return bounds;
        }

        // The page's lines, as _draw_computer_text breaks them and draw_line splits them at face changes
        public static List<Line> GetLines(terminal_text_t terminal, terminal_groupings group, Rect bounds, short currentLine, short linesPerPage)
        {
            var lines = new List<Line>();
            var baseText = get_text_base(terminal);
            var width = RECTANGLE_WIDTH(bounds);
            var groupEnd = (short) (group.start_index + group.length);
            var done = false;

            var startIndex = group.start_index;
            short endIndex;

            /* eat the previous lines */
            for (var index = 0; index < currentLine; ++index)
            {
                if (!calculate_line(baseText, width, startIndex, groupEnd, out endIndex))
                {
                    startIndex = endIndex > groupEnd ? groupEnd : endIndex;
                }
                else
                {
                    done = true;
                }
            }

            if (done)
            {
                return lines;
            }

            /* Go backwards, and see if there were any other face changes... */
            var lastIndex = group.start_index;
            short lastTextIndex = NONE;
            for (short textIndex = 0; textIndex < terminal.font_changes.Count; ++textIndex)
            {
                var fontFace = terminal.font_changes[textIndex];

                if (fontFace.index > lastIndex && fontFace.index < startIndex)
                {
                    lastIndex = fontFace.index;
                    lastTextIndex = textIndex;
                }
            }

            var face = lastTextIndex == NONE ? new text_face_data() : terminal.font_changes[lastTextIndex].Clone();

            /* Draw what is one the screen */
            for (var index = 0; index < linesPerPage; ++index)
            {
                if (!calculate_line(baseText, width, startIndex, groupEnd, out endIndex))
                {
                    if (endIndex > groupEnd)
                    {
                        endIndex = groupEnd;
                    }

                    lines.Add(GetLine(terminal, startIndex, endIndex, ref lastTextIndex, ref face, index));
                    startIndex = endIndex;
                }
            }

            return lines;
        }

        // draw_line: a run for each face, the face carrying on from line to line
        private static Line GetLine(terminal_text_t terminal, short startIndex, short endIndex, ref short textFaceStartIndex, ref text_face_data face, int lineNumber)
        {
            var line = new Line { Number = lineNumber, StartIndex = startIndex, EndIndex = endIndex };
            var faceCount = terminal.font_changes.Count;
            var textIndex = textFaceStartIndex == NONE ? 0 : textFaceStartIndex;
            text_face_data faceData = null;

            /* Get to the first one that concerns us.. */
            if (textIndex < faceCount)
            {
                do
                {
                    faceData = terminal.font_changes[textIndex];
                    if (faceData.index < startIndex) textIndex++;
                }
                while (faceData.index < startIndex && textIndex < faceCount);
            }

            var currentStart = startIndex;
            var currentEnd = endIndex;
            var column = 0;

            while (true)
            {
                if (textIndex < faceCount)
                {
                    faceData = terminal.font_changes[textIndex];

                    if (faceData.index >= currentStart && faceData.index < currentEnd)
                    {
                        currentEnd = faceData.index;
                        textIndex++;
                        textFaceStartIndex = (short) textIndex;
                    }
                }

                var text = TerminalText.Decode(terminal.text, currentStart, currentEnd);
                if (text.Length > 0)
                {
                    line.Runs.Add(new Run { Text = text, Column = column, Face = face.face, Color = face.color });
                }

                column += text.Length;

                if (currentEnd != endIndex)
                {
                    currentStart = currentEnd;
                    currentEnd = endIndex;
                    face = faceData.Clone();
                }
                else
                {
                    break;
                }
            }

            return line;
        }
    }
}
