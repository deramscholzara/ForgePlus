using ForgePlus.DataFileIO;
using ForgePlus.Jobs;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities.MapObjects;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using AlephOne;

namespace RuntimeCore.Materials
{
    // Resets its own statics (see ResetStatics), since pending sprites hold native memory that must be disposed
    [NoAutoStaticsCleanup]
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

            // Bottom row first, with mirroring applied (written by a scheduled conversion job)
            public NativeArray<Color32> Pixels;

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

        // A sprite whose pixel jobs are scheduled (or that failed), waiting for Finish to make its material
        private class PendingSprite
        {
            public SpriteDefinition Definition;
            public string FailureMessage;

            public int ViewCount;
            public SpriteLayout Layout;
            public int CanvasWidth;
            public int CanvasHeight;
            public NativeArray<Color32>[] Canvases;
            public PixelJobs.PixelBatch Batch;
        }

        private const string SpriteMaterialResourcePath = "Materials/Sprite";
        private const int MaximumCanvasDimension = 2048;

        private static readonly int SpriteViewsPropertyId = Shader.PropertyToID("_SpriteViews");
        private static readonly int ViewCountPropertyId = Shader.PropertyToID("_ViewCount");

        private static readonly Dictionary<string, DirectionalSprite> SpritesByKey = new Dictionary<string, DirectionalSprite>();
        private static readonly Dictionary<string, PendingSprite> PendingSprites = new Dictionary<string, PendingSprite>();

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

            if (!PendingSprites.TryGetValue(definition.Key, out var pendingSprite))
            {
                if (!ShapesLoading.Instance.TryLoadFile())
                {
                    return null;
                }

                pendingSprite = PrepareSprite(definition);
            }

            PendingSprites.Remove(definition.Key);
            sprite = FinishSprite(pendingSprite);

            // Failures are cached too, so missing shapes aren't rebuilt for every object that uses them
            SpritesByKey[definition.Key] = sprite;

            return sprite;
        }

        // Schedules the pixel work for these sprites on worker threads, so it runs while other things load.
        // Each is finished (its texture and material made) when GetSprite first asks for it.
        public static void PrepareSprites(IEnumerable<SpriteDefinition> definitions)
        {
            if (!ShapesLoading.Instance.TryLoadFile())
            {
                return;
            }

            foreach (var definition in definitions)
            {
                if (definition == null ||
                    SpritesByKey.ContainsKey(definition.Key) ||
                    PendingSprites.ContainsKey(definition.Key))
                {
                    continue;
                }

                PendingSprites[definition.Key] = PrepareSprite(definition);
            }
        }

        public static void ClearCollection()
        {
            DisposePendingSprites();

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

        // For Fast Enter Play Mode, where statics survive between Play sessions
        // (the previous session's sprites are left for Unity to unload)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            DisposePendingSprites();

            SpritesByKey.Clear();
            spriteMaterialTemplate = null;
        }

        private static void DisposePendingSprites()
        {
            foreach (var pendingSprite in PendingSprites.Values)
            {
                pendingSprite.Batch?.Dispose();
            }

            PendingSprites.Clear();
        }

        private static PendingSprite PrepareSprite(SpriteDefinition definition)
        {
            var pendingSprite = new PendingSprite
            {
                Definition = definition,
                ViewCount = GetSpriteViewCount(definition.Layers[0]),
            };

            if (pendingSprite.ViewCount <= 0)
            {
                pendingSprite.FailureMessage = $"Sprite \"{definition.Key}\" was not found in the loaded shapes file.";
                return pendingSprite;
            }

            pendingSprite.Batch = new PixelJobs.PixelBatch();
            pendingSprite.Layout = LayOutSprite(definition.Layers, pendingSprite.ViewCount, pendingSprite.Batch);

            var layout = pendingSprite.Layout;
            if (layout.PixelsPerWorldUnit <= 0f || layout.Right <= layout.Left || layout.Top <= layout.Bottom)
            {
                pendingSprite.FailureMessage = $"Sprite \"{definition.Key}\" has no drawable frames in the loaded shapes file.";
                pendingSprite.Batch.Dispose();
                pendingSprite.Batch = null;
                return pendingSprite;
            }

            ScheduleViews(pendingSprite);

            return pendingSprite;
        }

        private static DirectionalSprite FinishSprite(PendingSprite pendingSprite)
        {
            if (pendingSprite.FailureMessage != null)
            {
                Debug.LogWarning(pendingSprite.FailureMessage);
                return null;
            }

            var definition = pendingSprite.Definition;
            var layout = pendingSprite.Layout;

            Texture2DArray views;
            try
            {
                pendingSprite.Batch.Complete();

                views = CreateViews(pendingSprite);
            }
            finally
            {
                pendingSprite.Batch.Dispose();
            }

            var worldToMeters = definition.Scale / GeometryUtilities.WorldUnitIncrementsPerMeter;
            var bounds = Rect.MinMaxRect(
                layout.Left * worldToMeters,
                layout.Bottom * worldToMeters,
                layout.Right * worldToMeters,
                layout.Top * worldToMeters);

            return new DirectionalSprite(CreateMaterial(views, pendingSprite.ViewCount), bounds);
        }

        private static SpriteLayout LayOutSprite(SpriteLayer[] layers, int viewCount, PixelJobs.PixelBatch batch)
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

                    if (!TryGetSpriteFrame(layer, layerView, frame: 0, batch, out var frame))
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

        private static void ScheduleViews(PendingSprite pendingSprite)
        {
            var layout = pendingSprite.Layout;
            var canvasWidth = Mathf.Clamp(Mathf.CeilToInt((layout.Right - layout.Left) * layout.PixelsPerWorldUnit), 1, MaximumCanvasDimension);
            var canvasHeight = Mathf.Clamp(Mathf.CeilToInt((layout.Top - layout.Bottom) * layout.PixelsPerWorldUnit), 1, MaximumCanvasDimension);

            pendingSprite.CanvasWidth = canvasWidth;
            pendingSprite.CanvasHeight = canvasHeight;
            pendingSprite.Canvases = new NativeArray<Color32>[pendingSprite.ViewCount];

            for (var view = 0; view < pendingSprite.ViewCount; view++)
            {
                var placedFrames = layout.PlacedFramesByView[view];
                var canvasFrames = new PixelJobs.CanvasFrame[placedFrames.Count];

                for (var i = 0; i < placedFrames.Count; i++)
                {
                    var placedFrame = placedFrames[i];
                    var frame = placedFrame.Frame;

                    canvasFrames[i] = new PixelJobs.CanvasFrame
                    {
                        Pixels = frame.Pixels,
                        Width = frame.Width,
                        Height = frame.Height,
                        StartX = Mathf.RoundToInt((frame.WorldLeft + placedFrame.OffsetX - layout.Left) * layout.PixelsPerWorldUnit),
                        StartY = Mathf.RoundToInt((frame.WorldBottom + placedFrame.OffsetY - layout.Bottom) * layout.PixelsPerWorldUnit),
                    };
                }

                pendingSprite.Canvases[view] = pendingSprite.Batch.ScheduleComposite(canvasWidth, canvasHeight, canvasFrames);
            }
        }

        private static Texture2DArray CreateViews(PendingSprite pendingSprite)
        {
            var views = new Texture2DArray(pendingSprite.CanvasWidth, pendingSprite.CanvasHeight, pendingSprite.ViewCount, TextureFormat.RGBA32, mipChain: false)
            {
                name = $"Sprite Views ({pendingSprite.Definition.Key})",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (var view = 0; view < pendingSprite.ViewCount; view++)
            {
                views.SetPixelData(pendingSprite.Canvases[view], mipLevel: 0, element: view);
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
        private static bool TryGetSpriteFrame(SpriteLayer layer, int view, int frame, PixelJobs.PixelBatch batch, out SpriteFrame spriteFrame)
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
            using (var bitmap = IndexedShapeBitmap.Decode(layer.Collection, layer.CLUT, lowLevelShape.bitmap_index, information.flags))
            {
                if (bitmap == null)
                {
                    return false;
                }

                spriteFrame.Width = bitmap.Width;
                spriteFrame.Height = bitmap.Height;
                spriteFrame.Pixels = batch.ScheduleColorConversion(bitmap, bitmap.GetPalette32());
            }

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
