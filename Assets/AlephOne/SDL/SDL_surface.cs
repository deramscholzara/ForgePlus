// Stand-in for the parts of SDL2's SDL_surface.h and SDL_pixels.h that Aleph One uses to decode pictures:
// software surfaces of 8 (paletted), 16 or 32 bits per pixel, their pixels in native (little-endian) order,
// with rows padded to 4 bytes as SDL pads them.
namespace AlephOne
{
    public class SDL_Color
    {
        public byte r, g, b, a;
    }

    public class SDL_Palette
    {
        public SDL_Color[] colors;
    }

    public class SDL_PixelFormat
    {
        public byte BitsPerPixel;
        public uint Rmask, Gmask, Bmask, Amask;
        public SDL_Palette palette; // null unless BitsPerPixel is 8
    }

    public class SDL_Surface
    {
        public SDL_PixelFormat format;
        public int w, h;
        public int pitch;
        public byte[] pixels;
    }

    public static class SDL_surface
    {
        public const uint SDL_SWSURFACE = 0;

        public static SDL_Surface SDL_CreateRGBSurface(uint flags, int width, int height, int depth, uint Rmask, uint Gmask, uint Bmask, uint Amask)
        {
            if (width < 0 || height < 0 || (depth != 8 && depth != 16 && depth != 32))
                return null;

            var format = new SDL_PixelFormat
            {
                BitsPerPixel = (byte) depth,
                Rmask = Rmask,
                Gmask = Gmask,
                Bmask = Bmask,
                Amask = Amask,
            };

            if (depth == 8)
            {
                // SDL starts a palette out white
                var colors = new SDL_Color[256];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = new SDL_Color { r = 0xff, g = 0xff, b = 0xff, a = 0xff };
                format.palette = new SDL_Palette { colors = colors };
            }

            int pitch = (width * depth / 8 + 3) & ~3;

            return new SDL_Surface
            {
                format = format,
                w = width,
                h = height,
                pitch = pitch,
                pixels = new byte[pitch * height],
            };
        }

        public static int SDL_SetPaletteColors(SDL_Palette palette, SDL_Color[] colors, int firstcolor, int ncolors)
        {
            if (palette == null)
                return -1;

            for (int i = 0; i < ncolors && firstcolor + i < palette.colors.Length; i++)
            {
                if (colors[i] != null)
                    palette.colors[firstcolor + i] = colors[i];
            }
            return 0;
        }

        public static void SDL_FreeSurface(SDL_Surface surface)
        {
            // Garbage collected
        }
    }
}
