using AlephOne;

namespace ForgePlus.DataFileIO
{
    // The selected sounds file, which says which ambient and random sounds a level can use. Without one, what's valid
    // isn't known, so the fields that choose sounds can't be edited.
    public class SoundsLoading : FileLoadingBase<SoundsLoading, SoundsData, SoundsFile>
    {
        protected override DataFileTypes DataFileType
        {
            get
            {
                return DataFileTypes.Sounds;
            }
        }

        // Loads the selected file (such as one chosen in an earlier session) the first time it's needed
        public bool IsLoaded
        {
            get
            {
                TryLoadSelectedFile(() => LoadFile(forceReload: false), "Sounds", "sound fields can't be edited");

                return data != null;
            }
        }

        // Null without one
        public SoundsFile File
        {
            get
            {
                return IsLoaded ? data.SoundsFile : null;
            }
        }

        public override void UnloadFile()
        {
            // Its clips go with it
            data?.LoadedFile?.ReleaseClips();

            base.UnloadFile();
        }

        // For ambient sound images and sound objects
        public bool HasAmbientSound(short ambientSound)
        {
            var definition = SoundManager.get_ambient_sound_definition(ambientSound);

            return IsLoaded && definition != null && data.SoundsFile.CanPlay(definition.sound_index);
        }

        // For random sound images
        public bool HasRandomSound(short randomSound)
        {
            return IsLoaded && data.SoundsFile.CanPlay(SoundManager.RandomSoundIndexToSoundIndex(randomSound));
        }

        // Quiet, normal or loud, or NONE without a sounds file (or a sound for the code)
        public short AmbientSoundBehavior(short ambientSound)
        {
            var definition = SoundManager.get_ambient_sound_definition(ambientSound);

            return IsLoaded && definition != null ? data.SoundsFile.Behavior(definition.sound_index) : cstypes.NONE;
        }
    }
}
