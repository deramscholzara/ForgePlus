using AlephOne;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.Extensions
{
    // Display names taken from the names of Aleph One's own constants (polygon type 5 is "platform", from _polygon_is_platform)
    [NoAutoStaticsCleanup]
    public static class AlephOneNames
    {
        // Civilians keep their prefix in their names ("civilian crew", not "crew")
        private const string CivilianPrefix = "_civilian_";

        private static readonly Dictionary<string, Dictionary<long, string>> NamesByKey = new Dictionary<string, Dictionary<long, string>>();

        public static string PolygonType(short type)
        {
            return GetName(typeof(map), type, prefixes: new[] { "_polygon_is_", "_polygon_" });
        }

        public static string SideType(short type)
        {
            var candidates = new[] { "_full_side", "_high_side", "_low_side", "_composite_side", "_split_side" };

            return GetName(typeof(map), type, suffix: "_side", candidates: candidates);
        }

        public static string LightType(short type)
        {
            var candidates = new[] { "_normal_light", "_strobe_light", "_media_light" };

            return GetName(typeof(lightsource), type, suffix: "_light", candidates: candidates);
        }

        public static string LightingFunction(short function)
        {
            var candidates = new[]
            {
                "_constant_lighting_function",
                "_linear_lighting_function",
                "_smooth_lighting_function",
                "_flicker_lighting_function",
            };

            return GetName(typeof(lightsource), function, suffix: "_lighting_function", candidates: candidates);
        }

        public static string PlatformType(short type)
        {
            return GetName(typeof(platforms), type, prefixes: new[] { "_platform_is_" });
        }

        // Media sounds share the media type prefix
        public static string MediaType(short type)
        {
            var candidates = new[] { "_media_water", "_media_lava", "_media_goo", "_media_sewage", "_media_jjaro" };

            return GetName(typeof(media), type, prefixes: new[] { "_media_" }, candidates: candidates);
        }

        public static string ControlPanelClass(short panelClass)
        {
            return GetName(typeof(map), panelClass, prefixes: new[] { "_panel_is_" });
        }

        public static string TerminalGroupType(short type)
        {
            var candidates = new[]
            {
                "_logon_group",
                "_unfinished_group",
                "_success_group",
                "_failure_group",
                "_information_group",
                "_end_group",
                "_interlevel_teleport_group",
                "_intralevel_teleport_group",
                "_checkpoint_group",
                "_sound_group",
                "_movie_group",
                "_track_group",
                "_pict_group",
                "_logoff_group",
                "_camera_group",
                "_static_group",
                "_tag_group",
            };

            return GetName(typeof(computer_interface), type, suffix: "_group", candidates: candidates);
        }

        // Monster flags share the monster type prefix
        public static string MonsterType(short type)
        {
            var excludedPrefixes = new[] { "_monster_is_", "_monster_was_", "_monster_has_", "_monster_can_" };

            return GetName(typeof(monsters), type, prefixes: new[] { "_monster_", CivilianPrefix }, excludedPrefixes: excludedPrefixes);
        }

        public static string ItemType(short type)
        {
            return GetName(typeof(items), type, prefixes: new[] { "_i_" });
        }

        private static string GetName(Type constantsClass, long value, string[] prefixes = null, string[] excludedPrefixes = null, string suffix = "", string[] candidates = null)
        {
            prefixes = prefixes ?? Array.Empty<string>();
            excludedPrefixes = excludedPrefixes ?? Array.Empty<string>();

            var key = string.Join("|",
                                  constantsClass.FullName,
                                  string.Join(",", prefixes),
                                  suffix,
                                  candidates != null ? string.Join(",", candidates) : string.Empty);

            if (!NamesByKey.TryGetValue(key, out var names))
            {
                names = GetNames(constantsClass, prefixes, excludedPrefixes, suffix, candidates);

                NamesByKey[key] = names;
            }

            if (names.TryGetValue(value, out var name))
            {
                return name;
            }

            return value.ToString();
        }

        private static Dictionary<long, string> GetNames(Type constantsClass, string[] prefixes, string[] excludedPrefixes, string suffix, string[] candidates)
        {
            var names = new Dictionary<long, string>();

            // In declaration order (reflection doesn't guarantee any order), so the first alias of a value wins
            foreach (var field in constantsClass.GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(field => field.MetadataToken))
            {
                var constantName = field.Name;

                if (!field.IsLiteral || constantName.StartsWith("NUMBER_OF", StringComparison.Ordinal))
                {
                    continue;
                }

                bool matches;
                if (candidates != null)
                {
                    matches = candidates.Contains(constantName);
                }
                else
                {
                    matches = prefixes.Any(prefix => constantName.StartsWith(prefix, StringComparison.Ordinal)) &&
                              !excludedPrefixes.Any(prefix => constantName.StartsWith(prefix, StringComparison.Ordinal));
                }

                if (!matches)
                {
                    continue;
                }

                var constantValue = Convert.ToInt64(field.GetRawConstantValue());

                // Where constants alias the same value, the first is the canonical one
                if (!names.ContainsKey(constantValue))
                {
                    names[constantValue] = Prettify(constantName, prefixes, suffix);
                }
            }

            return names;
        }

        private static string Prettify(string constantName, string[] prefixes, string suffix)
        {
            var name = constantName;

            var prefix = prefixes.Where(p => name.StartsWith(p, StringComparison.Ordinal)).OrderByDescending(p => p.Length).FirstOrDefault();
            if (prefix != null && prefix != CivilianPrefix)
            {
                name = name.Substring(prefix.Length);
            }
            else
            {
                name = name.TrimStart('_');
            }

            if (!string.IsNullOrEmpty(suffix) && name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - suffix.Length);
            }

            return name.Trim('_').Replace('_', ' ');
        }
    }
}
