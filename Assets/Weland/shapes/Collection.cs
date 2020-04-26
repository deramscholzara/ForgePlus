using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Weland
{
    class CollectionHeader
    {
        public short Status;
        public ushort Flags;
        public int Offset;
        public int Length;
        public int Offset16;
        public int Length16;

        public void Load(BinaryReaderBE reader)
        {
            Status = reader.ReadInt16();
            Flags = reader.ReadUInt16();
            Offset = reader.ReadInt32();
            Length = reader.ReadInt32();
            Offset16 = reader.ReadInt32();
            Length16 = reader.ReadInt32();
            reader.BaseStream.Seek(12, SeekOrigin.Current);
        }
    }

    public enum CollectionType : short
    {
        Unused,
        Wall,
        Object,
        Interface,
        Scenery
    }

    public class Collection
    {
#pragma warning disable 0414
        public short Version;
        public CollectionType Type;
        public ushort Flags;

        short colorCount;
        short colorTableCount;
        int colorTableOffset;

        short highLevelShapeCount;
        int highLevelShapeOffsetTableOffset;

        short lowLevelShapeCount;
        int lowLevelShapeOffsetTableOffset;

        public short BitmapCount
        {
            get
            {
                return bitmapCount;
            }
        }

        public int ColorTableCount
        {
            get
            {
                return colorTables.Count;
            }
        }

        short bitmapCount;
        int bitmapOffsetTableOffset;

        short pixelsToWorld;
        int size;

#pragma warning restore 0414

        struct ColorValue
        {
            public byte Flags;
            public byte Value;

            public ushort Red;
            public ushort Green;
            public ushort Blue;

            public void Load(BinaryReaderBE reader)
            {
                Flags = reader.ReadByte();
                Value = reader.ReadByte();

                Red = reader.ReadUInt16();
                Green = reader.ReadUInt16();
                Blue = reader.ReadUInt16();
            }
        }

        /// <summary>
        /// This is "Frame" data in Anvil terms.
        /// </summary>
        struct LowLevelShapeDefinition
        {
            public ushort Flags;
            public double MinimumLightIntensity;
            public short BitmapIndex;
            public short OriginX, OriginY;
            public short KeyX, KeyY;
            public short WorldLeft, WorldRight, WorldTop, WorldBottom;
            public short WorldX0, WorldY0;
            private short[] Unused;

            public void Load(BinaryReaderBE reader)
            {
                Flags = reader.ReadUInt16();
                MinimumLightIntensity = reader.ReadFixed();
                BitmapIndex = reader.ReadInt16();
                OriginX = reader.ReadInt16();
                OriginY = reader.ReadInt16();
                KeyX = reader.ReadInt16();
                KeyY = reader.ReadInt16();
                WorldLeft = reader.ReadInt16();
                WorldRight = reader.ReadInt16();
                WorldTop = reader.ReadInt16();
                WorldBottom = reader.ReadInt16();
                WorldX0 = reader.ReadInt16();
                WorldY0 = reader.ReadInt16();

                Unused = new short[4];
                for (int i = 0; i < Unused.Length; i++)
                {
                    Unused[i] = reader.ReadInt16();
                }
            }
        }

        List<ColorValue[]> colorTables = new List<ColorValue[]>();
        List<Bitmap> bitmaps = new List<Bitmap>();
        List<LowLevelShapeDefinition> lowLevelShapes = new List<LowLevelShapeDefinition>();

        public void Load(BinaryReaderBE reader)
        {
            long origin = reader.BaseStream.Position;

            Version = reader.ReadInt16();
            Type = (CollectionType)reader.ReadInt16();
            Flags = reader.ReadUInt16();
            colorCount = reader.ReadInt16();
            colorTableCount = reader.ReadInt16();
            colorTableOffset = reader.ReadInt32();
            highLevelShapeCount = reader.ReadInt16();
            highLevelShapeOffsetTableOffset = reader.ReadInt32();
            lowLevelShapeCount = reader.ReadInt16();
            lowLevelShapeOffsetTableOffset = reader.ReadInt32();
            bitmapCount = reader.ReadInt16();
            bitmapOffsetTableOffset = reader.ReadInt32();
            pixelsToWorld = reader.ReadInt16();
            size = reader.ReadInt32();
            reader.BaseStream.Seek(253 * 2, SeekOrigin.Current);

            colorTables.Clear();
            reader.BaseStream.Seek(origin + colorTableOffset, SeekOrigin.Begin);
            for (int i = 0; i < colorTableCount; ++i)
            {
                ColorValue[] table = new ColorValue[colorCount];
                for (int j = 0; j < colorCount; ++j)
                {
                    table[j].Load(reader);
                }
                colorTables.Add(table);
            }

            bitmaps.Clear();
            reader.BaseStream.Seek(origin + bitmapOffsetTableOffset, SeekOrigin.Begin);
            for (int i = 0; i < bitmapCount; ++i)
            {
                int bitmapPosition = reader.ReadInt32();
                long nextPositionInOffsetTable = reader.BaseStream.Position;

                reader.BaseStream.Seek(origin + bitmapPosition, SeekOrigin.Begin);

                Bitmap bitmap = new Bitmap();
                bitmap.Load(reader);
                bitmaps.Add(bitmap);

                reader.BaseStream.Seek(nextPositionInOffsetTable, SeekOrigin.Begin);
            }

            lowLevelShapes.Clear();
            reader.BaseStream.Seek(origin + lowLevelShapeOffsetTableOffset, SeekOrigin.Begin);
            for (int i = 0; i < lowLevelShapeCount; i++)
            {
                long shapeDefinitionPosition = origin + reader.ReadInt32();
                long nextPositionInOffsetTable = reader.BaseStream.Position;

                reader.BaseStream.Seek(shapeDefinitionPosition, SeekOrigin.Begin);

                LowLevelShapeDefinition lowLevelShapeDefinition = new LowLevelShapeDefinition();
                lowLevelShapeDefinition.Load(reader);
                lowLevelShapes.Add(lowLevelShapeDefinition);

                reader.BaseStream.Seek(nextPositionInOffsetTable, SeekOrigin.Begin);
            }
        }

        // This is how Weland does it, the unity version is below
        ////public System.Drawing.Bitmap GetShape(byte ColorTableIndex, byte BitmapIndex)
        ////{
        ////    Bitmap bitmap = bitmaps[BitmapIndex];
        ////    ColorValue[] colorTable = colorTables[ColorTableIndex];
        ////    Color[] colors = new Color[colorTable.Length];
        ////    for (int i = 0; i < colorTable.Length; ++i)
        ////    {
        ////        ColorValue color = colorTable[i];
        ////        colors[i] = Color.FromArgb(color.Red >> 8,
        ////                       color.Green >> 8,
        ////                       color.Blue >> 8);
        ////    }

        ////    System.Drawing.Bitmap result = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height);
        ////    for (int x = 0; x < bitmap.Width; ++x)
        ////    {
        ////        for (int y = 0; y < bitmap.Height; ++y)
        ////        {
        ////            result.SetPixel(x, y, colors[bitmap.Data[x + y * bitmap.Width]]);
        ////        }
        ////    }

        ////    return result;
        ////}

        public Texture2D GetShape(byte ColorTableIndex, byte BitmapIndex)
        {
            Bitmap bitmap = Type == CollectionType.Wall && BitmapIndex < (lowLevelShapeCount - 1) ? bitmaps[lowLevelShapes[BitmapIndex].BitmapIndex] : bitmaps[BitmapIndex];
            ColorValue[] colorTable = colorTables[ColorTableIndex];
            Color[] colors = new Color[colorTable.Length];
            bool hasAlpha = false;

            for (int i = 0; i < colorTable.Length; i++)
            {
                ColorValue color = colorTable[i];
                colors[i].r = (float)color.Red / (float)ushort.MaxValue;
                colors[i].g = (float)color.Green / (float)ushort.MaxValue;
                colors[i].b = (float)color.Blue / (float)ushort.MaxValue;
                if (colors[i].r != 0.0f || colors[i].g != 0.0f || colors[i].b != 1.0f)
                {
                    colors[i].a = 1;
                }
                else
                {
                    colors[i].a = 0;
                }
            }

            Texture2D result;
            for (int i = 0; i < bitmap.Data.Length; i++)
            {
                if (bitmap.Data[i] == 0)
                {
                    hasAlpha = true;
                }
            }

            bool isLandscape = bitmap.Width == 512 && bitmap.Height == 270;

            if (hasAlpha)
            {
                result = new Texture2D(bitmap.Width, bitmap.Height, TextureFormat.ARGB32, mipChain: true);
            }
            else
            {
                if (isLandscape)
                {
                    result = new Texture2D(bitmap.Width, bitmap.Height, TextureFormat.RGB24, mipChain: false);
                }
                else
                {
                    result = new Texture2D(bitmap.Width, bitmap.Height, TextureFormat.RGB24, mipChain: true);
                }
            }

            if (!bitmap.ColumnOrder || isLandscape)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        result.SetPixel(bitmap.Width - 1 - x, bitmap.Height - y - 1, colors[bitmap.Data[x + y * bitmap.Width]]);

                    }
                }
            }
            else
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        result.SetPixel(bitmap.Width - 1 - x, bitmap.Height - y - 1, colors[bitmap.Data[x * bitmap.Height + y]]);

                    }
                }

            }

            if (isLandscape)
            {
                result.wrapModeV = TextureWrapMode.Clamp;
            }

            result.filterMode = FilterMode.Point;
            result.Apply();

            return result;
        }
    }
}