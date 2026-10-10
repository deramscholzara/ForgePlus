// Port of Aleph One: Source_Files/Sound/SoundFile.h, SoundFile.cpp (the sound definitions)
//
// SoundData is a byte array. M2SoundFile doesn't keep the sound file opened (GetSoundData opens it again to load a
// sound's data). GetSoundHeader returns null for a permutation it doesn't have, rather than failing.
using System;
using System.Collections.Generic;
using static AlephOne.cstypes;
using static AlephOne.Packing;
using static AlephOne.sound_definitions;
using static AlephOne.SoundManagerEnums;

namespace AlephOne
{
    public class SoundDefinition
    {
        private static int HeaderSize() { return SIZEOF_sound_definition; }

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
            int count = LoadPermutations ? permutations : Math.Min(permutations, (short) 1);

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
        private const ushort bufferCmd = 0x8051;

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
                if (format != FOUR_CHARS_TO_INT('t', 'w', 'o', 's') || comp_id != -1)
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

        // ForgePlus: from the header's bytes, read from the file's current position
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
            int count = Math.Min(MaximumHeaderSize, file_length - position);
            if (count < 22 || !SoundFile.Read(count, buffer)) return false;

            try
            {
                return Load(buffer);
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        // ForgePlus: the samples as the file has them (16-bit ones big-endian), with signed 8-bit ones made unsigned
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

        // Finds the System 7 header in a 'snd ' resource (a format 1 or 2 resource, whose bufferCmd gives the header's offset)
        public bool Load(LoadedResource rsrc)
        {
            Clear();

            byte[] data = rsrc.GetPointer();
            if (data == null) return false;

            try
            {
                var s = new StreamPointer(data);

                // Get resource format
                StreamToValue(s, out ushort format);
                if (format != 1 && format != 2)
                {
                    // Not ported: logWarning("Unknown sound resource format %d", format)
                    return false;
                }

                // Skip sound data types or reference count
                if (format == 1)
                {
                    StreamToValue(s, out ushort num_data_formats);
                    s.Skip(num_data_formats * 6);
                }
                else if (format == 2)
                {
                    s.Skip(2);
                }

                // Scan sound commands for bufferCmd
                StreamToValue(s, out ushort num_cmds);
                for (int i = 0; i < num_cmds; ++i)
                {
                    StreamToValue(s, out ushort cmd);
                    StreamToValue(s, out ushort param1);
                    StreamToValue(s, out uint param2);

                    if (cmd == bufferCmd)
                    {
                        // ForgePlus: from as much of the largest header as the resource has after the offset
                        int count = Math.Min(MaximumHeaderSize, rsrc.GetLength() - (int) param2);
                        if (count < 22) return false;

                        var buffer = new byte[MaximumHeaderSize];
                        Array.Copy(data, (int) param2, buffer, 0, count);

                        if (Load(buffer))
                        {
                            data_offset += (int) param2;
                            return true;
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // A truncated resource
            }
            catch (ArgumentException)
            {
                // A truncated resource
            }

            return false;
        }

        // ForgePlus: as LoadData(OpenedFile) does, from the resource's start
        public byte[] LoadData(LoadedResource rsrc)
        {
            byte[] data = rsrc.GetPointer();
            if (data == null || data_offset == 0 || length <= 0 || data_offset + length > rsrc.GetLength())
            {
                return null;
            }

            var p = new byte[length];
            Array.Copy(data, data_offset, p, 0, length);

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
        public abstract SoundHeader GetSoundHeader(SoundDefinition definition, int permutation);

        // Null if they can't be loaded
        public abstract byte[] GetSoundData(SoundDefinition definition, short permutation);

        public virtual int SourceCount() { return 1; }
    }

    // Marathon 1's sounds, which are 'snd ' resources, each sound's permutations following it
    public class M1SoundFile : SoundFile
    {
        private static readonly uint SOUND_RESOURCE_TYPE = FOUR_CHARS_TO_INT('s', 'n', 'd', ' ');

        private const int MAXIMUM_PERMUTATIONS_PER_SOUND = 5;

        private readonly OpenedResourceFile resource_file = new OpenedResourceFile();
        private readonly LoadedResource cached_rsrc = new LoadedResource();
        private short cached_sound_code = -1;

        private readonly Dictionary<short, SoundDefinition> definitions = new Dictionary<short, SoundDefinition>();
        private readonly Dictionary<short, SoundHeader> headers = new Dictionary<short, SoundHeader>();

        public override bool Open(FileSpecifier SoundFile)
        {
            Close();
            return SoundFile.Open(resource_file);
        }

        // ForgePlus: whether it has any sounds (any file with resources opens as one)
        public bool HasSounds()
        {
            resource_file.Push();
            int count = resource_manager.count_1_resources(SOUND_RESOURCE_TYPE);
            resource_file.Pop();

            return count > 0;
        }

        public override void Close()
        {
            headers.Clear();
            definitions.Clear();
            cached_sound_code = -1;
            cached_rsrc.Unload();
            resource_file.Close();
        }

        public override SoundDefinition GetSoundDefinition(int source, int sound_index)
        {
            // ForgePlus: resource IDs are 16-bit
            if (sound_index < short.MinValue || sound_index > short.MaxValue) return null;

            if (resource_file.Check(SOUND_RESOURCE_TYPE, (short) sound_index))
            {
                if (!definitions.TryGetValue((short) sound_index, out var definition))
                {
                    definition = new SoundDefinition();
                    definition.behavior_index = 2; // sound_is_loud
                    definition.sound_code = (short) sound_index;
                    // look for permutations
                    definition.permutations = 1;
                    while (resource_file.Check(SOUND_RESOURCE_TYPE, (short) (sound_index + definition.permutations)) && definition.permutations < MAXIMUM_PERMUTATIONS_PER_SOUND)
                    {
                        ++definition.permutations;
                    }

                    definitions.Add((short) sound_index, definition);
                }

                return definition;
            }
            else
            {
                return null;
            }
        }

        public override SoundHeader GetSoundHeader(SoundDefinition definition, int permutation)
        {
            short sound_index = (short) (definition.sound_code + Math.Min(permutation, MAXIMUM_PERMUTATIONS_PER_SOUND));

            if (!headers.TryGetValue(sound_index, out var header))
            {
                LoadResource(sound_index);

                header = new SoundHeader();
                header.Load(cached_rsrc);
                headers.Add(sound_index, header);
            }

            return header;
        }

        public override byte[] GetSoundData(SoundDefinition definition, short permutation)
        {
            short sound_index = (short) (definition.sound_code + Math.Min((int) permutation, MAXIMUM_PERMUTATIONS_PER_SOUND));

            LoadResource(sound_index);

            return GetSoundHeader(definition, permutation).LoadData(cached_rsrc);
        }

        private void LoadResource(short sound_index)
        {
            if (cached_sound_code != sound_index)
            {
                // ForgePlus: one that can't be loaded leaves nothing, rather than the previous sound's resource
                if (!resource_file.Get(SOUND_RESOURCE_TYPE, sound_index, cached_rsrc))
                {
                    cached_rsrc.Unload();
                }

                cached_sound_code = sound_index;
            }
        }
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
                    tag != SOUND_FILE_TAG ||
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

        public override SoundHeader GetSoundHeader(SoundDefinition definition, int permutation)
        {
            return (uint) permutation < (uint) definition.sounds.Count ? definition.sounds[permutation] : null;
        }

        public override byte[] GetSoundData(SoundDefinition definition, short permutation)
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
    }
}
