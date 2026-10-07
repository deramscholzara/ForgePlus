using AlephOne;
using ForgePlus.Localization;
using System.Collections.Generic;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;
using static AlephOne.computer_interface;
using static AlephOne.screen_drawing;

namespace ForgePlus.UI
{
    // A terminal's text as Aleph One draws it: its characters (Mac Roman), and its faces and colors (set_text_face's,
    // which follow the terminal's text color)
    [NoAutoStaticsCleanup]
    public static class TerminalText
    {
        public const short ColorCount = _computer_interface_color_blue - _computer_interface_text_color + 1;

        // The faces (and plain), then the colors, as the styles panel and the group's style buttons offer them
        public static readonly IReadOnlyList<Style> Styles = CreateStyles();

        // Applied as its kind and value, and shown with its key (as its terminal source code) and name, in its face and
        // color
        public class Style
        {
            public TerminalSource.StyleKind Kind;
            public short Value;
            public short Face;
            public short ColorIndex;
            public string Key;
            public string NameKey;

            public string Name
            {
                get
                {
                    return Kind == TerminalSource.StyleKind.Color ? Strings.Get(Strings.Terminals, "Terminals.Styles.Color", ColorIndex) : Strings.Get(Strings.Terminals, NameKey);
                }
            }
        }

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

        private static List<Style> CreateStyles()
        {
            var styles = new List<Style>
            {
                new Style { Kind = TerminalSource.StyleKind.Plain, Value = _plain_text, Face = _plain_text, Key = "-", NameKey = "Terminals.Styles.Plain" },
                new Style { Kind = TerminalSource.StyleKind.Face, Value = _bold_text, Face = _bold_text, Key = "B", NameKey = "Terminals.Styles.Bold" },
                new Style { Kind = TerminalSource.StyleKind.Face, Value = _italic_text, Face = _italic_text, Key = "I", NameKey = "Terminals.Styles.Italic" },
                new Style { Kind = TerminalSource.StyleKind.Face, Value = _underline_text, Face = _underline_text, Key = "U", NameKey = "Terminals.Styles.Underline" },
            };

            for (short color = 0; color < ColorCount; color++)
            {
                styles.Add(new Style { Kind = TerminalSource.StyleKind.Color, Value = color, Face = _plain_text, ColorIndex = color, Key = color.ToString() });
            }

            return styles;
        }
    }
}
