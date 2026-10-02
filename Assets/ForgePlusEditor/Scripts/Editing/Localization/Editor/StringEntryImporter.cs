using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Localization;
using UnityEditor;
using UnityEngine;

namespace ForgePlus.Localization.Editor
{
    // Adds string entries to the en-US tables from a JSON-lines file, one entry a line:
    // {"table": "Common", "key": "Some.Key", "value": "Some text", "smart": false}
    // An entry whose value has {0}-style placeholders is a Smart String (formatted with Strings.Get's arguments) unless
    // it says otherwise. An entry the table already has is updated.
    public static class StringEntryImporter
    {
        private static readonly Regex Placeholder = new Regex(@"\{\d+(:[^}]*)?\}");

        public static string Import(string manifestPath)
        {
            var tables = new Dictionary<string, ResourceTable>();
            var added = 0;
            var problems = new List<string>();

            var lineNumber = 0;
            foreach (var line in File.ReadAllLines(manifestPath))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                StringEntryLine entry;
                try
                {
                    entry = JsonUtility.FromJson<StringEntryLine>(line);
                }
                catch (System.Exception exception)
                {
                    problems.Add($"line {lineNumber}: {exception.Message}");
                    continue;
                }

                if (string.IsNullOrEmpty(entry.table) || string.IsNullOrEmpty(entry.key) || entry.value == null)
                {
                    problems.Add($"line {lineNumber}: missing table, key or value");
                    continue;
                }

                if (!tables.TryGetValue(entry.table, out var table))
                {
                    table = UxmlStringExtractor.FindTable(entry.table);
                    tables[entry.table] = table;
                }

                if (table == null)
                {
                    problems.Add($"line {lineNumber}: no table \"{entry.table}\"");
                    continue;
                }

                var isSmart = entry.smart || Placeholder.IsMatch(entry.value);
                table.AddStringEntry(entry.key, entry.value, isSmart);
                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);
                added++;
            }

            AssetDatabase.SaveAssets();

            return $"Imported {added} entries from {Path.GetFileName(manifestPath)}" + (problems.Count > 0 ? ":\n" + string.Join("\n", problems) : ".");
        }

        [System.Serializable]
        private class StringEntryLine
        {
            public string table;
            public string key;
            public string value;
            public bool smart;
        }
    }
}
