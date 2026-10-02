using AlephOne;
using ForgePlus.Localization;
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

        public string ResourceForkSource
        {
            get
            {
                if (ResourceFork == null)
                {
                    return null;
                }

                if (resourceForkSidecar == null)
                {
                    return Container == Containers.None
                               ? sourceFileName
                               : Strings.Get(Strings.Common, "Runtime.MacFileForks.Source.InContainer", sourceFileName, ContainerName(Container));
                }

                return resourceForkSidecar == SidecarSuffixes[2]
                           ? Strings.Get(Strings.Common, "Runtime.MacFileForks.Source.NamedFork", sourceFileName)
                           : sourceFileName + resourceForkSidecar;
            }
        }

        // Whether resources can be removed from the resource fork (see SetResourceRemoved)
        public bool CanEditResources { get; private set; }

        // The sidecar's suffix, when the resource fork is a sidecar's (otherwise it's in the file itself)
        private string resourceForkSidecar;

        // The resource fork WriteAround was given to save instead
        private byte[] resourceForkReplacement;

        // Whether WriteAround saves the file without resources
        private bool savesNoResources;

        // Resources (by type and ID) the saved resource fork leaves out
        private readonly HashSet<(uint Type, short Id)> removedResources = new HashSet<(uint Type, short Id)>();

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
        // file's sidecars next to it. The resource fork is saved as it's been edited (see SetResourceRemoved), or as the
        // one given instead (such as one with only some of its resources). An empty one saves the file without resources
        // (its container's resource fork is empty, and it has no sidecars).
        public void WriteAround(string path, byte[] resourceFork = null)
        {
            savesNoResources = resourceFork != null && resourceFork.Length == 0;
            resourceForkReplacement = CanEditResources ? resourceFork : null;

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
                if (savesNoResources)
                {
                    break;
                }

                var contents = sidecar.Key == resourceForkSidecar && CanEditResources ? SavedResourceFork(sidecar.Value) : sidecar.Value;
                if (contents.Length == 0)
                {
                    continue;
                }

                File.WriteAllBytes(path + sidecar.Key, contents);
            }

            resourceForkReplacement = null;
            savesNoResources = false;
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
            // Each candidate's bytes, and sidecar suffix (null for the file itself)
            var candidates = new List<(byte[] Bytes, string Sidecar)>();

            foreach (var sidecar in sidecars)
            {
                if (sidecar.Key != SidecarSuffixes[2])
                {
                    candidates.Add((sidecar.Value, sidecar.Key));
                }
            }

            candidates.Add((fileBytes, null));

            foreach (var sidecar in sidecars)
            {
                if (sidecar.Key == SidecarSuffixes[2])
                {
                    candidates.Add((sidecar.Value, sidecar.Key));
                }
            }

            foreach (var candidate in candidates)
            {
                var forkBytes = ResourceForkOf(candidate.Bytes);
                var resourceFork = MacResourceFork.Read(forkBytes);
                if (resourceFork != null)
                {
                    ResourceFork = resourceFork;
                    resourceForkSidecar = candidate.Sidecar;

                    // A sidecar's resources can be changed while it's just a resource fork (rather than one in a
                    // MacBinary or AppleSingle file of its own), and the file's while it's in the file's container
                    CanEditResources = candidate.Sidecar != null ? forkBytes == candidate.Bytes : Container != Containers.None;
                    return;
                }
            }
        }

        // Leaves the resource out of the saved resource fork, or (for removed: false) keeps it again
        public void SetResourceRemoved(uint type, short id, bool removed)
        {
            if (removed)
            {
                removedResources.Add((type, id));
            }
            else
            {
                removedResources.Remove((type, id));
            }
        }

        public bool IsResourceRemoved(uint type, short id)
        {
            return removedResources.Contains((type, id));
        }

        // The container's resource fork as it's saved: empty without resources, as saved (below) when it's the one Aleph One
        // uses, and otherwise (when a sidecar's is used instead) as it was
        private byte[] ContainerResourceFork(byte[] resourceFork)
        {
            if (savesNoResources)
            {
                return new byte[0];
            }

            return resourceForkSidecar == null ? SavedResourceFork(resourceFork) : resourceFork;
        }

        // The resource fork as it's saved: as it was, or without the resources that were removed
        private byte[] SavedResourceFork(byte[] resourceFork)
        {
            if (resourceForkReplacement != null)
            {
                return resourceForkReplacement;
            }

            return removedResources.Count == 0 ? resourceFork : ResourceFork.Without(removedResources);
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
                    return Strings.Get(Strings.Common, "Runtime.MacFileForks.Container.MacBinary");
                case Containers.AppleSingle:
                    return Strings.Get(Strings.Common, "Runtime.MacFileForks.Container.AppleSingle");
                default:
                    return Strings.Get(Strings.Common, "Runtime.MacFileForks.Container.DataForkOnly");
            }
        }

        // The header as it was, with the saved file's name and data fork length (the Finder's information, such as the
        // file's type, creator and dates, is kept)
        private byte[] BuildMacBinary(byte[] dataFork, string path)
        {
            var resourceFork = ContainerResourceFork(macBinaryResourceFork);
            var header = (byte[]) macBinaryHeader.Clone();

            var name = csstrings.utf8_to_mac_roman(MacFileName(csstrings.mac_roman_to_utf8(Slice(header, 2, header[1])), path));
            var nameLength = Math.Min(name.Length, 63);
            Array.Clear(header, 1, 64);
            header[1] = (byte) nameLength;
            Array.Copy(name, 0, header, 2, nameLength);

            WriteBE32(header, 83, dataFork.Length);
            WriteBE32(header, 87, resourceFork.Length);

            var crc = MacBinaryCrc(header);
            header[124] = (byte) (crc >> 8);
            header[125] = (byte) crc;

            var bytes = new byte[MacBinaryHeaderSize + PaddedTo128(dataFork.Length) + PaddedTo128(resourceFork.Length) + macBinaryTrailer.Length];
            Array.Copy(header, bytes, MacBinaryHeaderSize);
            Array.Copy(dataFork, 0, bytes, MacBinaryHeaderSize, dataFork.Length);

            var resourceStart = MacBinaryHeaderSize + PaddedTo128(dataFork.Length);
            Array.Copy(resourceFork, 0, bytes, resourceStart, resourceFork.Length);
            Array.Copy(macBinaryTrailer, 0, bytes, resourceStart + PaddedTo128(resourceFork.Length), macBinaryTrailer.Length);

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

            var resourceForkIndex = entries.FindIndex(entry => entry.Key == AppleSingleResourceForkId);
            if (resourceForkIndex >= 0)
            {
                entries[resourceForkIndex] = new KeyValuePair<uint, byte[]>(AppleSingleResourceForkId, ContainerResourceFork(entries[resourceForkIndex].Value));
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

    // A resource fork's resources, read as resource_manager's read_map reads them, which can be written again without
    // some of them (keeping the rest, and the fork's other bytes, as they were)
    public class MacResourceFork
    {
        private const int HeaderSize = 16;
        private const int MapHeaderSize = 28;
        private const int TypeEntrySize = 8;
        private const int ReferenceSize = 12;
        private const int NoName = 0xffff;

        private class Resource
        {
            public short Id;
            public int NameOffset;
            public byte Attributes;
            public int DataOffset;
        }

        private readonly byte[] bytes;
        private readonly int dataOffset;
        private readonly int mapOffset;
        private readonly int nameListOffset;

        // Each type's resources (in the order the map lists them)
        private readonly List<KeyValuePair<uint, List<Resource>>> types = new List<KeyValuePair<uint, List<Resource>>>();

        private MacResourceFork(byte[] bytes, int dataOffset, int mapOffset, int nameListOffset)
        {
            this.bytes = bytes;
            this.dataOffset = dataOffset;
            this.mapOffset = mapOffset;
            this.nameListOffset = nameListOffset;
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
                if (bytes.Length < HeaderSize)
                {
                    return null;
                }

                var dataOffset = MacFileForks.ReadBE32(bytes, 0);
                var mapOffset = MacFileForks.ReadBE32(bytes, 4);
                var dataSize = MacFileForks.ReadBE32(bytes, 8);
                var mapSize = MacFileForks.ReadBE32(bytes, 12);

                if (dataOffset < HeaderSize || mapOffset < 0 || (long) dataOffset + dataSize > bytes.Length ||
                    (long) mapOffset + mapSize > bytes.Length || mapSize < MapHeaderSize + 2)
                {
                    return null;
                }

                var typeListOffset = mapOffset + MacFileForks.ReadBE16(bytes, mapOffset + 24);
                var nameListOffset = mapOffset + MacFileForks.ReadBE16(bytes, mapOffset + 26);
                var fork = new MacResourceFork(bytes, dataOffset, mapOffset, nameListOffset);

                var typeCount = (short) MacFileForks.ReadBE16(bytes, typeListOffset) + 1;
                for (var i = 0; i < typeCount; i++)
                {
                    var typeEntry = typeListOffset + 2 + i * TypeEntrySize;
                    var type = (uint) MacFileForks.ReadBE32(bytes, typeEntry);
                    var referenceCount = (short) MacFileForks.ReadBE16(bytes, typeEntry + 4) + 1;
                    var referenceListOffset = typeListOffset + MacFileForks.ReadBE16(bytes, typeEntry + 6);

                    var resources = new List<Resource>(referenceCount);
                    for (var j = 0; j < referenceCount; j++)
                    {
                        var reference = referenceListOffset + j * ReferenceSize;
                        resources.Add(new Resource
                        {
                            Id = (short) MacFileForks.ReadBE16(bytes, reference),
                            NameOffset = MacFileForks.ReadBE16(bytes, reference + 2),
                            Attributes = bytes[reference + 4],
                            DataOffset = MacFileForks.ReadBE32(bytes, reference + 4) & 0x00ffffff,
                        });
                    }

                    fork.types.Add(new KeyValuePair<uint, List<Resource>>(type, resources));
                }

                return fork;
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        public bool Has(uint type, short id)
        {
            return Find(type, id) != null;
        }

        // Null if there's no such resource
        public byte[] Get(uint type, short id)
        {
            var resource = Find(type, id);
            if (resource == null)
            {
                return null;
            }

            var offset = dataOffset + resource.DataOffset;
            var length = MacFileForks.ReadBE32(bytes, offset);
            var data = new byte[length];
            Array.Copy(bytes, offset + 4, data, 0, length);

            return data;
        }

        // Every resource, by type and ID, in the map's order
        public IEnumerable<(uint Type, short Id)> All
        {
            get
            {
                foreach (var type in types)
                {
                    foreach (var resource in type.Value)
                    {
                        yield return (type.Key, resource.Id);
                    }
                }
            }
        }

        // The fork without the resources (by type and ID)
        public byte[] Without(ICollection<(uint Type, short Id)> removed)
        {
            return Rebuild((type, id) => removed.Contains((type, id)) ? (short?) null : id);
        }

        // The fork with the resources newId gives an ID (null leaves one out), each with that ID and its data (or its
        // replacement's, by its type and ID here): the bytes before the data (the header, and what the system and
        // application keep there) as they were, then the resources' data in its order (replacements after), then the
        // map (with its header's handles and attributes as they were), listing the types and resources that are kept
        // in their order, with their names. A type's second resource to get an ID is left out.
        public byte[] Rebuild(Func<uint, short, short?> newId, IDictionary<(uint Type, short Id), byte[]> replacements = null)
        {
            var keptTypes = new List<KeyValuePair<uint, List<KeyValuePair<Resource, short>>>>();
            foreach (var type in types)
            {
                var kept = new List<KeyValuePair<Resource, short>>();
                var ids = new HashSet<short>();

                foreach (var resource in type.Value)
                {
                    var id = newId(type.Key, resource.Id);
                    if (id.HasValue && ids.Add(id.Value))
                    {
                        kept.Add(new KeyValuePair<Resource, short>(resource, id.Value));
                    }
                }

                if (kept.Count > 0)
                {
                    keptTypes.Add(new KeyValuePair<uint, List<KeyValuePair<Resource, short>>>(type.Key, kept));
                }
            }

            bool IsReplaced(uint type, Resource resource)
            {
                return replacements != null && replacements.ContainsKey((type, resource.Id));
            }

            var output = new List<byte>(bytes.Length);
            for (var i = 0; i < dataOffset; i++)
            {
                output.Add(bytes[i]);
            }

            // Data, in its order in the fork (resources that share data share it still)
            var dataOrder = new SortedSet<int>();
            foreach (var type in keptTypes)
            {
                foreach (var resource in type.Value)
                {
                    if (!IsReplaced(type.Key, resource.Key))
                    {
                        dataOrder.Add(resource.Key.DataOffset);
                    }
                }
            }

            var newDataOffsets = new Dictionary<int, int>();
            foreach (var offset in dataOrder)
            {
                newDataOffsets[offset] = output.Count - dataOffset;

                var length = MacFileForks.ReadBE32(bytes, dataOffset + offset);
                for (var i = 0; i < 4 + length; i++)
                {
                    output.Add(bytes[dataOffset + offset + i]);
                }
            }

            var replacedDataOffsets = new Dictionary<Resource, int>();
            foreach (var type in keptTypes)
            {
                foreach (var resource in type.Value)
                {
                    if (IsReplaced(type.Key, resource.Key))
                    {
                        var data = replacements[(type.Key, resource.Key.Id)];
                        replacedDataOffsets[resource.Key] = output.Count - dataOffset;
                        AddBE32(output, data.Length);
                        output.AddRange(data);
                    }
                }
            }

            var dataSize = output.Count - dataOffset;
            var newMapOffset = output.Count;

            // The map's header (its copy of the fork's header and its list offsets are filled in below), its type list,
            // then the types' reference lists
            for (var i = 0; i < MapHeaderSize; i++)
            {
                output.Add(bytes[mapOffset + i]);
            }

            var typeListStart = output.Count;
            AddBE16(output, keptTypes.Count - 1);

            var referenceListOffset = 2 + keptTypes.Count * TypeEntrySize;
            foreach (var type in keptTypes)
            {
                AddBE32(output, (int) type.Key);
                AddBE16(output, type.Value.Count - 1);
                AddBE16(output, referenceListOffset);
                referenceListOffset += type.Value.Count * ReferenceSize;
            }

            var names = new List<byte>();
            foreach (var type in keptTypes)
            {
                foreach (var resource in type.Value)
                {
                    var nameOffset = NoName;
                    if (resource.Key.NameOffset != NoName)
                    {
                        var name = nameListOffset + resource.Key.NameOffset;
                        nameOffset = names.Count;
                        for (var i = 0; i <= bytes[name]; i++)
                        {
                            names.Add(bytes[name + i]);
                        }
                    }

                    var resourceDataOffset = replacedDataOffsets.TryGetValue(resource.Key, out var replacedOffset) ?
                                             replacedOffset :
                                             newDataOffsets[resource.Key.DataOffset];

                    AddBE16(output, resource.Value);
                    AddBE16(output, nameOffset);
                    AddBE32(output, (resource.Key.Attributes << 24) | resourceDataOffset);

                    // The handle, which is only meaningful in memory
                    AddBE32(output, 0);
                }
            }

            var newNameListOffset = output.Count - newMapOffset;
            output.AddRange(names);

            var mapSize = output.Count - newMapOffset;
            var fork = output.ToArray();

            foreach (var headerOffset in new[] { 0, newMapOffset })
            {
                WriteBE32(fork, headerOffset, dataOffset);
                WriteBE32(fork, headerOffset + 4, newMapOffset);
                WriteBE32(fork, headerOffset + 8, dataSize);
                WriteBE32(fork, headerOffset + 12, mapSize);
            }

            WriteBE16(fork, newMapOffset + 24, typeListStart - newMapOffset);
            WriteBE16(fork, newMapOffset + 26, newNameListOffset);

            return fork;
        }

        private Resource Find(uint type, short id)
        {
            foreach (var resourceType in types)
            {
                if (resourceType.Key == type)
                {
                    var resource = resourceType.Value.Find(candidate => candidate.Id == id);
                    if (resource != null)
                    {
                        return resource;
                    }
                }
            }

            return null;
        }

        private static void AddBE16(List<byte> output, int value)
        {
            output.Add((byte) (value >> 8));
            output.Add((byte) value);
        }

        private static void AddBE32(List<byte> output, int value)
        {
            output.Add((byte) (value >> 24));
            output.Add((byte) (value >> 16));
            output.Add((byte) (value >> 8));
            output.Add((byte) value);
        }

        private static void WriteBE16(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte) (value >> 8);
            bytes[offset + 1] = (byte) value;
        }

        private static void WriteBE32(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte) (value >> 24);
            bytes[offset + 1] = (byte) (value >> 16);
            bytes[offset + 2] = (byte) (value >> 8);
            bytes[offset + 3] = (byte) value;
        }
    }
}
