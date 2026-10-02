using AlephOne;
using ForgePlus.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.Extensions
{
    // Display names of Aleph One's values, each the Common string table's "Term.<class>.<constant>" entry (so it can be
    // reworded), which starts as the name of Aleph One's own constant for it (polygon type 5 is "platform", from
    // _polygon_is_platform). A value with no constant is shown as its number.
    [NoAutoStaticsCleanup]
    public static class AlephOneNames
    {
        // Civilians keep their prefix in their names ("civilian crew", not "crew")
        private const string CivilianPrefix = "_civilian_";

        private sealed class Category
        {
            public Type ConstantsClass;
            public string[] Prefixes = Array.Empty<string>();
            public string[] ExcludedPrefixes = Array.Empty<string>();
            public string Suffix = string.Empty;
            public string[] Candidates;

            // Each value's constant (the first, where constants alias a value), and its name from it
            public Dictionary<long, (string Constant, string Name)> Names;
        }

        private static readonly Category PolygonTypes = new Category
        {
            ConstantsClass = typeof(map),
            Prefixes = new[] { "_polygon_is_", "_polygon_" },
        };

        private static readonly Category SideTypes = new Category
        {
            ConstantsClass = typeof(map),
            Suffix = "_side",
            Candidates = new[] { "_full_side", "_high_side", "_low_side", "_composite_side", "_split_side" },
        };

        private static readonly Category LightTypes = new Category
        {
            ConstantsClass = typeof(lightsource),
            Suffix = "_light",
            Candidates = new[] { "_normal_light", "_strobe_light", "_media_light" },
        };

        private static readonly Category LightingFunctions = new Category
        {
            ConstantsClass = typeof(lightsource),
            Suffix = "_lighting_function",
            Candidates = new[]
            {
                "_constant_lighting_function",
                "_linear_lighting_function",
                "_smooth_lighting_function",
                "_flicker_lighting_function",
            },
        };

        private static readonly Category PlatformTypes = new Category
        {
            ConstantsClass = typeof(platforms),
            Prefixes = new[] { "_platform_is_" },
        };

        // Media sounds share the media type prefix
        private static readonly Category MediaTypes = new Category
        {
            ConstantsClass = typeof(media),
            Prefixes = new[] { "_media_" },
            Candidates = new[] { "_media_water", "_media_lava", "_media_goo", "_media_sewage", "_media_jjaro" },
        };

        private static readonly Category ControlPanelClasses = new Category
        {
            ConstantsClass = typeof(map),
            Prefixes = new[] { "_panel_is_" },
        };

        private static readonly Category TerminalGroupTypes = new Category
        {
            ConstantsClass = typeof(computer_interface),
            Suffix = "_group",
            Candidates = new[]
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
            },
        };

        // Monster flags share the monster type prefix
        private static readonly Category MonsterTypes = new Category
        {
            ConstantsClass = typeof(monsters),
            Prefixes = new[] { "_monster_", CivilianPrefix },
            ExcludedPrefixes = new[] { "_monster_is_", "_monster_was_", "_monster_has_", "_monster_can_" },
        };

        private static readonly Category AmbientSounds = new Category
        {
            ConstantsClass = typeof(SoundManagerEnums),
            Prefixes = new[] { "_ambient_snd_" },
        };

        private static readonly Category RandomSounds = new Category
        {
            ConstantsClass = typeof(SoundManagerEnums),
            Prefixes = new[] { "_random_snd_" },
        };

        private static readonly Category ItemTypes = new Category
        {
            ConstantsClass = typeof(items),
            Prefixes = new[] { "_i_" },
        };

        private static readonly Category[] Categories =
        {
            PolygonTypes, SideTypes, LightTypes, LightingFunctions, PlatformTypes, MediaTypes, ControlPanelClasses,
            TerminalGroupTypes, MonsterTypes, AmbientSounds, RandomSounds, ItemTypes,
        };

        public static string PolygonType(short type) { return GetName(PolygonTypes, type); }

        public static string SideType(short type) { return GetName(SideTypes, type); }

        public static string LightType(short type) { return GetName(LightTypes, type); }

        public static string LightingFunction(short function) { return GetName(LightingFunctions, function); }

        public static string PlatformType(short type) { return GetName(PlatformTypes, type); }

        public static string MediaType(short type) { return GetName(MediaTypes, type); }

        public static string ControlPanelClass(short panelClass) { return GetName(ControlPanelClasses, panelClass); }

        public static string TerminalGroupType(short type) { return GetName(TerminalGroupTypes, type); }

        // As terminal source names it ("logon", for #LOGON), which is syntax, so it's never localized
        public static string TerminalGroupSourceName(short type)
        {
            return GetNames(TerminalGroupTypes).TryGetValue(type, out var name) ? name.Name : type.ToString();
        }

        public static string MonsterType(short type) { return GetName(MonsterTypes, type); }

        public static string AmbientSound(short ambientSound) { return GetName(AmbientSounds, ambientSound); }

        public static string RandomSound(short randomSound) { return GetName(RandomSounds, randomSound); }

        public static string ItemType(short type) { return GetName(ItemTypes, type); }

        // Every name's entry (its key, and its text from its constant), for filling the Common table
        public static IEnumerable<KeyValuePair<string, string>> AllEntries()
        {
            foreach (var category in Categories)
            {
                foreach (var name in GetNames(category).Values)
                {
                    yield return new KeyValuePair<string, string>(TermKey(category, name.Constant), name.Name);
                }
            }
        }

        private static string TermKey(Category category, string constant)
        {
            return $"Term.{category.ConstantsClass.Name}.{constant}";
        }

        private static string GetName(Category category, long value)
        {
            if (!GetNames(category).TryGetValue(value, out var name))
            {
                return value.ToString();
            }

            return Strings.TryGet(Strings.Common, TermKey(category, name.Constant), out var localized) ? localized : name.Name;
        }

        private static Dictionary<long, (string Constant, string Name)> GetNames(Category category)
        {
            if (category.Names != null)
            {
                return category.Names;
            }

            var names = new Dictionary<long, (string Constant, string Name)>();

            // In declaration order (reflection doesn't guarantee any order), so the first alias of a value wins
            foreach (var field in category.ConstantsClass.GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(field => field.MetadataToken))
            {
                var constantName = field.Name;

                if (!field.IsLiteral || constantName.StartsWith("NUMBER_OF", StringComparison.Ordinal))
                {
                    continue;
                }

                bool matches;
                if (category.Candidates != null)
                {
                    matches = category.Candidates.Contains(constantName);
                }
                else
                {
                    matches = category.Prefixes.Any(prefix => constantName.StartsWith(prefix, StringComparison.Ordinal)) &&
                              !category.ExcludedPrefixes.Any(prefix => constantName.StartsWith(prefix, StringComparison.Ordinal));
                }

                if (!matches)
                {
                    continue;
                }

                var constantValue = Convert.ToInt64(field.GetRawConstantValue());

                // Where constants alias the same value, the first is the canonical one
                if (!names.ContainsKey(constantValue))
                {
                    names[constantValue] = (constantName, Prettify(constantName, category.Prefixes, category.Suffix));
                }
            }

            category.Names = names;

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
