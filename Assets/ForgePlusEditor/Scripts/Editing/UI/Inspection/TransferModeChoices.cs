using AlephOne;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.Inspection
{
    // The transfer modes a surface (a side, floor or ceiling) can choose, as "index - name". What the original Marathon 2
    // and Infinity draw on surfaces (render.c: instantiate_polygon_transfer_mode) can be chosen as it is. Aleph One adds
    // some (marked as Aleph One only). The rest are for objects: Marathon 2 and Infinity stop with an error drawing them
    // on a surface (and Aleph One draws them as normal), so they're listed but can't be chosen.
    [NoAutoStaticsCleanup]
    public static class TransferModeChoices
    {
        private const string AlephOneOnly = " (Aleph One only)";
        private const string ObjectsOnly = " (objects only)";

        private static readonly string[] Names =
        {
            "Normal",
            "Fade Out to Black",
            "Invisibility",
            "Subtle Invisibility",
            "Pulsate",
            "Wobble",
            "Fast Wobble",
            "Static",
            "50% Static",
            "Landscape",
            "Smear",
            "Fade Out Static",
            "Pulsating Static",
            "Fold In",
            "Fold Out",
            "Horizontal Slide",
            "Fast Horizontal Slide",
            "Vertical Slide",
            "Fast Vertical Slide",
            "Wander",
            "Fast Wander",
            "Big Landscape",
            "Reverse Horizontal Slide",
            "Reverse Fast Horizontal Slide",
            "Reverse Vertical Slide",
            "Reverse Fast Vertical Slide",
            "2x Scale",
            "4x Scale",
        };

        public static readonly List<string> All = new List<string>();

        public static readonly List<string> Unavailable = new List<string>();

        static TransferModeChoices()
        {
            for (short mode = 0; mode < Names.Length; mode++)
            {
                All.Add(Choice(mode));

                if (IsObjectsOnly(mode))
                {
                    Unavailable.Add(Choice(mode));
                }
            }
        }

        // "7 - Static", or just the number for a mode outside the list
        public static string Choice(short mode)
        {
            if (mode < 0 || mode >= Names.Length)
            {
                return mode.ToString();
            }

            var suffix = IsAlephOneOnly(mode) ? AlephOneOnly : (IsObjectsOnly(mode) ? ObjectsOnly : string.Empty);

            return $"{mode} - {Names[mode]}{suffix}";
        }

        public static bool TryParse(string choice, out short mode)
        {
            mode = 0;
            var index = All.IndexOf(choice);
            if (index < 0 || Unavailable.Contains(choice))
            {
                return false;
            }

            mode = (short) index;
            return true;
        }

        // The reverse slides and texture scales, and big landscapes (which only Aleph One's OpenGL renderer draws on surfaces;
        // the original engine's surface case for them is commented out)
        private static bool IsAlephOneOnly(short mode)
        {
            return mode >= map._xfer_big_landscape;
        }

        private static bool IsObjectsOnly(short mode)
        {
            switch (mode)
            {
                case map._xfer_fade_out_to_black:
                case map._xfer_invisibility:
                case map._xfer_subtle_invisibility:
                case map._xfer_50percent_static:
                case map._xfer_fade_out_static:
                case map._xfer_pulsating_static:
                case map._xfer_fold_in:
                case map._xfer_fold_out:
                    return true;
                default:
                    return false;
            }
        }
    }
}
