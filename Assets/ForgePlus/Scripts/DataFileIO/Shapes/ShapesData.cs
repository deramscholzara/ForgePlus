using UnityEngine;
using Weland;

namespace ForgePlus.DataFileIO
{
    public class ShapesData : FileDataBase<ShapesFile>
    {
        public Texture2D GetShape(ShapeDescriptor shapeDescriptor)
        {
            LoadData();

            return file.GetShape(shapeDescriptor);
        }
    }
}
