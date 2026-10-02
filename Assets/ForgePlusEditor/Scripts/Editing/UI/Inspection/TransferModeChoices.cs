using AlephOne;
using ForgePlus.Localization;
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
        // Each mode's name's entry (TransferMode.Name.<name>)
        private static readonly string[] NameKeys =
        {
            "Normal",
            "FadeOutToBlack",
            "Invisibility",
            "SubtleInvisibility",
            "Pulsate",
            "Wobble",
            "FastWobble",
            "Static",
            "HalfStatic",
            "Landscape",
            "Smear",
            "FadeOutStatic",
            "PulsatingStatic",
            "FoldIn",
            "FoldOut",
            "HorizontalSlide",
            "FastHorizontalSlide",
            "VerticalSlide",
            "FastVerticalSlide",
            "Wander",
            "FastWander",
            "BigLandscape",
            "ReverseHorizontalSlide",
            "ReverseFastHorizontalSlide",
            "ReverseVerticalSlide",
            "ReverseFastVerticalSlide",
            "Scale2x",
            "Scale4x",
        };

        public static List<string> All
        {
            get
            {
                var all = new List<string>();
                for (short mode = 0; mode < NameKeys.Length; mode++)
                {
                    all.Add(Choice(mode));
                }

                return all;
            }
        }

        public static List<string> Unavailable
        {
            get
            {
                var unavailable = new List<string>();
                for (short mode = 0; mode < NameKeys.Length; mode++)
                {
                    if (IsObjectsOnly(mode))
                    {
                        unavailable.Add(Choice(mode));
                    }
                }

                return unavailable;
            }
        }

        // "7 - Static", or just the number for a mode outside the list
        public static string Choice(short mode)
        {
            if (mode < 0 || mode >= NameKeys.Length)
            {
                return mode.ToString();
            }

            var name = Strings.Get(Strings.Textures, "TransferMode.Name." + NameKeys[mode]);
            var choiceKey = IsAlephOneOnly(mode) ? "TransferMode.Choice.AlephOneOnly" :
                            (IsObjectsOnly(mode) ? "TransferMode.Choice.ObjectsOnly" : "TransferMode.Choice");

            return Strings.Get(Strings.Textures, choiceKey, mode, name);
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
