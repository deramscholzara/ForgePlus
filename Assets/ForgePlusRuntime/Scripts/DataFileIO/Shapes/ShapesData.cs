using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public class ShapesData : FileDataBase<ShapesFile>
    {
        public Texture2D GetShape(ushort shapeDescriptor)
        {
            LoadData();

            return file.GetShape(shapeDescriptor);
        }

        public bool IsWallCollection(short collection)
        {
            LoadData();

            return file.IsWallCollection(collection);
        }

        public void Close()
        {
            file?.Close();
        }
    }
}
