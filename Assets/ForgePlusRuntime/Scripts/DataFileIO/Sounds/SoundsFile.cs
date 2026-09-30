using AlephOne;
using System.IO;

namespace ForgePlus.DataFileIO
{
    // The sound definitions of a Marathon 2/Infinity sounds file, for knowing which sounds a level can use
    public class SoundsFile : IFileLoadable
    {
        private readonly M2SoundFile soundFile = new M2SoundFile();

        public string Path { get; private set; }

        public void Load(string fileName)
        {
            if (!soundFile.Open(new FileSpecifier(fileName)))
            {
                throw new IOException($"\"{fileName}\" is not a readable Marathon 2 or Infinity sounds file.");
            }

            Path = fileName;
        }

        // Whether the sound has something to play in any source (Aleph One falls back to the 8-bit source when the
        // 16-bit one has no permutations, and plays nothing for a sound with no code or no permutations)
        public bool HasSound(short soundIndex)
        {
            for (var source = 0; source < soundFile.SourceCount(); source++)
            {
                var definition = soundFile.GetSoundDefinition(source, soundIndex);

                if (definition != null && definition.sound_code != cstypes.NONE && definition.permutations > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
