#if !NO_EDITING
using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using System;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Steps shared by several kinds of level edits
    public static class LevelEditing
    {
        // A frame later, so the inspector whose edit this is has finished with its (soon destroyed) entities
        public static async void RebuildLevel(Action afterRebuilding = null)
        {
            await Awaitable.NextFrameAsync();

            MapsLoading.Instance.RebuildLevel();

            afterRebuilding?.Invoke();
        }

        // A landscape texture is drawn as a landscape (keeping a big landscape's mode), and other textures aren't
        public static short TransferModeForTexture(ushort shapeDescriptor, short transferMode)
        {
            if (shapeDescriptor.UsesLandscapeCollection())
            {
                return AlephOneExtensions.IsLandscapeTransferMode(transferMode) ? transferMode : map._xfer_landscape;
            }

            return AlephOneExtensions.IsLandscapeTransferMode(transferMode) ? map._xfer_normal : transferMode;
        }
    }
}
#endif
