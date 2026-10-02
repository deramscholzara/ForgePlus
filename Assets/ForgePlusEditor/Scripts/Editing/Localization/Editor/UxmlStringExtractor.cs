using ForgePlus.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Localization;
using UnityEditor;
using UnityEngine;

namespace ForgePlus.Localization.Editor
{
    // Moves the display text written into the UI layouts (text, tooltip and label attributes) into the string tables:
    // each becomes an entry of the table for the part of ForgePlus that shows it (see TableFor), and the attribute
    // becomes its key ("@Table/Key"). Text that's already a key, and templates' placeholders (which their instances
    // always replace), are left as they are, so it can be run again for new layouts or text.
    public static class UxmlStringExtractor
    {
        private const string LayoutsFolder = "Assets/ForgePlusEditor/Resources/UI";
        private const string LocaleCode = "en-US";

        // Texts in templates that each instance replaces (so they're never shown)
        private static readonly HashSet<string> TemplatePlaceholders = new HashSet<string>
        {
            "Button", "Label:", "Label", "Choice:", "Option", "Flag", "Header", "Subheader", "value", "Texture:",
            "Level Name", "Type", "00", "Sound", "C: 00 B: 00", "Toggle",
        };

        private static readonly Regex InstanceOpen = new Regex(@"<ui:Instance\b[^>]*?\bname=""(?<name>[^""]+)""[^>]*?(?<selfClosing>/?)>");
        private static readonly Regex InstanceClose = new Regex(@"</ui:Instance>");
        private static readonly Regex Element = new Regex(@"<(?<tag>[\w:.]+)(?<attributes>(?:\s+[\w:-]+=""[^""]*"")*)\s*/?>");
        private static readonly Regex Attribute = new Regex(@"(?<attribute>[\w:-]+)=""(?<value>[^""]*)""");

        [MenuItem("Tools/ForgePlus/Localization/Move Layout Text to String Tables")]
        public static void MoveLayoutTextToTables()
        {
            var report = Extract(dryRun: false);
            Debug.Log(report);
        }

        // Which table a layout's text goes in, by the layout's path under Resources/UI
        public static string TableFor(string layoutPath)
        {
            var name = Path.GetFileNameWithoutExtension(layoutPath);
            var folder = Path.GetFileName(Path.GetDirectoryName(layoutPath));

            switch (folder)
            {
                case "Inspectors":
                    switch (name)
                    {
                        case "Inspector - Side":
                        case "Inspector - Line":
                            return "Geometry";
                        case "Inspector - Side Textures":
                        case "Inspector - Polygon Textures":
                            return "Textures";
                        case "Inspector - Light":
                            return "Lights";
                        case "Inspector - Media":
                            return "Media";
                        case "Inspector - Platform":
                            return "Platforms";
                        case "Inspector - Placement":
                        case "Inspector - Placements":
                            return "Objects";
                        case "Inspector - Ambient Sound":
                        case "Inspector - Random Sound":
                            return "Sounds";
                        case "Inspector - Annotation":
                            return "Annotations";
                        case "Inspector - Level":
                            return "Level";
                        case "Inspector - Map":
                            return "Map";
                        default:
                            // Polygon (Geometry and Sounds modes), MapObject (Objects and Sounds), Placeholder Side
                            // (Geometry and Textures)
                            return Strings.Common;
                    }
                case "Panels":
                    switch (name)
                    {
                        case "Menu":
                        case "FilesTab":
                        case "InputsTab":
                        case "SettingsTab":
                            return Strings.Menu;
                        case "VisualizationGeometry":
                            return "Geometry";
                        case "VisualizationObjects":
                            return "Objects";
                        case "SoundPalette":
                            return "Sounds";
                        case "Annotations":
                            return "Annotations";
                        case "Map":
                            return "Map";
                        case "Terminal":
                        case "TerminalDetails":
                        case "TerminalStyles":
                            return "Terminals";
                        default:
                            return Strings.Common;
                    }
                case "Templates":
                    switch (name)
                    {
                        case "LevelItem":
                        case "PathSelector":
                            return Strings.Menu;
                        case "SwatchSound":
                            return "Sounds";
                        default:
                            return Strings.Common;
                    }
                default:
                    return Strings.Common;
            }
        }

        // Returns what was (or, for a dry run, would be) moved
        public static string Extract(bool dryRun)
        {
            var tables = new Dictionary<string, ResourceTable>();
            var report = new StringBuilder();
            var entryCount = 0;
            var fileCount = 0;

            foreach (var layoutPath in Directory.GetFiles(LayoutsFolder, "*.uxml", SearchOption.AllDirectories).Select(path => path.Replace('\\', '/')).OrderBy(path => path))
            {
                var tableName = TableFor(layoutPath);
                var isTemplate = layoutPath.Contains("/Templates/");
                var context = ContextFor(layoutPath);

                var lines = File.ReadAllText(layoutPath).Split('\n');
                var instanceNames = new Stack<string>();
                var changed = false;

                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    var instanceOpen = InstanceOpen.Match(line);
                    var lineInstanceName = instanceOpen.Success ? instanceOpen.Groups["name"].Value : null;

                    line = Element.Replace(line, elementMatch =>
                    {
                        var tag = elementMatch.Groups["tag"].Value;
                        var attributes = elementMatch.Groups["attributes"].Value;
                        if (attributes.Length == 0)
                        {
                            return elementMatch.Value;
                        }

                        var elementName = AttributeValue(attributes, "name");
                        var overriddenElement = tag == "ui:AttributeOverrides" ? AttributeValue(attributes, "element-name") : null;
                        var owner = tag == "ui:Instance" ? lineInstanceName : (instanceNames.Count > 0 ? instanceNames.Peek() : null);

                        var newAttributes = Attribute.Replace(attributes, attributeMatch =>
                        {
                            var attribute = attributeMatch.Groups["attribute"].Value;
                            var rawValue = attributeMatch.Groups["value"].Value;
                            if (attribute != "text" && attribute != "tooltip" && attribute != "label")
                            {
                                return attributeMatch.Value;
                            }

                            var value = WebUtility.HtmlDecode(rawValue);
                            if (!IsDisplayText(value) || (isTemplate && TemplatePlaceholders.Contains(value)))
                            {
                                return attributeMatch.Value;
                            }

                            var keyParts = new List<string> { context };
                            if (tag == "ui:AttributeOverrides")
                            {
                                keyParts.Add(owner != null ? Pascal(owner) : Slug(value));
                                keyParts.Add(Pascal(overriddenElement ?? "Element"));
                            }
                            else if (tag == "ui:Instance")
                            {
                                keyParts.Add(owner != null ? Pascal(owner) : Slug(value));
                            }
                            else if (!string.IsNullOrEmpty(elementName))
                            {
                                if (owner != null)
                                {
                                    keyParts.Add(Pascal(owner));
                                }

                                keyParts.Add(Pascal(elementName));
                            }
                            else
                            {
                                if (owner != null)
                                {
                                    keyParts.Add(Pascal(owner));
                                }

                                keyParts.Add(Slug(value));
                            }

                            keyParts.Add(Pascal(attribute));

                            var table = dryRun ? null : GetTable(tables, tableName);
                            var key = UniqueKey(tables, tableName, string.Join(".", keyParts), value, dryRun);

                            if (!dryRun)
                            {
                                if (table == null)
                                {
                                    return attributeMatch.Value;
                                }

                                table.AddStringEntry(key, value, false);
                                EditorUtility.SetDirty(table);
                                EditorUtility.SetDirty(table.SharedData);
                            }

                            entryCount++;
                            changed = true;
                            report.AppendLine($"{tableName}: {key} = {value}");

                            return $"{attribute}=\"@{tableName}/{key}\"";
                        });

                        return elementMatch.Value.Replace(attributes, newAttributes);
                    });

                    lines[i] = line;

                    if (instanceOpen.Success && instanceOpen.Groups["selfClosing"].Value != "/")
                    {
                        instanceNames.Push(lineInstanceName);
                    }

                    if (InstanceClose.IsMatch(line) && instanceNames.Count > 0)
                    {
                        instanceNames.Pop();
                    }
                }

                if (changed)
                {
                    fileCount++;

                    if (!dryRun)
                    {
                        File.WriteAllText(layoutPath, string.Join("\n", lines), new UTF8Encoding(false));
                    }
                }
            }

            if (!dryRun)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return $"{(dryRun ? "Would move" : "Moved")} {entryCount} texts from {fileCount} layouts:\n{report}";
        }

        // "Inspectors/Inspector - Polygon Textures.uxml" is "Inspector.PolygonTextures"; "Panels/FilesTab.uxml" is "FilesTab"
        private static string ContextFor(string layoutPath)
        {
            var name = Path.GetFileNameWithoutExtension(layoutPath);
            if (name.StartsWith("Inspector - ", StringComparison.Ordinal))
            {
                return "Inspector." + Pascal(name.Substring("Inspector - ".Length));
            }

            return Pascal(name);
        }

        private static bool IsDisplayText(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value[0] != '@' &&
                   value.Any(char.IsLetter);
        }

        private static string AttributeValue(string attributes, string attribute)
        {
            var match = Regex.Match(attributes, $@"\b{Regex.Escape(attribute)}=""(?<value>[^""]*)""");
            return match.Success ? match.Groups["value"].Value : null;
        }

        // "sound-direction" or "Sound Direction" is "SoundDirection"
        private static string Pascal(string text)
        {
            var words = Regex.Split(text, @"[^A-Za-z0-9]+").Where(word => word.Length > 0);
            return string.Concat(words.Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));
        }

        // The first few words of a text, as a key part
        private static string Slug(string text)
        {
            var words = Regex.Split(text, @"[^A-Za-z0-9]+").Where(word => word.Length > 0).Take(4);
            var slug = string.Concat(words.Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));
            return slug.Length > 0 ? slug : "Text";
        }

        // The key, or (if the table already has it for different text) the key with a number after it
        private static string UniqueKey(Dictionary<string, ResourceTable> tables, string tableName, string key, string value, bool dryRun)
        {
            var table = GetTable(tables, tableName);
            if (table == null)
            {
                return key;
            }

            var candidate = key;
            for (var number = 2; ; number++)
            {
                var existing = table.GetEntry<StringEntry>(candidate);
                if (existing == null || existing.Value == value)
                {
                    return candidate;
                }

                candidate = $"{key}{number}";
            }
        }

        private static ResourceTable GetTable(Dictionary<string, ResourceTable> tables, string tableName)
        {
            if (!tables.TryGetValue(tableName, out var table))
            {
                table = FindTable(tableName);
                if (table == null)
                {
                    Debug.LogError($"There's no {LocaleCode} table for the \"{tableName}\" string table collection.");
                }

                tables[tableName] = table;
            }

            return table;
        }

        public static ResourceTable FindTable(string tableName)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ResourceTable"))
            {
                var table = AssetDatabase.LoadAssetAtPath<ResourceTable>(AssetDatabase.GUIDToAssetPath(guid));
                if (table != null && table.TableCollectionName == tableName && table.LocaleIdentifier.Code == LocaleCode)
                {
                    return table;
                }
            }

            return null;
        }
    }
}
