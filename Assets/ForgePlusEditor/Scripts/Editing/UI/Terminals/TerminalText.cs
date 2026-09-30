using AlephOne;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static AlephOne.computer_interface;
using static AlephOne.screen_drawing;

namespace ForgePlus.UI
{
    // A terminal's text as Aleph One draws it: its characters (Mac Roman), and its faces and colors (set_text_face's,
    // which follow the terminal's text color)
    public static class TerminalText
    {
        public const short ColorCount = _computer_interface_color_blue - _computer_interface_text_color + 1;

        // The printable characters, with the line ends as line breaks where they're kept
        public static string Decode(List<byte> text, int startIndex, int endIndex, bool keepLineEnds = false)
        {
            var builder = new StringBuilder(endIndex - startIndex);

            for (var i = startIndex; i < endIndex; i++)
            {
                var c = text_at(text, i);
                if (c == MAC_LINE_END && keepLineEnds)
                {
                    builder.Append('\n');
                }
                else if (c >= ' ')
                {
                    builder.Append((char) csstrings.mac_roman_to_unicode(c));
                }
            }

            return builder.ToString();
        }

        // Text in a face and color: Aleph One's terminal font (Courier Prime) in the face's bold and italic, underlined
        // with a line as thick as the scale
        public static void ApplyStyle(VisualElement text, short face, short color, float scale)
        {
            text.style.unityFontDefinition = FontDefinition.FromFont(Resources.Load<Font>($"UI/Fonts/{GetFontName(face)}"));

            var textColor = GetColor(color);
            text.style.color = textColor;

            if ((face & _underline_text) != 0)
            {
                text.style.borderBottomWidth = Mathf.Max(1f, scale);
                text.style.borderBottomColor = textColor;
            }
        }

        private static Color GetColor(short color)
        {
            var index = _computer_interface_text_color + color;
            if (index < _computer_interface_text_color || index >= NumInterfaceColors)
            {
                index = _computer_interface_text_color;
            }

            var rgb = get_interface_color((short) index);
            return new Color(rgb.red / 65535f, rgb.green / 65535f, rgb.blue / 65535f);
        }

        private static string GetFontName(short face)
        {
            switch (face & (_bold_text | _italic_text))
            {
                case _bold_text:
                    return "CourierPrimeBold";
                case _italic_text:
                    return "CourierPrimeItalic";
                case _bold_text | _italic_text:
                    return "CourierPrimeBoldItalic";
                default:
                    return "CourierPrime";
            }
        }
    }
}
