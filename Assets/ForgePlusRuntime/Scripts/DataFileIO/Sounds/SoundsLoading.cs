using AlephOne;
using System;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    // The selected sounds file, which says which of the engine's ambient and random sounds a level can use.
    // Without one, what's valid isn't known, so the fields that choose sounds can't be edited.
    public class SoundsLoading : FileLoadingBase<SoundsLoading, SoundsData, SoundsFile>
    {
        private string failedPath;

        protected override DataFileTypes DataFileType
        {
            get { return DataFileTypes.Sounds; }
        }

        // Loads the selected file (such as one chosen in an earlier session) the first time it's needed
        public bool IsLoaded
        {
            get
            {
                TryLoadFile();

                return data != null;
            }
        }

        public override void LoadFile(bool forceReload = true)
        {
            failedPath = null;

            base.LoadFile(forceReload);
        }

        // Whether the ambient sound code (as in ambient sound images and sound objects) plays a sound in the file
        public bool HasAmbientSound(short ambientSound)
        {
            var definition = SoundManager.get_ambient_sound_definition(ambientSound);

            return IsLoaded && definition != null && data.SoundsFile.HasSound(definition.sound_index);
        }

        // Whether the random sound code (as in random sound images) plays a sound in the file
        public bool HasRandomSound(short randomSound)
        {
            var soundIndex = SoundManager.RandomSoundIndexToSoundIndex(randomSound);

            return IsLoaded && soundIndex != cstypes.NONE && data.SoundsFile.HasSound(soundIndex);
        }

        private void TryLoadFile()
        {
            if (data != null)
            {
                // Already loaded, so exit
                return;
            }

            var path = FileSettings.Instance.GetFilePath(DataFileType);

            if (string.IsNullOrEmpty(path) || path == failedPath)
            {
                // No file, or this file already failed to load, so don't retry (and log) for every lookup
                return;
            }

            try
            {
                base.LoadFile(forceReload: false);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Sounds file \"{path}\" could not be loaded, so sound fields can't be edited: {exception}");

                base.UnloadFile();

                failedPath = path;
            }
        }
    }
}
