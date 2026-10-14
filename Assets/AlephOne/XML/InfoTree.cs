// Port of Aleph One: Source_Files/XML/InfoTree.h, InfoTree.cpp (reading XML)
//
// Aleph One's InfoTree is a boost::property_tree::iptree, so element and attribute names are matched ignoring case, and
// values are read as boost's stream translator reads them (the whole value, apart from surrounding whitespace, must be a
// value of the type; a bool is 0, 1, true or false). Its XML is read as boost's parser (rapidxml) reads it, which is
// more forgiving than .NET's: an attribute can be repeated (the first is read), and closing tags aren't checked against
// the elements they close.
//
// Not ported: writing XML, INI files, colors and fonts.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using static AlephOne.cstypes;
using static AlephOne.shape_descriptors;

namespace AlephOne
{
    public class InfoTree
    {
        // Thrown where boost::property_tree throws its parse_error, path_error or data_error
        public class parse_error : Exception
        {
            public parse_error(string message, Exception inner = null) : base(message, inner) { }
        }

        public class path_error : Exception
        {
            public path_error(string message) : base(message) { }
        }

        private readonly string elementName;
        private readonly List<(string Name, string Value)> attributes = new List<(string Name, string Value)>();
        private readonly List<InfoTree> childElements = new List<InfoTree>();
        private readonly StringBuilder text = new StringBuilder();

        // A document has no name, and its root elements as its children
        private InfoTree(string elementName)
        {
            this.elementName = elementName;
        }

        // The element's name (empty for a document)
        public string name
        {
            get
            {
                return elementName;
            }
        }

        public static InfoTree load_xml(FileSpecifier filename)
        {
            byte[] data;
            try
            {
                data = File.ReadAllBytes(filename.GetPath());
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                throw new parse_error($"couldn't open '{filename.GetPath()}' for reading", exception);
            }

            return load_xml(data, data.Length);
        }

        // Aleph One reads from a std::istringstream of the bytes; NULs at the end (as text resources and chunks can have)
        // aren't part of the XML
        public static InfoTree load_xml(byte[] buffer, int buflen)
        {
            var length = Math.Min(buflen, buffer.Length);
            while (length > 0 && buffer[length - 1] == 0)
            {
                length--;
            }

            return new XmlReader(Encoding.UTF8.GetString(buffer, 0, length)).ReadDocument();
        }

        public InfoTree get_child(string path)
        {
            var child = children_named(path).FirstOrDefault();
            if (child == null)
            {
                throw new path_error($"No such node ({path})");
            }

            return child;
        }

        public IEnumerable<InfoTree> children_named(string key)
        {
            return childElements.Where(child => string.Equals(child.name, key, StringComparison.OrdinalIgnoreCase));
        }

        // Every child, in order (for (const InfoTree::value_type &v : root))
        public IEnumerable<InfoTree> children()
        {
            return childElements;
        }

        // The element's text (get_value)
        public string get_value(string default_value)
        {
            return text.Length > 0 ? text.ToString() : default_value;
        }

        public bool read_attr_text(string path, out string value)
        {
            foreach (var attribute in attributes)
            {
                if (string.Equals(attribute.Name, path, StringComparison.OrdinalIgnoreCase))
                {
                    value = attribute.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool read_attr(string path, ref string value)
        {
            if (read_attr_text(path, out string temp))
            {
                value = temp;
                return true;
            }

            return false;
        }

        public bool read_attr(string path, ref short value)
        {
            if (read_attr_text(path, out string text) && TryParseInteger(text, short.MinValue, short.MaxValue, out var temp))
            {
                value = (short) temp;
                return true;
            }

            return false;
        }

        public bool read_attr(string path, ref ushort value)
        {
            if (read_attr_text(path, out string text) && TryParseInteger(text, ushort.MinValue, ushort.MaxValue, out var temp))
            {
                value = (ushort) temp;
                return true;
            }

            return false;
        }

        public bool read_attr(string path, ref int value)
        {
            if (read_attr_text(path, out string text) && TryParseInteger(text, int.MinValue, int.MaxValue, out var temp))
            {
                value = (int) temp;
                return true;
            }

            return false;
        }

        public bool read_attr(string path, ref float value)
        {
            if (read_attr_text(path, out string text) &&
                float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var temp))
            {
                value = temp;
                return true;
            }

            return false;
        }

        public bool read_attr(string path, ref bool value)
        {
            if (!read_attr_text(path, out string text))
            {
                return false;
            }

            switch (text.Trim())
            {
                case "1":
                case "true":
                    value = true;
                    return true;
                case "0":
                case "false":
                    value = false;
                    return true;
                default:
                    return false;
            }
        }

        public bool read_attr_bounded(string path, ref short value, short min, short max)
        {
            short temp = 0;
            if (read_attr(path, ref temp) && temp >= min && temp <= max)
            {
                value = temp;
                return true;
            }

            return false;
        }

        public bool read_attr_bounded(string path, ref ushort value, ushort min, ushort max)
        {
            ushort temp = 0;
            if (read_attr(path, ref temp) && temp >= min && temp <= max)
            {
                value = temp;
                return true;
            }

            return false;
        }

        public bool read_attr_bounded(string path, ref float value, float min, float max)
        {
            float temp = 0;
            if (read_attr(path, ref temp) && temp >= min && temp <= max)
            {
                value = temp;
                return true;
            }

            return false;
        }

        public bool read_indexed(string path, ref short value, int num_slots, bool allow_none = false)
        {
            short temp = 0;
            if (read_attr(path, ref temp) && temp >= (allow_none ? NONE : 0) && temp <= num_slots - 1)
            {
                value = temp;
                return true;
            }

            return false;
        }

        public bool read_fixed(string path, ref int value, float min = -short.MaxValue, float max = short.MaxValue)
        {
            float temp = 0;
            if (read_attr_bounded(path, ref temp, min, max))
            {
                value = (int) (FIXED_ONE * temp + 0.5);
                return true;
            }

            return false;
        }

        // Resolved now, against the search path (which a plugin's MML has its folder at the front of)
        public bool read_path(string key, FileSpecifier file)
        {
            if (read_attr_text(key, out string path))
            {
                file.SetNameWithPath(path);
                return true;
            }

            return false;
        }

        public bool read_shape(ref ushort descriptor, bool allow_empty = true)
        {
            ushort seq = UNONE;
            var seq_present = read_attr_bounded("seq", ref seq, 0, MAXIMUM_SHAPES_PER_COLLECTION - 1);
            if (!seq_present)
            {
                seq_present = read_attr_bounded("frame", ref seq, 0, MAXIMUM_SHAPES_PER_COLLECTION - 1);
            }

            ushort coll = UNONE;
            var coll_present = read_attr_bounded("coll", ref coll, 0, MAXIMUM_COLLECTIONS - 1);
            ushort clut = 0;
            read_attr_bounded("clut", ref clut, 0, MAXIMUM_CLUTS_PER_COLLECTION - 1);

            if (coll_present && seq_present)
            {
                descriptor = BUILD_DESCRIPTOR(BUILD_COLLECTION(coll, clut), seq);
                return true;
            }
            else if (!coll_present && !seq_present && allow_empty)
            {
                descriptor = UNONE;
                return true;
            }

            return false;
        }

        public bool read_damage(damage_definition def)
        {
            var status = false;
            if (read_indexed("type", ref def.type, map.NUMBER_OF_DAMAGE_TYPES, true))
                status = true;
            if (read_indexed("flags", ref def.flags, 2))
                status = true;
            if (read_attr("base", ref def.@base))
                status = true;
            if (read_attr("random", ref def.random))
                status = true;
            if (read_fixed("scale", ref def.scale))
                status = true;
            return status;
        }

        // As a stream extracts an integer: an optional sign, then decimal digits, with whitespace around it
        private static bool TryParseInteger(string text, long min, long max, out long value)
        {
            return long.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value) &&
                   value >= min && value <= max;
        }

        // ForgePlus: reads XML as boost::property_tree's read_xml (rapidxml) does: elements, their attributes (every one,
        // in order, so a repeated one is kept too) and their text (with entities and CDATA), skipping declarations,
        // processing instructions, comments and DOCTYPEs. Closing tags close the open element, whatever they're named.
        private sealed class XmlReader
        {
            private readonly string xml;
            private int position;

            public XmlReader(string xml)
            {
                this.xml = xml;

                // A byte order mark isn't part of the XML
                position = xml.Length > 0 && xml[0] == '﻿' ? 1 : 0;
            }

            public InfoTree ReadDocument()
            {
                var document = new InfoTree(string.Empty);
                var open = new Stack<InfoTree>();
                open.Push(document);

                while (position < xml.Length)
                {
                    var current = open.Peek();

                    if (xml[position] != '<')
                    {
                        var end = xml.IndexOf('<', position);
                        if (end < 0)
                        {
                            end = xml.Length;
                        }

                        if (current != document)
                        {
                            current.text.Append(DecodeEntities(xml.Substring(position, end - position)));
                        }

                        position = end;
                    }
                    else if (StartsWith("<!--"))
                    {
                        SkipPast("-->");
                    }
                    else if (StartsWith("<![CDATA["))
                    {
                        var start = position + 9;
                        SkipPast("]]>");
                        if (current != document)
                        {
                            current.text.Append(xml, start, position - 3 - start);
                        }
                    }
                    else if (StartsWith("<?"))
                    {
                        SkipPast("?>");
                    }
                    else if (StartsWith("<!"))
                    {
                        SkipDeclaration();
                    }
                    else if (StartsWith("</"))
                    {
                        SkipPast(">");
                        if (open.Count == 1)
                        {
                            throw new parse_error($"unexpected closing tag at {position}");
                        }

                        open.Pop();
                    }
                    else
                    {
                        position++;
                        var element = new InfoTree(ReadName());
                        if (element.name.Length == 0)
                        {
                            throw new parse_error($"expected element name at {position}");
                        }

                        current.childElements.Add(element);

                        if (!ReadAttributes(element))
                        {
                            open.Push(element);
                        }
                    }
                }

                if (open.Count != 1)
                {
                    throw new parse_error("unexpected end of data");
                }

                return document;
            }

            // Returns whether the element closed itself (with "/>")
            private bool ReadAttributes(InfoTree element)
            {
                while (true)
                {
                    SkipWhitespace();
                    if (position >= xml.Length)
                    {
                        throw new parse_error("unexpected end of data");
                    }

                    if (xml[position] == '>')
                    {
                        position++;
                        return false;
                    }

                    if (StartsWith("/>"))
                    {
                        position += 2;
                        return true;
                    }

                    var attributeName = ReadName();
                    if (attributeName.Length == 0)
                    {
                        throw new parse_error($"expected attribute name at {position}");
                    }

                    SkipWhitespace();
                    if (position >= xml.Length || xml[position] != '=')
                    {
                        throw new parse_error($"expected = at {position}");
                    }

                    position++;
                    SkipWhitespace();
                    if (position >= xml.Length || (xml[position] != '"' && xml[position] != '\''))
                    {
                        throw new parse_error($"expected ' or \" at {position}");
                    }

                    var quote = xml[position++];
                    var end = xml.IndexOf(quote, position);
                    if (end < 0)
                    {
                        throw new parse_error("unexpected end of data");
                    }

                    element.attributes.Add((attributeName, DecodeEntities(xml.Substring(position, end - position))));
                    position = end + 1;
                }
            }

            private string ReadName()
            {
                var start = position;
                while (position < xml.Length && !char.IsWhiteSpace(xml[position]) && xml[position] != '>' && xml[position] != '/' &&
                       xml[position] != '=' && xml[position] != '<' && xml[position] != '"' && xml[position] != '\'')
                {
                    position++;
                }

                return xml.Substring(start, position - start);
            }

            // A DOCTYPE, with any bracketed internal subset
            private void SkipDeclaration()
            {
                var depth = 0;
                while (position < xml.Length)
                {
                    var character = xml[position++];
                    if (character == '[')
                    {
                        depth++;
                    }
                    else if (character == ']')
                    {
                        depth--;
                    }
                    else if (character == '>' && depth <= 0)
                    {
                        return;
                    }
                }

                throw new parse_error("unexpected end of data");
            }

            private bool StartsWith(string value)
            {
                return string.CompareOrdinal(xml, position, value, 0, value.Length) == 0;
            }

            private void SkipPast(string value)
            {
                var end = xml.IndexOf(value, position, StringComparison.Ordinal);
                if (end < 0)
                {
                    throw new parse_error("unexpected end of data");
                }

                position = end + value.Length;
            }

            private void SkipWhitespace()
            {
                while (position < xml.Length && char.IsWhiteSpace(xml[position]))
                {
                    position++;
                }
            }

            // Null for numbers that aren't characters
            private static string CodePoint(int number)
            {
                return number >= 0 && number <= 0x10FFFF && (number < 0xD800 || number > 0xDFFF) ? char.ConvertFromUtf32(number) : null;
            }

            // The predefined entities and character references (others are left as they are, as rapidxml leaves them)
            private static string DecodeEntities(string value)
            {
                if (value.IndexOf('&') < 0)
                {
                    return value;
                }

                var decoded = new StringBuilder(value.Length);
                for (var i = 0; i < value.Length; i++)
                {
                    if (value[i] == '&')
                    {
                        var end = value.IndexOf(';', i);
                        if (end > i)
                        {
                            var entity = value.Substring(i + 1, end - i - 1);
                            string replacement = null;
                            switch (entity)
                            {
                                case "lt": replacement = "<"; break;
                                case "gt": replacement = ">"; break;
                                case "amp": replacement = "&"; break;
                                case "quot": replacement = "\""; break;
                                case "apos": replacement = "'"; break;
                                default:
                                    if (entity.StartsWith("#x") &&
                                        int.TryParse(entity.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                                    {
                                        replacement = CodePoint(hex);
                                    }
                                    else if (entity.StartsWith("#") &&
                                             int.TryParse(entity.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                                    {
                                        replacement = CodePoint(number);
                                    }

                                    break;
                            }

                            if (replacement != null)
                            {
                                decoded.Append(replacement);
                                i = end;
                                continue;
                            }
                        }
                    }

                    decoded.Append(value[i]);
                }

                return decoded.ToString();
            }
        }
    }
}
