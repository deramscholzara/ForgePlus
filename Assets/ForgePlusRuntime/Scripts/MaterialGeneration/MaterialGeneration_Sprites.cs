using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities.MapObjects;
using System.Collections.Generic;
using UnityEngine;
using AlephOne;

namespace RuntimeCore.Materials
{
    public class MaterialGeneration_Sprites
    {
        public class DirectionalSprite
        {
            public DirectionalSprite(Material material, Rect bounds)
            {
                Material = material;
                Bounds = bounds;
            }

            public Material Material { get; private set; }

            // Relative to the object's origin, in meters
            public Rect Bounds { get; private set; }
        }

        // One view's frame of a high-level shape, as Aleph One draws it
        private struct SpriteFrame
        {
            public int Width;
            public int Height;

            // Bottom row first, with mirroring applied
            public Color32[] Pixels;

            // Relative to the sprite's origin, in world unit increments.
            // Aleph One doesn't flip these for mirrored shapes (RenderPlaceObjs.cpp).
            public short WorldLeft;
            public short WorldRight;
            public short WorldTop;
            public short WorldBottom;

            // The keypoint, where parasitic sprites (such as player torsos) attach
            public short WorldX0;
            public short WorldY0;

            public bool KeypointObscured;
        }

        private struct PlacedFrame
        {
            public SpriteFrame Frame;

            // In world unit increments
            public float OffsetX;
            public float OffsetY;
        }

        // Every view's frames in draw order (back to front), and their combined bounds in world unit increments
        private class SpriteLayout
        {
            public List<PlacedFrame>[] PlacedFramesByView;
            public float Left = float.MaxValue;
            public float Right = float.MinValue;
            public float Bottom = float.MaxValue;
            public float Top = float.MinValue;
            public float PixelsPerWorldUnit;
        }

        private const string SpriteMaterialResourcePath = "Materials/Sprite";
        private const int MaximumCanvasDimension = 2048;

        private static readonly int SpriteViewsPropertyId = Shader.PropertyToID("_SpriteViews");
        private static readonly int ViewCountPropertyId = Shader.PropertyToID("_ViewCount");

        private static readonly Dictionary<string, DirectionalSprite> SpritesByKey = new Dictionary<string, DirectionalSprite>();

        private static Material spriteMaterialTemplate;

        public static DirectionalSprite GetSprite(SpriteDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            if (SpritesByKey.TryGetValue(definition.Key, out var sprite))
            {
                return sprite;
            }

            sprite = BuildSprite(definition);

            // Failures are cached too, so missing shapes aren't rebuilt for every object that uses them
            SpritesByKey[definition.Key] = sprite;

            return sprite;
        }

        public static void ClearCollection()
        {
            foreach (var sprite in SpritesByKey.Values)
            {
                if (sprite == null)
                {
                    continue;
                }

                Object.Destroy(sprite.Material.GetTexture(SpriteViewsPropertyId));
                Object.Destroy(sprite.Material);
            }

            SpritesByKey.Clear();
        }

        private static DirectionalSprite BuildSprite(SpriteDefinition definition)
        {
            if (!ShapesLoading.Instance.TryLoadFile())
            {
                return null;
            }

            var viewCount = GetSpriteViewCount(definition.Layers[0]);

            if (viewCount <= 0)
            {
                Debug.LogWarning($"Sprite \"{definition.Key}\" was not found in the loaded shapes file.");
                return null;
            }

            var layout = LayOutSprite(definition.Layers, viewCount);

            if (layout.PixelsPerWorldUnit <= 0f || layout.Right <= layout.Left || layout.Top <= layout.Bottom)
            {
                Debug.LogWarning($"Sprite \"{definition.Key}\" has no drawable frames in the loaded shapes file.");
                return null;
            }

            var views = ComposeViews(layout, viewCount, definition.Key);

            var worldToMeters = definition.Scale / GeometryUtilities.WorldUnitIncrementsPerMeter;
            var bounds = Rect.MinMaxRect(
                layout.Left * worldToMeters,
                layout.Bottom * worldToMeters,
                layout.Right * worldToMeters,
                layout.Top * worldToMeters);

            return new DirectionalSprite(CreateMaterial(views, viewCount), bounds);
        }

        private static SpriteLayout LayOutSprite(SpriteLayer[] layers, int viewCount)
        {
            var layout = new SpriteLayout();
            layout.PlacedFramesByView = new List<PlacedFrame>[viewCount];

            for (var view = 0; view < viewCount; view++)
            {
                var placedFrames = new List<PlacedFrame>();
                var offsetX = 0f;
                var offsetY = 0f;
                var hostIndex = -1;

                foreach (var layer in layers)
                {
                    var layerView = GetSpriteViewCount(layer) == viewCount ? view : 0;

                    if (!TryGetSpriteFrame(layer, layerView, frame: 0, out var frame))
                    {
                        continue;
                    }

                    var placedFrame = new PlacedFrame
                    {
                        Frame = frame,
                        OffsetX = offsetX,
                        OffsetY = offsetY,
                    };

                    // A parasite draws in front of its host only when the host's keypoint is obscured
                    if (hostIndex >= 0 && !placedFrames[hostIndex].Frame.KeypointObscured)
                    {
                        // Inserted behind the host, the parasite is at hostIndex, so it's the next host
                        placedFrames.Insert(hostIndex, placedFrame);
                    }
                    else
                    {
                        placedFrames.Add(placedFrame);
                        hostIndex = placedFrames.Count - 1;
                    }

                    offsetX += frame.WorldX0;
                    offsetY += frame.WorldY0;

                    layout.Left = Mathf.Min(layout.Left, frame.WorldLeft + placedFrame.OffsetX);
                    layout.Right = Mathf.Max(layout.Right, frame.WorldRight + placedFrame.OffsetX);
                    layout.Bottom = Mathf.Min(layout.Bottom, frame.WorldBottom + placedFrame.OffsetY);
                    layout.Top = Mathf.Max(layout.Top, frame.WorldTop + placedFrame.OffsetY);

                    var worldWidth = frame.WorldRight - frame.WorldLeft;
                    if (worldWidth > 0)
                    {
                        layout.PixelsPerWorldUnit = Mathf.Max(layout.PixelsPerWorldUnit, frame.Width / (float) worldWidth);
                    }
                }

                layout.PlacedFramesByView[view] = placedFrames;
            }

            return layout;
        }

        private static Texture2DArray ComposeViews(SpriteLayout layout, int viewCount, string key)
        {
            var canvasWidth = Mathf.Clamp(Mathf.CeilToInt((layout.Right - layout.Left) * layout.PixelsPerWorldUnit), 1, MaximumCanvasDimension);
            var canvasHeight = Mathf.Clamp(Mathf.CeilToInt((layout.Top - layout.Bottom) * layout.PixelsPerWorldUnit), 1, MaximumCanvasDimension);

            var views = new Texture2DArray(canvasWidth, canvasHeight, viewCount, TextureFormat.RGBA32, mipChain: false)
            {
                name = $"Sprite Views ({key})",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (var view = 0; view < viewCount; view++)
            {
                var canvas = new Color32[canvasWidth * canvasHeight];

                foreach (var placedFrame in layout.PlacedFramesByView[view])
                {
                    var frame = placedFrame.Frame;
                    var startX = Mathf.RoundToInt((frame.WorldLeft + placedFrame.OffsetX - layout.Left) * layout.PixelsPerWorldUnit);
                    var startY = Mathf.RoundToInt((frame.WorldBottom + placedFrame.OffsetY - layout.Bottom) * layout.PixelsPerWorldUnit);

                    for (var y = 0; y < frame.Height; y++)
                    {
                        var canvasY = startY + y;
                        if (canvasY < 0 || canvasY >= canvasHeight)
                        {
                            continue;
                        }

                        for (var x = 0; x < frame.Width; x++)
                        {
                            var canvasX = startX + x;
                            if (canvasX < 0 || canvasX >= canvasWidth)
                            {
                                continue;
                            }

                            var pixel = frame.Pixels[x + y * frame.Width];
                            if (pixel.a > 0)
                            {
                                canvas[canvasX + canvasY * canvasWidth] = pixel;
                            }
                        }
                    }
                }

                views.SetPixels32(canvas, view);
            }

            views.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            return views;
        }

        private static Material CreateMaterial(Texture2DArray views, int viewCount)
        {
            if (!spriteMaterialTemplate)
            {
                spriteMaterialTemplate = Resources.Load<Material>(SpriteMaterialResourcePath);
            }

            var material = new Material(spriteMaterialTemplate)
            {
                name = "Sprite",
                enableInstancing = true,
            };
            material.SetTexture(SpriteViewsPropertyId, views);
            material.SetFloat(ViewCountPropertyId, viewCount);

            return material;
        }

        private static int GetSpriteViewCount(SpriteLayer layer)
        {
            var animation = shapes.get_shape_animation_data(layer.ShapeDescriptor);
            if (animation == null || animation.frames_per_view < 1)
            {
                return 0;
            }

            // There are frames_per_view indexes for each view (shapes.cpp: load_high_level_shape)
            return animation.low_level_shape_indexes.Length / animation.frames_per_view;
        }

        // Resolved as Aleph One does (map.cpp: get_object_shape_and_transfer_mode)
        private static bool TryGetSpriteFrame(SpriteLayer layer, int view, int frame, out SpriteFrame spriteFrame)
        {
            spriteFrame = default;

            var animation = shapes.get_shape_animation_data(layer.ShapeDescriptor);
            if (animation == null || frame < 0 || frame >= animation.frames_per_view)
            {
                return false;
            }

            var index = view * animation.frames_per_view + frame;
            if (index < 0 || index >= animation.low_level_shape_indexes.Length)
            {
                return false;
            }

            var lowLevelShapeIndex = animation.low_level_shape_indexes[index];
            var lowLevelShape = shapes.get_low_level_shape_definition(layer.Collection, lowLevelShapeIndex);
            if (lowLevelShape == null)
            {
                return false;
            }

            var information = shapes.extended_get_shape_information(layer.Collection, lowLevelShapeIndex);
            var bitmap = IndexedShapeBitmap.Decode(layer.Collection, layer.CLUT, lowLevelShape.bitmap_index, information.flags);
            if (bitmap == null)
            {
                return false;
            }

            spriteFrame.Width = bitmap.Width;
            spriteFrame.Height = bitmap.Height;
            spriteFrame.Pixels = bitmap.GetPixels32(bitmap.GetPalette32());
            spriteFrame.WorldLeft = information.world_left;
            spriteFrame.WorldRight = information.world_right;
            spriteFrame.WorldTop = information.world_top;
            spriteFrame.WorldBottom = information.world_bottom;
            spriteFrame.WorldX0 = information.world_x0;
            spriteFrame.WorldY0 = information.world_y0;
            spriteFrame.KeypointObscured = (information.flags & collection_definition._KEYPOINT_OBSCURED_BIT) != 0;

            return true;
        }
    }
}
