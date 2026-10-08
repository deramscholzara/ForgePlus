using AlephOne;
using ForgePlus.Localization;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.UI
{
    // World distances (1024ths of a world unit) as text: proper fractions ("6 and 9/1024", "-119/1024") or, with the
    // setting, improper ones ("6153/1024"), and "0/1024" to "1023/1024" for a distance that wraps (such as a texture
    // offset). A bare number is read as 1024ths, so a distance is never shown as one.
    [AutoStaticsCleanup]
    public static partial class WorldDistances
    {
        // A fraction, after an optional whole number separated by anything that isn't a digit (such as " and ")
        private static readonly Regex MixedFraction = new Regex(@"^(?:(?<whole>\d+)\D+?)?(?<numerator>\d+)\s*/\s*(?<denominator>\d+)$", RegexOptions.Compiled);

        public static event Action OnFormatChanged;

        private static bool usesImproperFractions;

        public static bool UsesImproperFractions
        {
            get
            {
                return usesImproperFractions;
            }
            set
            {
                if (usesImproperFractions != value)
                {
                    usesImproperFractions = value;
                    OnFormatChanged?.Invoke();
                }
            }
        }

        public static string Format(int distance)
        {
            var whole = distance / world.WORLD_ONE;

            if (usesImproperFractions || whole == 0)
            {
                return Strings.Get(Strings.Common, "WorldDistance.Fraction", distance.ToString(CultureInfo.CurrentCulture));
            }

            // The sign is the whole value's ("-2 and 92/1024")
            var fraction = Math.Abs(distance % world.WORLD_ONE);

            return Strings.Get(Strings.Common, "WorldDistance.Mixed", whole.ToString(CultureInfo.CurrentCulture), fraction.ToString(CultureInfo.CurrentCulture));
        }

        public static string FormatWrapped(int distance)
        {
            return Strings.Get(Strings.Common, "WorldDistance.Fraction", Wrap(distance).ToString(CultureInfo.CurrentCulture));
        }

        public static int Wrap(int distance)
        {
            return ((distance % world.WORLD_ONE) + world.WORLD_ONE) % world.WORLD_ONE;
        }

        // A fraction (proper or improper), a bare number of 1024ths ("6153"), a decimal number of world units ("6.0087"),
        // or a percentage of one ("55%"), rounded to the nearest 1024th
        public static bool TryParse(string text, out int distance)
        {
            distance = 0;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            text = text.Trim();

            var sign = 1;
            if (text[0] == '-' || text[0] == '+')
            {
                sign = text[0] == '-' ? -1 : 1;
                text = text.Substring(1).TrimStart();
            }

            double worldUnits;
            var match = MixedFraction.Match(text);

            if (text.EndsWith("%"))
            {
                if (!double.TryParse(text.Substring(0, text.Length - 1).TrimEnd(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var percent))
                {
                    return false;
                }

                worldUnits = percent / 100.0;
            }
            else if (match.Success)
            {
                var denominator = double.Parse(match.Groups["denominator"].Value, CultureInfo.InvariantCulture);
                if (denominator <= 0)
                {
                    return false;
                }

                var whole = match.Groups["whole"].Success ? double.Parse(match.Groups["whole"].Value, CultureInfo.InvariantCulture) : 0;
                worldUnits = whole + double.Parse(match.Groups["numerator"].Value, CultureInfo.InvariantCulture) / denominator;
            }
            else if (text.Contains("."))
            {
                if (!double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out worldUnits))
                {
                    return false;
                }
            }
            else if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var increments))
            {
                distance = sign * increments;
                return true;
            }
            else
            {
                return false;
            }

            var rounded = Math.Round(sign * worldUnits * world.WORLD_ONE, MidpointRounding.AwayFromZero);
            if (rounded < int.MinValue || rounded > int.MaxValue)
            {
                return false;
            }

            distance = (int) rounded;
            return true;
        }
    }
}
