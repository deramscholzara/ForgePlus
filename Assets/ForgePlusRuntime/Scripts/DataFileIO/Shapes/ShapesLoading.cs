using RuntimeCore.Materials;
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

        public override void UnloadFile()
        {
            MaterialGeneration_Geometry.ClearCollection();

            base.UnloadFile();
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
