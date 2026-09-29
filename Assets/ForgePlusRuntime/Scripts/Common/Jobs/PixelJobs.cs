using System;
using System.Collections.Generic;
using ForgePlus.DataFileIO;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace ForgePlus.Jobs
{
    public static class PixelJobs
    {
        private const int RowsPerBatch = 16;

        // Where a texture format keeps each channel of a pixel (-1 where it has none)
        private struct PixelLayout
        {
            public int BytesPerPixel;
            public int Red;
            public int Green;
            public int Blue;
            public int Alpha;

            public static PixelLayout For(TextureFormat format)
            {
                switch (format)
                {
                    case TextureFormat.RGBA32:
                        return new PixelLayout { BytesPerPixel = 4, Red = 0, Green = 1, Blue = 2, Alpha = 3 };
                    case TextureFormat.ARGB32:
                        return new PixelLayout { BytesPerPixel = 4, Red = 1, Green = 2, Blue = 3, Alpha = 0 };
                    case TextureFormat.RGB24:
                        return new PixelLayout { BytesPerPixel = 3, Red = 0, Green = 1, Blue = 2, Alpha = -1 };
                    default:
                        throw new ArgumentException($"Pixels can't be converted to {format}.", nameof(format));
                }
            }
        }

        // Converts a bitmap's palette indexes ([y * width + x], top row first) to a texture's pixel data, in Unity's bottom-up order.
        // Landscapes are stored rotated, so their run x becomes texture row x, and element y becomes column (height - 1 - y).
        [BurstCompile]
        private struct IndexedToPixelsJob : IJobParallelFor
        {
            public int Width;
            public int Height;
            public bool IsLandscape;
            public PixelLayout Layout;

            [ReadOnly]
            public NativeArray<byte> Indexes;

            [ReadOnly]
            public NativeArray<Color32> Palette;

            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeArray<byte> Pixels;

            public void Execute(int y)
            {
                for (var x = 0; x < Width; x++)
                {
                    var pixelIndex = IsLandscape ? x * Height + (Height - 1 - y) : (Height - 1 - y) * Width + x;
                    var offset = pixelIndex * Layout.BytesPerPixel;
                    var color = Palette[Indexes[y * Width + x]];

                    Pixels[offset + Layout.Red] = color.r;
                    Pixels[offset + Layout.Green] = color.g;
                    Pixels[offset + Layout.Blue] = color.b;

                    if (Layout.Alpha >= 0)
                    {
                        Pixels[offset + Layout.Alpha] = color.a;
                    }
                }
            }
        }

        // Draws a frame's opaque pixels onto a canvas, one frame row per index
        [BurstCompile]
        private struct CompositeFrameJob : IJobParallelFor
        {
            public int FrameWidth;
            public int StartX;
            public int StartY;
            public int CanvasWidth;
            public int CanvasHeight;

            [ReadOnly]
            public NativeArray<Color32> FramePixels;

            [NativeDisableParallelForRestriction]
            public NativeArray<Color32> Canvas;

            public void Execute(int y)
            {
                var canvasY = StartY + y;
                if (canvasY < 0 || canvasY >= CanvasHeight)
                {
                    return;
                }

                for (var x = 0; x < FrameWidth; x++)
                {
                    var canvasX = StartX + x;
                    if (canvasX < 0 || canvasX >= CanvasWidth)
                    {
                        continue;
                    }

                    var pixel = FramePixels[x + y * FrameWidth];
                    if (pixel.a > 0)
                    {
                        Canvas[canvasX + canvasY * CanvasWidth] = pixel;
                    }
                }
            }
        }

        public struct CanvasFrame
        {
            public NativeArray<Color32> Pixels;
            public int Width;
            public int Height;
            public int StartX;
            public int StartY;
        }

        // Jobs that are scheduled together and completed together, whose memory is freed on Dispose.
        // Conversions run in parallel, and each canvas draws its frames in order once they're converted.
        // Their results are pixel data for Texture2D.SetPixelData and Texture2DArray.SetPixelData.
        public sealed class PixelBatch : IDisposable
        {
            private readonly List<IDisposable> nativeArrays = new List<IDisposable>();

            private JobHandle conversions;
            private JobHandle all;

            // The bitmap's buffers are copied, so it can be disposed once this returns
            public NativeArray<byte> ScheduleConversion(IndexedShapeBitmap bitmap, Color32[] palette, bool isLandscape, TextureFormat format)
            {
                var layout = PixelLayout.For(format);
                var pixelCount = bitmap.PixelCount;

                var nativeIndexes = new NativeArray<byte>(pixelCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                NativeArray<byte>.Copy(bitmap.Indexes, nativeIndexes, pixelCount);
                var nativePalette = new NativeArray<Color32>(palette, Allocator.Persistent);
                var nativePixels = new NativeArray<byte>(pixelCount * layout.BytesPerPixel, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                nativeArrays.Add(nativeIndexes);
                nativeArrays.Add(nativePalette);
                nativeArrays.Add(nativePixels);

                var handle = new IndexedToPixelsJob
                {
                    Width = bitmap.Width,
                    Height = bitmap.Height,
                    IsLandscape = isLandscape,
                    Layout = layout,
                    Indexes = nativeIndexes,
                    Palette = nativePalette,
                    Pixels = nativePixels,
                }.Schedule(bitmap.Height, RowsPerBatch);

                conversions = JobHandle.CombineDependencies(conversions, handle);
                all = JobHandle.CombineDependencies(all, handle);

                return nativePixels;
            }

            // For frames to draw onto canvases
            public NativeArray<Color32> ScheduleColorConversion(IndexedShapeBitmap bitmap, Color32[] palette)
            {
                return ScheduleConversion(bitmap, palette, isLandscape: false, TextureFormat.RGBA32).Reinterpret<Color32>(sizeof(byte));
            }

            // Frames are drawn in order, so later frames cover earlier ones
            public NativeArray<Color32> ScheduleComposite(int canvasWidth, int canvasHeight, CanvasFrame[] frames)
            {
                var canvas = new NativeArray<Color32>(canvasWidth * canvasHeight, Allocator.Persistent);
                nativeArrays.Add(canvas);

                var dependency = conversions;

                foreach (var frame in frames)
                {
                    dependency = new CompositeFrameJob
                    {
                        FrameWidth = frame.Width,
                        StartX = frame.StartX,
                        StartY = frame.StartY,
                        CanvasWidth = canvasWidth,
                        CanvasHeight = canvasHeight,
                        FramePixels = frame.Pixels,
                        Canvas = canvas,
                    }.Schedule(frame.Height, RowsPerBatch, dependency);
                }

                all = JobHandle.CombineDependencies(all, dependency);

                return canvas;
            }

            public void Complete()
            {
                all.Complete();
            }

            public void Dispose()
            {
                all.Complete();

                foreach (var array in nativeArrays)
                {
                    array.Dispose();
                }

                nativeArrays.Clear();
            }
        }
    }
}
