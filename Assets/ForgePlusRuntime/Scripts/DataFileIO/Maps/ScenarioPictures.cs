using AlephOne;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    // The scenario's pictures (terminal pictures and logos), from the maps file's resources or the shapes file's,
    // as Aleph One finds them (images.get_picture_resource_from_scenario)
    [AutoStaticsCleanup]
    public static partial class ScenarioPictures
    {
        public class Picture
        {
            public Texture2D Texture;

            // The width in the picture's header, which Aleph One compares to the picture's own width
            public int HeaderWidth;
        }

        // Null where the picture couldn't be found or decoded
        private static readonly Dictionary<short, Picture> pictures = new Dictionary<short, Picture>();

        public static Picture Get(short pictureId)
        {
            if (!pictures.TryGetValue(pictureId, out var picture))
            {
                picture = Load(pictureId);
                pictures[pictureId] = picture;
            }

            return picture;
        }

        public static void Clear()
        {
            foreach (var picture in pictures.Values)
            {
                if (picture != null)
                {
                    Object.Destroy(picture.Texture);
                }
            }

            pictures.Clear();
        }

        private static Picture Load(short pictureId)
        {
            var resource = new LoadedResource();
            if (!images.get_picture_resource_from_scenario(pictureId, resource))
            {
                return null;
            }

            var surface = images.picture_to_surface(resource);
            if (surface == null)
            {
                return null;
            }

            var texture = new Texture2D(surface.w, surface.h, TextureFormat.RGBA32, mipChain: false)
            {
                name = $"Picture {pictureId}",
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixelData(ToPixels(surface), mipLevel: 0);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            return new Picture
            {
                Texture = texture,
                HeaderWidth = images.get_pict_header_width(resource),
            };
        }

        // Surfaces are top row first, and textures bottom row first
        private static Color32[] ToPixels(SDL_Surface surface)
        {
            var pixels = new Color32[surface.w * surface.h];
            var source = surface.pixels;

            for (var y = 0; y < surface.h; y++)
            {
                var row = y * surface.pitch;
                var destination = (surface.h - 1 - y) * surface.w;

                for (var x = 0; x < surface.w; x++)
                {
                    switch (surface.format.BitsPerPixel)
                    {
                        case 8:
                        {
                            var color = surface.format.palette.colors[source[row + x]];
                            pixels[destination + x] = new Color32(color.r, color.g, color.b, 255);
                            break;
                        }
                        case 16:
                        {
                            // 1-5-5-5, little-endian
                            var value = source[row + x * 2] | (source[row + x * 2 + 1] << 8);
                            pixels[destination + x] = new Color32(Expand5((value >> 10) & 0x1f), Expand5((value >> 5) & 0x1f), Expand5(value & 0x1f), 255);
                            break;
                        }
                        default:
                        {
                            // x-8-8-8, little-endian
                            var offset = row + x * 4;
                            pixels[destination + x] = new Color32(source[offset + 2], source[offset + 1], source[offset], 255);
                            break;
                        }
                    }
                }
            }

            return pixels;
        }

        private static byte Expand5(int value)
        {
            return (byte) ((value << 3) | (value >> 2));
        }
    }
}
