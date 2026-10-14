using RuntimeCore.Materials;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public class ShapesLoading : FileLoadingBase<ShapesLoading, ShapesData, ShapesFile>
    {
        protected override DataFileTypes DataFileType
        {
            get { return DataFileTypes.Shapes; }
        }

        // The plugins' shapes patches the loaded shapes have
        private List<string> loadedShapesPatches = new List<string>();

        public override void LoadFile(bool forceReload = true)
        {
            if (!forceReload && data != null)
            {
                // Already loaded, so exit
                return;
            }

            // The plugins (whose shapes patches are applied as the shapes load) are found first
            MmlLoading.Instance.EnsureInitialized();

            loadedShapesPatches = AlephOne.Plugins.instance().find_shapes_patches(MmlLoading.IsOpenGL);

            base.LoadFile(forceReload);
        }

        // When the plugins in use have other shapes patches than the loaded shapes have
        public void ReloadIfShapesPatchesChanged()
        {
            if (data != null && !AlephOne.Plugins.instance().find_shapes_patches(MmlLoading.IsOpenGL).SequenceEqual(loadedShapesPatches))
            {
                LoadFile();
            }
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