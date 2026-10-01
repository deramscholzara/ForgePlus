using AlephOne;
using System;
using System.Collections.Generic;
using System.IO;
using static AlephOne.SDL_rwops;

namespace ForgePlus.DataFileIO
{
    // What a Mac file has besides its data fork (where a map's wads are): its resource fork (with the scenario's
    // pictures, color tables and level script) and the Finder's information about it. They're found where Aleph One
    // finds them: in the file itself, when it's MacBinary or AppleSingle (FileHandler and resource_manager read those
    // transparently), and next to it, as <name>.rsrc, <name>.resources or (on macOS) its named fork
    // (resource_manager.open_res_file).
    // Saving a map writes only its data fork, so these carry the rest through to the saved file, in the same forms.
    public class MacFileForks
    {
        public enum Containers
        {
            // Only the data fork
            None,
            MacBinary,
            AppleSingle,
        }

        private const int MacBinaryHeaderSize = 128;
        private const uint AppleSingleMagic = 0x00051600;
        private const int AppleSingleHeaderSize = 26;
        private const int AppleSingleEntrySize = 12;
        private const uint AppleSingleDataForkId = 1;
        private const uint AppleSingleResourceForkId = 2;
        private const uint AppleSingleRealNameId = 3;

        // Next to the file, in the order Aleph One looks for them (it looks in the file itself after the first two)
        private static readonly string[] SidecarSuffixes = { ".rsrc", ".resources", "/..namedfork/rsrc" };

        private string sourceFileName;

        // MacBinary: its header, resource fork, and whatever follows the forks (such as a Get Info comment)
        private byte[] macBinaryHeader;
        private byte[] macBinaryResourceFork;
        private byte[] macBinaryTrailer;

        // AppleSingle: its header (up to its entry count), and its entries in order (the data fork's without its bytes)
        private byte[] appleSingleHeader;
        private readonly List<KeyValuePair<uint, byte[]>> appleSingleEntries = new List<KeyValuePair<uint, byte[]>>();

        // Each sidecar file's suffix and contents
        private readonly List<KeyValuePair<string, byte[]>> sidecars = new List<KeyValuePair<string, byte[]>>();

        public Containers Container { get; private set; }

        // The resource fork Aleph One uses for the file's resources (or null, if it has none), and where it is
        public MacResourceFork ResourceFork { get; private set; }

        public string ResourceForkSource { get; private set; }

        public static MacFileForks Read(string path)
        {
            var forks = new MacFileForks { sourceFileName = Path.GetFileName(path) };
            var bytes = File.ReadAllBytes(path);

            if (IsAppleSingle(bytes))
            {
                forks.ReadAppleSingle(bytes);
            }
            else if (IsMacBinary(bytes, out var dataLength, out var resourceLength))
            {
                forks.ReadMacBinary(bytes, dataLength, resourceLength);
            }

            foreach (var suffix in SidecarSuffixes)
            {
                var sidecarBytes = ReadSidecar(path + suffix);
                if (sidecarBytes != null)
                {
                    forks.sidecars.Add(new KeyValuePair<string, byte[]>(suffix, sidecarBytes));
                }
            }

            forks.FindResourceFork(bytes);

            return forks;
        }

        // Puts the data fork that saving wrote at the path into this file's container (if it had one), and writes this
        // file's sidecars next to it
        public void WriteAround(string path)
        {
            if (Container != Containers.None)
            {
                var dataFork = File.ReadAllBytes(path);
                var bytes = Container == Containers.MacBinary ? BuildMacBinary(dataFork, path) : BuildAppleSingle(dataFork, path);

                var temporaryPath = path + ".forks";
                File.WriteAllBytes(temporaryPath, bytes);
                File.Replace(temporaryPath, path, null);
            }

            foreach (var sidecar in sidecars)
            {
                File.WriteAllBytes(path + sidecar.Key, sidecar.Value);
            }
        }

        // As resource_manager.is_applesingle does, for the data fork (as FileHandler opens a map)
        private static bool IsAppleSingle(byte[] bytes)
        {
            var file = SDL_RWFromConstMem(bytes, bytes.Length);
            var isAppleSingle = resource_manager.is_applesingle(file, false, out _, out _);
            SDL_RWclose(file);

            return isAppleSingle;
        }

        private static bool IsMacBinary(byte[] bytes, out int dataLength, out int resourceLength)
        {
            var file = SDL_RWFromConstMem(bytes, bytes.Length);
            var isMacBinary = resource_manager.is_macbinary(file, out dataLength, out resourceLength);
            SDL_RWclose(file);

            return isMacBinary;
        }

        private static byte[] ReadSidecar(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    return bytes.Length > 0 ? bytes : null;
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is NotSupportedException)
            {
            }

            return null;
        }

        private void ReadMacBinary(byte[] bytes, int dataLength, int resourceLength)
        {
            var resourceStart = MacBinaryHeaderSize + PaddedTo128(dataLength);
            var trailerStart = Math.Min(bytes.Length, resourceStart + PaddedTo128(resourceLength));

            Container = Containers.MacBinary;
            macBinaryHeader = Slice(bytes, 0, MacBinaryHeaderSize);
            macBinaryResourceFork = Slice(bytes, resourceStart, resourceLength);
            macBinaryTrailer = Slice(bytes, trailerStart, bytes.Length - trailerStart);
        }

        private void ReadAppleSingle(byte[] bytes)
        {
            var entryCount = ReadBE16(bytes, AppleSingleHeaderSize - 2);

            Container = Containers.AppleSingle;
            appleSingleHeader = Slice(bytes, 0, AppleSingleHeaderSize - 2);

            for (var i = 0; i < entryCount; i++)
            {
                var entry = AppleSingleHeaderSize + i * AppleSingleEntrySize;
                var id = (uint) ReadBE32(bytes, entry);
                var contents = id == AppleSingleDataForkId ? null : Slice(bytes, ReadBE32(bytes, entry + 4), ReadBE32(bytes, entry + 8));

                appleSingleEntries.Add(new KeyValuePair<uint, byte[]>(id, contents));
            }
        }

        // In the order resource_manager.open_res_file looks for it
        private void FindResourceFork(byte[] fileBytes)
        {
            var candidates = new List<KeyValuePair<string, byte[]>>();

            foreach (var sidecar in sidecars)
            {
                if (sidecar.Key != SidecarSuffixes[2])
                {
                    candidates.Add(new KeyValuePair<string, byte[]>(sourceFileName + sidecar.Key, sidecar.Value));
                }
            }

            candidates.Add(new KeyValuePair<string, byte[]>(Container == Containers.None ? sourceFileName : $"{sourceFileName} ({ContainerName(Container)})", fileBytes));

            foreach (var sidecar in sidecars)
            {
                if (sidecar.Key == SidecarSuffixes[2])
                {
                    candidates.Add(new KeyValuePair<string, byte[]>($"{sourceFileName} (named fork)", sidecar.Value));
                }
            }

            foreach (var candidate in candidates)
            {
                var resourceFork = MacResourceFork.Read(ResourceForkOf(candidate.Value));
                if (resourceFork != null)
                {
                    ResourceFork = resourceFork;
                    ResourceForkSource = candidate.Key;
                    return;
                }
            }
        }

        // The resource fork in a file's bytes, as resource_manager's read_map finds it
        private static byte[] ResourceForkOf(byte[] bytes)
        {
            var file = SDL_RWFromConstMem(bytes, bytes.Length);

            try
            {
                if (resource_manager.is_applesingle(file, true, out var offset, out var length))
                {
                    return Slice(bytes, offset, length);
                }

                if (resource_manager.is_macbinary(file, out var dataLength, out var resourceLength))
                {
                    return Slice(bytes, MacBinaryHeaderSize + PaddedTo128(dataLength), resourceLength);
                }

                return bytes;
            }
            finally
            {
                SDL_RWclose(file);
            }
        }

        public static string ContainerName(Containers container)
        {
            switch (container)
            {
                case Containers.MacBinary:
                    return "MacBinary";
                case Containers.AppleSingle:
                    return "AppleSingle";
                default:
                    return "Data Fork Only";
            }
        }

        // The header as it was, with the saved file's name and data fork length (the Finder's information, such as the
        // file's type, creator and dates, is kept)
        private byte[] BuildMacBinary(byte[] dataFork, string path)
        {
            var header = (byte[]) macBinaryHeader.Clone();

            var name = csstrings.utf8_to_mac_roman(MacFileName(csstrings.mac_roman_to_utf8(Slice(header, 2, header[1])), path));
            var nameLength = Math.Min(name.Length, 63);
            Array.Clear(header, 1, 64);
            header[1] = (byte) nameLength;
            Array.Copy(name, 0, header, 2, nameLength);

            WriteBE32(header, 83, dataFork.Length);
            WriteBE32(header, 87, macBinaryResourceFork.Length);

            var crc = MacBinaryCrc(header);
            header[124] = (byte) (crc >> 8);
            header[125] = (byte) crc;

            var bytes = new byte[MacBinaryHeaderSize + PaddedTo128(dataFork.Length) + PaddedTo128(macBinaryResourceFork.Length) + macBinaryTrailer.Length];
            Array.Copy(header, bytes, MacBinaryHeaderSize);
            Array.Copy(dataFork, 0, bytes, MacBinaryHeaderSize, dataFork.Length);

            var resourceStart = MacBinaryHeaderSize + PaddedTo128(dataFork.Length);
            Array.Copy(macBinaryResourceFork, 0, bytes, resourceStart, macBinaryResourceFork.Length);
            Array.Copy(macBinaryTrailer, 0, bytes, resourceStart + PaddedTo128(macBinaryResourceFork.Length), macBinaryTrailer.Length);

            return bytes;
        }

        // The entries in their order, laid out one after another, with the saved data fork and file name
        private byte[] BuildAppleSingle(byte[] dataFork, string path)
        {
            var entries = new List<KeyValuePair<uint, byte[]>>(appleSingleEntries);
            var dataForkIndex = entries.FindIndex(entry => entry.Key == AppleSingleDataForkId);
            if (dataForkIndex < 0)
            {
                entries.Add(new KeyValuePair<uint, byte[]>(AppleSingleDataForkId, dataFork));
            }
            else
            {
                entries[dataForkIndex] = new KeyValuePair<uint, byte[]>(AppleSingleDataForkId, dataFork);
            }

            var nameIndex = entries.FindIndex(entry => entry.Key == AppleSingleRealNameId);
            if (nameIndex >= 0)
            {
                var name = csstrings.utf8_to_mac_roman(MacFileName(csstrings.mac_roman_to_utf8(entries[nameIndex].Value), path));
                entries[nameIndex] = new KeyValuePair<uint, byte[]>(AppleSingleRealNameId, name);
            }

            var length = AppleSingleHeaderSize + entries.Count * AppleSingleEntrySize;
            foreach (var entry in entries)
            {
                length += entry.Value.Length;
            }

            var bytes = new byte[length];
            Array.Copy(appleSingleHeader, bytes, appleSingleHeader.Length);
            bytes[AppleSingleHeaderSize - 2] = (byte) (entries.Count >> 8);
            bytes[AppleSingleHeaderSize - 1] = (byte) entries.Count;

            var offset = AppleSingleHeaderSize + entries.Count * AppleSingleEntrySize;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = AppleSingleHeaderSize + i * AppleSingleEntrySize;
                WriteBE32(bytes, entry, (int) entries[i].Key);
                WriteBE32(bytes, entry + 4, offset);
                WriteBE32(bytes, entry + 8, entries[i].Value.Length);

                Array.Copy(entries[i].Value, 0, bytes, offset, entries[i].Value.Length);
                offset += entries[i].Value.Length;
            }

            return bytes;
        }

        // The Mac file's name for the saved file: its own name if it's saved under its file's name, and otherwise the
        // saved file's name (with its extension only if the Mac name had its file's, as "Map" for "Map.sceA" doesn't)
        private string MacFileName(string originalName, string path)
        {
            var savedFileName = Path.GetFileName(path);

            if (string.Equals(savedFileName, sourceFileName, StringComparison.OrdinalIgnoreCase))
            {
                return originalName;
            }

            return originalName == sourceFileName ? savedFileName : Path.GetFileNameWithoutExtension(path);
        }

        // As resource_manager.is_macbinary checks it
        private static ushort MacBinaryCrc(byte[] header)
        {
            ushort crc = 0;
            for (var i = 0; i < 124; i++)
            {
                var data = (ushort) (header[i] << 8);
                for (var j = 0; j < 8; j++)
                {
                    if (((data ^ crc) & 0x8000) != 0)
                    {
                        crc = (ushort) ((crc << 1) ^ 0x1021);
                    }
                    else
                    {
                        crc <<= 1;
                    }

                    data <<= 1;
                }
            }

            return crc;
        }

        private static int PaddedTo128(int length)
        {
            return (length + 0x7f) & ~0x7f;
        }

        private static byte[] Slice(byte[] bytes, int offset, int length)
        {
            offset = Math.Clamp(offset, 0, bytes.Length);
            length = Math.Clamp(length, 0, bytes.Length - offset);

            var slice = new byte[length];
            Array.Copy(bytes, offset, slice, 0, length);

            return slice;
        }

        internal static int ReadBE32(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        internal static int ReadBE16(byte[] bytes, int offset)
        {
            return (bytes[offset] << 8) | bytes[offset + 1];
        }

        private static void WriteBE32(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte) (value >> 24);
            bytes[offset + 1] = (byte) (value >> 16);
            bytes[offset + 2] = (byte) (value >> 8);
            bytes[offset + 3] = (byte) value;
        }
    }

    // A resource fork's resources, read as resource_manager's read_map reads them (for showing what a map file has)
    public class MacResourceFork
    {
        private readonly byte[] bytes;
        private readonly int dataOffset;

        // Each type's resources' IDs and offsets (in the order the map lists them)
        private readonly List<KeyValuePair<uint, List<KeyValuePair<short, int>>>> types = new List<KeyValuePair<uint, List<KeyValuePair<short, int>>>>();

        private MacResourceFork(byte[] bytes, int dataOffset)
        {
            this.bytes = bytes;
            this.dataOffset = dataOffset;
        }

        // Each type, with how many resources of it there are
        public IEnumerable<KeyValuePair<uint, int>> Types
        {
            get
            {
                foreach (var type in types)
                {
                    yield return new KeyValuePair<uint, int>(type.Key, type.Value.Count);
                }
            }
        }

        // Null if the bytes aren't a resource fork
        public static MacResourceFork Read(byte[] bytes)
        {
            try
            {
                if (bytes.Length < 16)
                {
                    return null;
                }

                var dataOffset = MacFileForks.ReadBE32(bytes, 0);
                var mapOffset = MacFileForks.ReadBE32(bytes, 4);
                var dataSize = MacFileForks.ReadBE32(bytes, 8);
                var mapSize = MacFileForks.ReadBE32(bytes, 12);

                if (dataOffset < 0 || mapOffset < 0 || (long) dataOffset + dataSize > bytes.Length || (long) mapOffset + mapSize > bytes.Length || mapSize < 30)
                {
                    return null;
                }

                var fork = new MacResourceFork(bytes, dataOffset);

                var typeListOffset = mapOffset + MacFileForks.ReadBE16(bytes, mapOffset + 24);
                var typeCount = (short) MacFileForks.ReadBE16(bytes, typeListOffset) + 1;

                for (var i = 0; i < typeCount; i++)
                {
                    var typeEntry = typeListOffset + 2 + i * 8;
                    var type = (uint) MacFileForks.ReadBE32(bytes, typeEntry);
                    var referenceCount = (short) MacFileForks.ReadBE16(bytes, typeEntry + 4) + 1;
                    var referenceListOffset = typeListOffset + MacFileForks.ReadBE16(bytes, typeEntry + 6);

                    var resources = new List<KeyValuePair<short, int>>(referenceCount);
                    for (var j = 0; j < referenceCount; j++)
                    {
                        var reference = referenceListOffset + j * 12;
                        var id = (short) MacFileForks.ReadBE16(bytes, reference);
                        var resourceOffset = MacFileForks.ReadBE32(bytes, reference + 4) & 0x00ffffff;

                        resources.Add(new KeyValuePair<short, int>(id, resourceOffset));
                    }

                    fork.types.Add(new KeyValuePair<uint, List<KeyValuePair<short, int>>>(type, resources));
                }

                return fork;
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        // Null if there's no such resource
        public byte[] Get(uint type, short id)
        {
            foreach (var resourceType in types)
            {
                if (resourceType.Key != type)
                {
                    continue;
                }

                foreach (var resource in resourceType.Value)
                {
                    if (resource.Key == id)
                    {
                        var offset = dataOffset + resource.Value;
                        var length = MacFileForks.ReadBE32(bytes, offset);
                        var data = new byte[length];
                        Array.Copy(bytes, offset + 4, data, 0, length);

                        return data;
                    }
                }
            }

            return null;
        }
    }
}
