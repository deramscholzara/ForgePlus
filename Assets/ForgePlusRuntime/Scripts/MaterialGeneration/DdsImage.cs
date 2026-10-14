using UnityEngine;

namespace RuntimeCore.Materials
{
    // DirectDraw Surface images (which Aleph One loads itself, ImageLoader_Shared.cpp: LoadDDSFromFile): DXT1, DXT3 and
    // DXT5 compressed, or 24- or 32-bit uncompressed. Only the full-size image is read (not its mipmaps).
    public static class DdsImage
    {
        private const int HeaderSize = 128;

        public static bool IsDds(byte[] bytes)
        {
            return bytes.Length >= HeaderSize && bytes[0] == 'D' && bytes[1] == 'D' && bytes[2] == 'S' && bytes[3] == ' ' &&
                   ReadInt32(bytes, 4) == 124;
        }

        // A readable RGBA image, bottom row first (as Unity textures are), or null if it isn't one of the formats read
        public static Texture2D Load(byte[] bytes)
        {
            var height = ReadInt32(bytes, 12);
            var width = ReadInt32(bytes, 16);
            var pixelFormatFlags = ReadInt32(bytes, 80);
            var fourCC = ReadInt32(bytes, 84);
            var bitCount = ReadInt32(bytes, 88);

            if (width <= 0 || height <= 0)
            {
                return null;
            }

            const int FourCCFlag = 0x4;
            Color32[] topRowFirst;

            if ((pixelFormatFlags & FourCCFlag) != 0 && fourCC == FourCC('D', 'X', 'T', '3'))
            {
                // Unity has no DXT3 format, so it's decoded here
                var size = Mathf.Max(1, (width + 3) / 4) * Mathf.Max(1, (height + 3) / 4) * 16;
                if (HeaderSize + size > bytes.Length)
                {
                    return null;
                }

                topRowFirst = DecodeDxt3(bytes, HeaderSize, width, height);
            }
            else if ((pixelFormatFlags & FourCCFlag) != 0)
            {
                TextureFormat format;
                int blockSize;
                if (fourCC == FourCC('D', 'X', 'T', '1'))
                {
                    format = TextureFormat.DXT1;
                    blockSize = 8;
                }
                else if (fourCC == FourCC('D', 'X', 'T', '5'))
                {
                    format = TextureFormat.DXT5;
                    blockSize = 16;
                }
                else
                {
                    return null;
                }

                var size = Mathf.Max(1, (width + 3) / 4) * Mathf.Max(1, (height + 3) / 4) * blockSize;
                if (HeaderSize + size > bytes.Length)
                {
                    return null;
                }

                var data = new byte[size];
                System.Buffer.BlockCopy(bytes, HeaderSize, data, 0, size);

                var compressed = new Texture2D(width, height, format, mipChain: false);
                try
                {
                    compressed.LoadRawTextureData(data);
                    compressed.Apply(updateMipmaps: false);
                    topRowFirst = compressed.GetPixels32();
                }
                finally
                {
                    Object.Destroy(compressed);
                }
            }
            else if (bitCount == 24 || bitCount == 32)
            {
                var bytesPerPixel = bitCount / 8;
                if (HeaderSize + width * height * bytesPerPixel > bytes.Length)
                {
                    return null;
                }

                var redMask = ReadInt32(bytes, 92);
                var isBgr = redMask == 0x00ff0000;
                var hasAlpha = bitCount == 32 && ReadInt32(bytes, 104) != 0;

                topRowFirst = new Color32[width * height];
                for (var i = 0; i < topRowFirst.Length; i++)
                {
                    var offset = HeaderSize + i * bytesPerPixel;
                    var first = bytes[offset];
                    var third = bytes[offset + 2];
                    topRowFirst[i] = new Color32(
                        isBgr ? third : first,
                        bytes[offset + 1],
                        isBgr ? first : third,
                        hasAlpha ? bytes[offset + 3] : (byte) 255);
                }
            }
            else
            {
                return null;
            }

            // DDS images are stored top row first
            var pixels = new Color32[topRowFirst.Length];
            for (var y = 0; y < height; y++)
            {
                System.Array.Copy(topRowFirst, (height - 1 - y) * width, pixels, y * width, width);
            }

            var image = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            image.SetPixels32(pixels);
            image.Apply(updateMipmaps: false);

            return image;
        }

        // 4x4 blocks, each with explicit 4-bit alpha for each pixel, then a color block (two RGB565 colors, and the two
        // between them, chosen by 2 bits for each pixel), left to right and top to bottom
        private static Color32[] DecodeDxt3(byte[] bytes, int offset, int width, int height)
        {
            var pixels = new Color32[width * height];
            var palette = new Color32[4];
            var blocksWide = Mathf.Max(1, (width + 3) / 4);
            var blocksHigh = Mathf.Max(1, (height + 3) / 4);

            for (var blockY = 0; blockY < blocksHigh; blockY++)
            {
                for (var blockX = 0; blockX < blocksWide; blockX++)
                {
                    var block = offset + (blockY * blocksWide + blockX) * 16;

                    palette[0] = Rgb565(bytes[block + 8] | (bytes[block + 9] << 8));
                    palette[1] = Rgb565(bytes[block + 10] | (bytes[block + 11] << 8));
                    palette[2] = Lerp(palette[0], palette[1], 1, 3);
                    palette[3] = Lerp(palette[0], palette[1], 2, 3);
                    var indexes = ReadInt32(bytes, block + 12);

                    for (var i = 0; i < 16; i++)
                    {
                        var x = blockX * 4 + i % 4;
                        var y = blockY * 4 + i / 4;
                        if (x >= width || y >= height)
                        {
                            continue;
                        }

                        var alphaNibble = (bytes[block + i / 2] >> ((i % 2) * 4)) & 0xF;
                        var color = palette[(indexes >> (i * 2)) & 0x3];
                        color.a = (byte) (alphaNibble * 17);
                        pixels[y * width + x] = color;
                    }
                }
            }

            return pixels;
        }

        private static Color32 Rgb565(int value)
        {
            var red = (value >> 11) & 0x1F;
            var green = (value >> 5) & 0x3F;
            var blue = value & 0x1F;

            return new Color32((byte) ((red << 3) | (red >> 2)), (byte) ((green << 2) | (green >> 4)), (byte) ((blue << 3) | (blue >> 2)), 255);
        }

        private static Color32 Lerp(Color32 from, Color32 to, int numerator, int denominator)
        {
            return new Color32(
                (byte) ((from.r * (denominator - numerator) + to.r * numerator) / denominator),
                (byte) ((from.g * (denominator - numerator) + to.g * numerator) / denominator),
                (byte) ((from.b * (denominator - numerator) + to.b * numerator) / denominator),
                255);
        }

        private static int ReadInt32(byte[] bytes, int offset)
        {
            return bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
        }

        private static int FourCC(char a, char b, char c, char d)
        {
            return a | (b << 8) | (c << 16) | (d << 24);
        }
    }
}
