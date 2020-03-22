using UnityEngine;
using Weland;

namespace ForgePlus.DataFileIO
{
    public class ShapesLoading : FileLoadingBase<ShapesData, ShapesFile>
    {
        private static ShapesLoading instance;

        public static ShapesLoading Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ShapesLoading();
                }

                return instance;
            }
        }

        protected override DataFileTypes dataFileType
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
                Debug.LogError($"Tried getting shape ShapeDescriptor with no loaded Shapes file.  You may need to call LoadData() first.");
                // No shapes data is loaded, so exit
                return null;
            }

            return data.GetShape(shapeDescriptor);
        }
    }
}
