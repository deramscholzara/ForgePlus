using ForgePlus.LevelManipulation;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Localization;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.Localization
{
    // ForgePlus's display text, from its string tables: Common (what more than one mode shows), Menu, and one for each
    // mode (loaded as the mode is entered). In a layout, a text, tooltip or label of "@Table/Key" names its entry.
    [AutoStaticsCleanup]
    public static partial class Strings
    {
        public const string Common = "Common";
        public const string Menu = "Menu";

        // Each mode's own table (as TableFor names it)
        public const string Geometry = nameof(ModeManager.PrimaryModes.Geometry);
        public const string Textures = nameof(ModeManager.PrimaryModes.Textures);
        public const string Lights = nameof(ModeManager.PrimaryModes.Lights);
        public const string Media = nameof(ModeManager.PrimaryModes.Media);
        public const string Sounds = nameof(ModeManager.PrimaryModes.Sounds);
        public const string Heights = nameof(ModeManager.PrimaryModes.Heights);
        public const string Platforms = nameof(ModeManager.PrimaryModes.Platforms);
        public const string Objects = nameof(ModeManager.PrimaryModes.Objects);
        public const string Annotations = nameof(ModeManager.PrimaryModes.Annotations);
        public const string Level = nameof(ModeManager.PrimaryModes.Level);
        public const string Map = nameof(ModeManager.PrimaryModes.Map);
        public const string Terminals = nameof(ModeManager.PrimaryModes.Terminals);

        private const char KeyMarker = '@';
        private const char TableSeparator = '/';

        // The tables ForgePlus is using (Common and Menu, and the open mode's)
        private static HashSet<string> loadedTables = new HashSet<string>();

        private static Dictionary<Type, PropertyInfo> labelProperties = new Dictionary<Type, PropertyInfo>();

        // The mode's own table, or null for no mode
        public static string TableFor(ModeManager.PrimaryModes mode)
        {
            return mode == ModeManager.PrimaryModes.None ? null : mode.ToString();
        }

        // Selects the startup locale, and loads the tables used everywhere. The settings are registered if they aren't
        // (Fast Enter Play Mode clears them as a Play session starts).
        public static async Awaitable InitializeAsync(LocalizationSettings settings)
        {
            if (!LocalizationSettings.HasSettings && settings != null)
            {
                LocalizationSettings.Instance = settings;
            }

            if (!LocalizationSettings.HasSettings)
            {
                Debug.LogError("There are no localization settings, so ForgePlus's text can't be shown.");
                return;
            }

            await LocalizationSettings.InitializeAsync();

            // A Play session starts with none loaded (the last one's are still listed, with Fast Enter Play Mode, though
            // localization's cache of them is gone)
            loadedTables.Clear();

            await LoadTableAsync(Common);
            await LoadTableAsync(Menu);
        }

        public static bool IsLoaded(string table)
        {
            // And still in localization's cache (which is emptied as a Play session ends)
            return !string.IsNullOrEmpty(table) && loadedTables.Contains(table) &&
                   LocalizationSettings.ResourceDatabase?.GetTable(table) != null;
        }

        public static async Awaitable LoadTableAsync(string table)
        {
            var database = LocalizationSettings.ResourceDatabase;
            if (string.IsNullOrEmpty(table) || database == null)
            {
                return;
            }

            var loadedTable = database.GetTable(table) ?? await database.GetTableAsync(table);
            if (loadedTable == null)
            {
                Debug.LogError($"String table \"{table}\" could not be loaded.");
                return;
            }

            loadedTables.Add(table);
        }

        // Unity's built-in localization only releases all of a locale's tables at once, so this one stays in its cache
        public static void ReleaseTable(string table)
        {
            if (!string.IsNullOrEmpty(table))
            {
                loadedTables.Remove(table);
            }
        }

        // The entry's text, formatted with the arguments (as a Smart String), or the key itself if its table isn't
        // loaded or doesn't have it (so a missing string shows as what to add)
        public static string Get(string table, string key, params object[] args)
        {
            var database = LocalizationSettings.ResourceDatabase;
            var value = database?.GetLocalizedString(table, key, args: args != null && args.Length > 0 ? args : null);

            return string.IsNullOrEmpty(value) ? key : value;
        }

        // The entry's text, or false if its table isn't loaded or doesn't have it (for text with a fallback of its own)
        public static bool TryGet(string table, string key, out string value)
        {
            var database = LocalizationSettings.ResourceDatabase;
            value = IsLoaded(table) ? database?.GetLocalizedString(table, key) : null;

            return !string.IsNullOrEmpty(value);
        }

        // Binds each text, tooltip and label in the tree that names an entry ("@Table/Key") to it, and shows its text now
        // if its table is loaded (the binding fills it in when it is, otherwise)
        public static void Localize(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            root.Query<VisualElement>().ForEach(LocalizeElement);
        }

        // "@Table/Key" as its table and key
        public static bool TryParseKey(string text, out string table, out string key)
        {
            table = null;
            key = null;

            if (string.IsNullOrEmpty(text) || text[0] != KeyMarker)
            {
                return false;
            }

            var separator = text.IndexOf(TableSeparator);
            if (separator <= 1 || separator == text.Length - 1)
            {
                return false;
            }

            table = text.Substring(1, separator - 1);
            key = text.Substring(separator + 1);
            return true;
        }

        // In place of the element's "@Table/Key", whose binding would otherwise put its text back
        public static void SetText(TextElement element, string text)
        {
            element.ClearBinding("text");
            element.text = text;
        }

        public static void SetText(BaseBoolField field, string text)
        {
            field.ClearBinding("text");
            field.text = text;
        }

        // In place of the element's "@Table/Key" tooltip, whose binding would otherwise put it back
        public static void SetTooltip(VisualElement element, string tooltip)
        {
            element.ClearBinding("tooltip");
            element.tooltip = tooltip;
        }

        private static void LocalizeElement(VisualElement element)
        {
            if (TryParseKey(element.tooltip, out var tooltipTable, out var tooltipKey))
            {
                Bind(element, "tooltip", tooltipTable, tooltipKey, value => element.tooltip = value);
            }

            switch (element)
            {
                case TextElement textElement when TryParseKey(textElement.text, out var table, out var key):
                    Bind(element, "text", table, key, value => textElement.text = value);
                    break;
                case BaseBoolField boolField when TryParseKey(boolField.text, out var table, out var key):
                    Bind(element, "text", table, key, value => boolField.text = value);
                    break;
                case Foldout foldout when TryParseKey(foldout.text, out var table, out var key):
                    Bind(element, "text", table, key, value => foldout.text = value);
                    break;
            }

            // A field's label (any BaseField's)
            var labelProperty = LabelProperty(element.GetType());
            if (labelProperty != null && TryParseKey(labelProperty.GetValue(element) as string, out var labelTable, out var labelKey))
            {
                Bind(element, "label", labelTable, labelKey, value => labelProperty.SetValue(element, value));
            }
        }

        private static void Bind(VisualElement element, string property, string table, string key, Action<string> show)
        {
            show(IsLoaded(table) ? Get(table, key) : string.Empty);

            element.SetBinding(property, new LocalizedString { TableReference = table, TableEntryReference = key });
        }

        private static PropertyInfo LabelProperty(Type type)
        {
            if (!labelProperties.TryGetValue(type, out var property))
            {
                property = type.GetProperty("label", BindingFlags.Public | BindingFlags.Instance);
                if (property != null && (property.PropertyType != typeof(string) || !property.CanWrite))
                {
                    property = null;
                }

                labelProperties[type] = property;
            }

            return property;
        }
    }
}
