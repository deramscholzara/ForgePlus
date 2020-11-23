using ForgePlus.DataFileIO;
using System;
using System.IO;
using UnityEngine;

namespace Weland
{
    public class ShapesFile : IFileLoadable
    {
        CollectionHeader[] collectionHeaders;
        Collection[] collections;

        public void Load(BinaryReaderBE reader)
        {
            long origin = reader.BaseStream.Position;
            collectionHeaders = new CollectionHeader[ShapeDescriptor.MaximumCollections];
            for (int i = 0; i < collectionHeaders.Length; ++i)
            {
                collectionHeaders[i] = new CollectionHeader();
                collectionHeaders[i].Load(reader);
            }

            collections = new Collection[collectionHeaders.Length];
            for (int i = 0; i < collectionHeaders.Length; ++i)
            {
                collections[i] = new Collection();
                if (collectionHeaders[i].Offset > 0)
                {
                    reader.BaseStream.Seek(origin + collectionHeaders[i].Offset, SeekOrigin.Begin);
                    collections[i].Load(reader);
                }
            }
        }

        public void Load(string filename)
        {
            try
            {
                BinaryReaderBE reader = new BinaryReaderBE(File.Open(filename, FileMode.Open));
                Load(reader);
            }
            catch (Exception exception)
            {
                collectionHeaders = new CollectionHeader[ShapeDescriptor.MaximumCollections];
                collections = new Collection[collectionHeaders.Length];
                for (int i = 0; i < collectionHeaders.Length; ++i)
                {
                    collectionHeaders[i] = new CollectionHeader();
                    collections[i] = new Collection();
                }

                throw exception;
            }
        }

        public Collection GetCollection(int n)
        {
            return collections[n];
        }

        // This is how Weland does it, the unity version is below
        ////public System.Drawing.Bitmap GetShape(ShapeDescriptor d)
        ////{
        ////    Collection coll = collections[d.Collection];
        ////    if (d.Bitmap < coll.BitmapCount && d.CLUT < coll.ColorTableCount)
        ////    {
        ////        return coll.GetShape(d.CLUT, d.Bitmap);
        ////    }
        ////    else
        ////    {
        ////        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(128, 128);
        ////        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
        ////        {
        ////            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, 128, 128);
        ////            graphics.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(127, 127, 127)), rect);
        ////        }

        ////        return bitmap;
        ////    }
        ////}

        public Texture2D GetShape(ShapeDescriptor d)
        {
            Collection coll = collections[d.Collection];
            if (d.Bitmap < coll.BitmapCount && d.CLUT < coll.ColorTableCount)
            {
                var shape = coll.GetShape(d.CLUT, d.Bitmap);
                shape.name = $"CLUT({d.CLUT}) Bitmap({d.Bitmap}) Collection({d.Collection})";
                return shape;
            }
            else
            {
                // TODO: Generate a uniquely identifiable texture here
                //       like a unique color from a gradient, with
                //       a number for the index in the middle.
                ////Texture2D shape = new Texture2D(128, 128);
                ////shape.name = $"CLUT({d.CLUT}) Bitmap({d.Bitmap})";

                ////Debug.LogWarning($"Generating texture for missing shape! CLUT({d.CLUT}) Bitmap({d.Bitmap})");

                ////return shape;
                return null;
            }
        }
    }
}