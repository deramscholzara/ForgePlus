// Port of Aleph One: Source_Files/Sound/SoundFile.h, SoundFile.cpp (the sound definitions)
//
// Not ported: the sounds themselves (SoundHeader, SoundInfo, SoundData, SoundDefinition::Load/LoadData,
// M2SoundFile::GetSoundHeader/GetSoundData), so M2SoundFile::Open reads the definitions but not the sound headers, and
// doesn't keep the file open; and M1SoundFile (Marathon 1's resource-fork sounds).
using System.Collections.Generic;
using static AlephOne.Packing;
using static AlephOne.sound_definitions;

namespace AlephOne
{
    public class SoundDefinition
    {
        public const int MAXIMUM_PERMUTATIONS_PER_SOUND = 5;

        private static int HeaderSize() { return 64; }

        public short sound_code;
        public short behavior_index;
        public ushort flags;

        public ushort chance; // play sound if AbsRandom() >= chance

        /* if low_pitch==0 use FIXED_ONE; if high_pitch==0 use low pitch; else choose in [low_pitch, high_pitch] */
        public int low_pitch, high_pitch; // _fixed

        /* filled in later */
        public short permutations;
        public ushort permutations_played;

        public int group_offset, single_length, total_length; // magic numbers necessary to load sounds
        public List<int> sound_offsets = new List<int>(); // zero-based from group offset

        public SoundDefinition()
        {
            sound_code = 0;
            behavior_index = 1;
            flags = 0;
            chance = 0;
            low_pitch = 0;
            high_pitch = 0;
            permutations = 1;
            permutations_played = 0;
            group_offset = 0;
            single_length = 0;
            total_length = 0;
        }

        public bool Unpack(OpenedFile SoundFile)
        {
            if (!SoundFile.IsOpen()) return false;

            var headerBuffer = new byte[HeaderSize()];
            if (!SoundFile.Read(headerBuffer.Length, headerBuffer))
                return false;

            return Unpack(new StreamPointer(headerBuffer));
        }

        public bool Unpack(StreamPointer header)
        {
            StreamToValue(header, out sound_code);

            StreamToValue(header, out behavior_index);
            StreamToValue(header, out flags);

            StreamToValue(header, out chance);

            StreamToValue(header, out low_pitch);
            StreamToValue(header, out high_pitch);

            StreamToValue(header, out permutations);
            StreamToValue(header, out permutations_played);
            StreamToValue(header, out group_offset);
            StreamToValue(header, out single_length);
            StreamToValue(header, out total_length);

            sound_offsets.Clear();
            for (int i = 0; i < MAXIMUM_PERMUTATIONS_PER_SOUND; i++)
            {
                StreamToValue(header, out int sound_offset);
                sound_offsets.Add(sound_offset);
            }

            header.Skip(4); //uint32 last_played
            header.Skip(4 * 2);

            return true;
        }
    }

    public abstract class SoundFile
    {
        public abstract bool Open(FileSpecifier SoundFile);
        public abstract void Close();
        public abstract SoundDefinition GetSoundDefinition(int source, int sound_index);

        public virtual int SourceCount() { return 1; }
    }

    public class M2SoundFile : SoundFile
    {
        private int version;
        private uint tag;

        private short source_count;
        private short sound_count;

        private const int v1Unused = 124;

        private readonly List<List<SoundDefinition>> sound_definitions = new List<List<SoundDefinition>>();

        private static int HeaderSize() { return SIZEOF_sound_file_header; }

        public override bool Open(FileSpecifier SoundFileSpec)
        {
            Close();

            var sound_file = new OpenedFile();

            if (!SoundFileSpec.Open(sound_file, false)) return false;

            try
            {
                var headerBuffer = new byte[HeaderSize()];

                if (!sound_file.Read(headerBuffer.Length, headerBuffer))
                    return false;

                var header = new StreamPointer(headerBuffer);
                StreamToValue(header, out version);
                StreamToValue(header, out tag);
                StreamToValue(header, out source_count);
                StreamToValue(header, out sound_count);
                header.Skip(v1Unused * 2);

                if ((version != 0 && version != 1) ||
                    tag != cstypes.FOUR_CHARS_TO_INT('s', 'n', 'd', '2') ||
                    sound_count < 0 ||
                    source_count < 0)
                {
                    return false;
                }

                if (sound_count == 0)
                {
                    sound_count = source_count;
                    source_count = 1;
                }

                // load the definitions
                for (int source = 0; source < source_count; source++)
                {
                    var definitions = new List<SoundDefinition>(sound_count);
                    sound_definitions.Add(definitions);
                    for (int i = 0; i < sound_count; i++)
                    {
                        var definition = new SoundDefinition();
                        definitions.Add(definition);
                        if (!definition.Unpack(sound_file))
                        {
                            Close();
                            return false;
                        }
                    }
                }

                // Not ported: loading all the sound headers, and keeping the sound file opened

                return true;
            }
            finally
            {
                sound_file.Close();
            }
        }

        public override void Close()
        {
            sound_definitions.Clear();
        }

        public override SoundDefinition GetSoundDefinition(int source, int sound_index)
        {
            // Both are compared as size_t, so negative ones are out of range too
            if ((uint) source < (uint) sound_definitions.Count && (uint) sound_index < (uint) sound_definitions[source].Count)
                return sound_definitions[source][sound_index];
            else
                return null;
        }

        public override int SourceCount() { return source_count; }

        // How many sounds each source has (Aleph One keeps this private, and bounds lookups by it)
        public int SoundCount() { return sound_count; }
    }
}
