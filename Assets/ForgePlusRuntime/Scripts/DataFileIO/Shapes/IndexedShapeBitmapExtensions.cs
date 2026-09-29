using AlephOne;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public static class IndexedShapeBitmapExtensions
    {
        public static Color32 ToColor32(this rgb_color_value color)
        {
            // Aleph One's colors have 16 bits per channel
            return new Color32((byte) (color.red >> 8), (byte) (color.green >> 8), (byte) (color.blue >> 8), byte.MaxValue);
        }

        // Transparent where the bitmap's palette has no color
        public static Color32[] GetPalette32(this IndexedShapeBitmap bitmap)
        {
            var palette = new Color32[bitmap.Palette.Length];
            for (var i = 0; i < palette.Length; i++)
            {
                var color = bitmap.Palette[i];
                if (color != null)
                {
                    palette[i] = color.ToColor32();
                }
            }

            return palette;
        }
    }
}
