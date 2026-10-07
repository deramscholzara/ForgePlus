using AlephOne;
using ForgePlus.Localization;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.Inspection
{
    // The transfer modes a surface can choose. What the original games draw on surfaces (render.c:
    // instantiate_polygon_transfer_mode) can be chosen, and so can Aleph One's additions. The rest are for objects: the
    // original games stop with an error drawing them on a surface, so they're listed but can't be chosen.
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

        // Includes the surface's own mode, even if it's no mode's
        public static List<string> All(short currentMode)
        {
            return Inspector_Base.ChoicesOf(Modes(currentMode), Choice);
        }

        public static List<string> Unavailable(short currentMode)
        {
            var unavailable = new List<string>();
            foreach (var mode in Modes(currentMode))
            {
                if (IsObjectsOnly(mode))
                {
                    unavailable.Add(Choice(mode));
                }
            }

            return unavailable;
        }

        public static List<string> AlephOneOnly(short currentMode)
        {
            var alephOneOnly = new List<string>();
            foreach (var mode in Modes(currentMode))
            {
                if (IsAlephOneOnly(mode))
                {
                    alephOneOnly.Add(Choice(mode));
                }
            }

            return alephOneOnly;
        }

        // "7 - Static", or just the number for a mode outside the list
        public static string Choice(short mode)
        {
            if (!IsListed(mode))
            {
                return mode.ToString();
            }

            var name = Strings.Get(Strings.Textures, "TransferMode.Name." + NameKeys[mode]);
            var choiceKey = IsAlephOneOnly(mode) ? "TransferMode.Choice.AlephOneOnly" :
                            (IsObjectsOnly(mode) ? "TransferMode.Choice.ObjectsOnly" : "TransferMode.Choice");

            return Strings.Get(Strings.Textures, choiceKey, mode, name);
        }

        public static bool TryParse(short currentMode, string choice, out short mode)
        {
            return Inspector_Base.TryFindChoice(Modes(currentMode), Choice, choice, out mode) && !IsObjectsOnly(mode);
        }

        private static bool IsListed(short mode)
        {
            return mode >= 0 && mode < NameKeys.Length;
        }

        private static List<short> Modes(short currentMode)
        {
            var modes = new List<short>(Inspector_Base.ShortRange(0, NameKeys.Length));
            if (!IsListed(currentMode))
            {
                modes.Add(currentMode);
            }

            return modes;
        }

        // The reverse slides and texture scales, and big landscapes (which only Aleph One's OpenGL renderer draws on
        // surfaces)
        private static bool IsAlephOneOnly(short mode)
        {
            return IsListed(mode) && mode >= map._xfer_big_landscape;
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
