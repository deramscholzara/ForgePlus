// Port of Aleph One: Source_Files/Sound/SoundFile.h, SoundFile.cpp (the sound definitions)
//
// SoundData is a byte array. M2SoundFile doesn't keep the sound file opened (GetSoundData opens it again to load a
// sound's data).
//
// Not ported: M2SoundFile::GetSoundHeader, and M1SoundFile (Marathon 1's resource-fork sounds).
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

        public bool Load(OpenedFile SoundFile, bool LoadPermutations)
        {
            if (!SoundFile.IsOpen()) return false;

            sounds.Clear();
            int count = LoadPermutations ? permutations : System.Math.Min(permutations, (short) 1);

            for (int i = 0; i < count; i++)
            {
                var sound = new SoundHeader();
                sounds.Add(sound);

                if (!SoundFile.SetPosition(group_offset + sound_offsets[i])
                    || !sound.Load(SoundFile))
                {
                    sounds.Clear();
                    return false;
                }
            }

            return true;
        }

        public byte[] LoadData(OpenedFile SoundFile, short permutation)
        {
            if (!SoundFile.IsOpen())
            {
                return null;
            }

            if (!SoundFile.SetPosition(group_offset + sound_offsets[permutation]))
            {
                return null;
            }

            if (permutation >= sounds.Count)
            {
                return null;
            }

            return sounds[permutation].LoadData(SoundFile);
        }

        // The permutations' headers (loaded by Load)
        public readonly List<SoundHeader> sounds = new List<SoundHeader>();
    }

    public enum AudioFormat
    {
        _8_bit,
        _16_bit,
        _32_float,
    }

    public class SoundInfo
    {
        public AudioFormat audio_format = AudioFormat._8_bit;
        public bool stereo = false;
        public bool little_endian = false;
        public int bytes_per_frame = 1;
        public int loop_start = 0;
        public int loop_end = 0;
        public uint rate = 0; // _fixed (16.16)
        public int length = 0;
    }

    // A System 7 sound header (standard, extended or compressed) and its samples
    public class SoundHeader : SoundInfo
    {
        private const byte stdSH = 0x00; // standard sound header
        private const byte extSH = 0xFF; // extended sound header
        private const byte cmpSH = 0xFE; // compressed sound header

        // The largest header (an extended or compressed one)
        private const int MaximumHeaderSize = 64;

        private int data_offset = 0;
        private bool signed_8bits = false;

        public int Length() { return length; }

        public void Clear() { length = 0; }

        private bool UnpackStandardSystem7Header(StreamPointer header)
        {
            bytes_per_frame = 1;
            audio_format = AudioFormat._8_bit;
            stereo = false;
            little_endian = false;
            header.Skip(4); // sample pointer
            StreamToValue(header, out length);
            StreamToValue(header, out rate);
            StreamToValue(header, out loop_start);
            StreamToValue(header, out loop_end);

            return true;
        }

        private bool UnpackExtendedSystem7Header(StreamPointer header)
        {
            header.Skip(4); // sample pointer
            StreamToValue(header, out int num_channels);
            stereo = (num_channels == 2);
            StreamToValue(header, out rate);
            StreamToValue(header, out loop_start);
            StreamToValue(header, out loop_end);
            StreamToValue(header, out byte header_type);
            header.Skip(1); // baseFrequency
            StreamToValue(header, out int num_frames);

            if (header_type == 0xfe)
            {
                header.Skip(10); // AIFF rate
                header.Skip(4); // marker chunk
                StreamToValue(header, out uint format);
                header.Skip(4 * 3); // future use, ptr, ptr
                StreamToValue(header, out short comp_id);
                if (format != cstypes.FOUR_CHARS_TO_INT('t', 'w', 'o', 's') || comp_id != -1)
                {
                    return false;
                }
                signed_8bits = true;
                header.Skip(4);
            }
            else
            {
                header.Skip(22);
            }

            StreamToValue(header, out short sample_size);

            audio_format = sample_size == 16 ? AudioFormat._16_bit : AudioFormat._8_bit;
            bytes_per_frame = (audio_format == AudioFormat._16_bit ? 2 : 1) * (stereo ? 2 : 1);

            length = num_frames * bytes_per_frame;
            little_endian = false;

            return true;
        }

        // ForgePlus: from the header's bytes, as read from the file's current position (the stream Aleph One reads from)
        private bool Load(byte[] buffer)
        {
            Clear();

            byte encoding = buffer[20];
            var s = new StreamPointer(buffer);

            switch (encoding)
            {
                case stdSH:
                    if (UnpackStandardSystem7Header(s))
                    {
                        data_offset = 22;
                        return true;
                    }
                    break;
                case extSH:
                case cmpSH:
                    if (UnpackExtendedSystem7Header(s))
                    {
                        data_offset = 64;
                        return true;
                    }
                    break;
            }

            return false;
        }

        public bool Load(OpenedFile SoundFile)
        {
            // ForgePlus: as much of the largest header as the file has
            if (!SoundFile.GetPosition(out int position) || !SoundFile.GetLength(out int file_length)) return false;

            var buffer = new byte[MaximumHeaderSize];
            int count = System.Math.Min(MaximumHeaderSize, file_length - position);
            if (count < 22 || !SoundFile.Read(count, buffer)) return false;

            try
            {
                return Load(buffer);
            }
            catch (System.IndexOutOfRangeException)
            {
                return false;
            }
        }

        // ForgePlus: the samples as the file has them: 8-bit ones unsigned (as signed ones are converted to), and
        // 16-bit ones big-endian (as little_endian says), for the caller to read (rather than swapped to the platform's)
        public byte[] LoadData(OpenedFile SoundFile)
        {
            if (data_offset == 0 || length <= 0)
            {
                return null;
            }

            if (!SoundFile.GetPosition(out int position) || !SoundFile.SetPosition(position + data_offset))
            {
                return null;
            }

            var p = new byte[length];
            if (!SoundFile.Read(length, p))
            {
                return null;
            }

            if (audio_format == AudioFormat._8_bit && signed_8bits)
            {
                ConvertSignedToUnsignedByte(p, length);
            }

            return p;
        }

        private static void ConvertSignedToUnsignedByte(byte[] data, int length)
        {
            for (int i = 0; i < length; i++)
            {
                data[i] = (byte) ((sbyte) data[i] + 128);
            }
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

        private FileSpecifier sound_file_spec;

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

                // load all the headers
                for (int source = 0; source < source_count; ++source)
                {
                    for (int i = 0; i < sound_count; ++i)
                    {
                        sound_definitions[source][i].Load(sound_file, true);
                    }
                }

                // ForgePlus: rather than keeping the sound file opened, it's opened again to load a sound's data
                sound_file_spec = SoundFileSpec;

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

        // The permutation's samples (see SoundHeader.LoadData), or null if they can't be loaded
        public byte[] GetSoundData(SoundDefinition definition, short permutation)
        {
            if (sound_file_spec == null) return null;

            var sound_file = new OpenedFile();
            if (!sound_file_spec.Open(sound_file, false)) return null;

            try
            {
                return definition.LoadData(sound_file, permutation);
            }
            finally
            {
                sound_file.Close();
            }
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
