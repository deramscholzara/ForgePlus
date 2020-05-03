using UnityEngine;
using Weland;

namespace ForgePlus.DataFileIO
{
    public class ShapesLoading : FileLoadingBase<ShapesLoading, ShapesData, ShapesFile>
    {
        protected override DataFileTypes DataFileType
        {
            get
            {
                return DataFileTypes.Shapes;
            }
        }

        public Texture2D GetShape(ShapeDescriptor shapeDescriptor)
        {
            LoadFile(forceReload: false);

            if (data == null)
            {
                // No shapes data is loaded, so exit
                return null;
            }

            return data.GetShape(shapeDescriptor);
        }
    }
}
