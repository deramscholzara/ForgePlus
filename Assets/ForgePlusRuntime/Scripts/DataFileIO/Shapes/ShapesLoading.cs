using RuntimeCore.Materials;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public class ShapesLoading : FileLoadingBase<ShapesLoading, ShapesData, ShapesFile>
    {
        protected override DataFileTypes DataFileType
        {
            get { return DataFileTypes.Shapes; }
        }

        public override void UnloadFile()
        {
            MaterialGeneration_Geometry.ClearCollection();
            MaterialGeneration_Sprites.ClearCollection();

            data?.Close();

            base.UnloadFile();
        }

        // Loads the file if it isn't already, for Aleph One's shapes accessors
        public bool TryLoadFile()
        {
            LoadFile(forceReload: false);

            return data != null;
        }

        public Texture2D GetShape(ushort shapeDescriptor)
        {
            LoadFile(forceReload: false);

            if (data == null)
            {
                // No shapes data is loaded, so exit
                return null;
            }

            return data.GetShape(shapeDescriptor);
        }

        public bool IsWallCollection(short collection)
        {
            LoadFile(forceReload: false);

            return data != null && data.IsWallCollection(collection);
        }
    }
}