using System;
using System.Collections.Generic;
using static AlephOne.cstypes;

namespace ForgePlus.DataFileIO
{
    // A resource fork's resources, read as resource_manager's read_map reads them, which can be written again without
    // some of them (keeping the rest, and the fork's other bytes, as they were)
    public class MacResourceFork
    {
        public static readonly uint PictType = FOUR_CHARS_TO_INT('P', 'I', 'C', 'T');
        public static readonly uint ClutType = FOUR_CHARS_TO_INT('c', 'l', 'u', 't');
        public static readonly uint SoundType = FOUR_CHARS_TO_INT('s', 'n', 'd', ' ');
        public static readonly uint TextType = FOUR_CHARS_TO_INT('T', 'E', 'X', 'T');

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

        // In the order the map lists them
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

        // Every resource, in the map's order
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

        // Null if the bytes aren't a resource fork
        public static MacResourceFork Read(byte[] bytes)
        {
            try
            {
                if (bytes.Length < HeaderSize)
                {
                    return null;
                }

                var dataOffset = BigEndian.Read32(bytes, 0);
                var mapOffset = BigEndian.Read32(bytes, 4);
                var dataSize = BigEndian.Read32(bytes, 8);
                var mapSize = BigEndian.Read32(bytes, 12);

                if (dataOffset < HeaderSize || mapOffset < 0 || (long) dataOffset + dataSize > bytes.Length ||
                    (long) mapOffset + mapSize > bytes.Length || mapSize < MapHeaderSize + 2)
                {
                    return null;
                }

                var typeListOffset = mapOffset + BigEndian.Read16(bytes, mapOffset + 24);
                var nameListOffset = mapOffset + BigEndian.Read16(bytes, mapOffset + 26);
                var fork = new MacResourceFork(bytes, dataOffset, mapOffset, nameListOffset);

                var typeCount = (short) BigEndian.Read16(bytes, typeListOffset) + 1;
                for (var i = 0; i < typeCount; i++)
                {
                    var typeEntry = typeListOffset + 2 + i * TypeEntrySize;
                    var type = (uint) BigEndian.Read32(bytes, typeEntry);
                    var referenceCount = (short) BigEndian.Read16(bytes, typeEntry + 4) + 1;
                    var referenceListOffset = typeListOffset + BigEndian.Read16(bytes, typeEntry + 6);

                    var resources = new List<Resource>(referenceCount);
                    for (var j = 0; j < referenceCount; j++)
                    {
                        var reference = referenceListOffset + j * ReferenceSize;
                        resources.Add(new Resource
                        {
                            Id = (short) BigEndian.Read16(bytes, reference),
                            NameOffset = BigEndian.Read16(bytes, reference + 2),
                            Attributes = bytes[reference + 4],
                            DataOffset = BigEndian.Read32(bytes, reference + 4) & 0x00ffffff,
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
            var length = BigEndian.Read32(bytes, offset);
            var data = new byte[length];
            Array.Copy(bytes, offset + 4, data, 0, length);

            return data;
        }

        public byte[] Without(ICollection<(uint Type, short Id)> removed)
        {
            return Rebuild((type, id) => removed.Contains((type, id)) ? (short?) null : id);
        }

        // The fork with the resources newId gives an ID (null leaves one out), with their data or its replacement (by
        // original type and ID). Bytes before the data and the map header's handles and attributes are kept as they were.
        // A type's second resource to get the same ID is left out.
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

            // In the fork's order, so resources that share data still share it
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

                var length = BigEndian.Read32(bytes, dataOffset + offset);
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
                        BigEndian.Add32(output, data.Length);
                        output.AddRange(data);
                    }
                }
            }

            var dataSize = output.Count - dataOffset;
            var newMapOffset = output.Count;

            // Its copy of the fork's header and its list offsets are filled in below
            for (var i = 0; i < MapHeaderSize; i++)
            {
                output.Add(bytes[mapOffset + i]);
            }

            var typeListStart = output.Count;
            BigEndian.Add16(output, keptTypes.Count - 1);

            var referenceListOffset = 2 + keptTypes.Count * TypeEntrySize;
            foreach (var type in keptTypes)
            {
                BigEndian.Add32(output, (int) type.Key);
                BigEndian.Add16(output, type.Value.Count - 1);
                BigEndian.Add16(output, referenceListOffset);
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

                    BigEndian.Add16(output, resource.Value);
                    BigEndian.Add16(output, nameOffset);
                    BigEndian.Add32(output, (resource.Key.Attributes << 24) | resourceDataOffset);

                    // The handle, which is only meaningful in memory
                    BigEndian.Add32(output, 0);
                }
            }

            var newNameListOffset = output.Count - newMapOffset;
            output.AddRange(names);

            var mapSize = output.Count - newMapOffset;
            var fork = output.ToArray();

            foreach (var headerOffset in new[] { 0, newMapOffset })
            {
                BigEndian.Write32(fork, headerOffset, dataOffset);
                BigEndian.Write32(fork, headerOffset + 4, newMapOffset);
                BigEndian.Write32(fork, headerOffset + 8, dataSize);
                BigEndian.Write32(fork, headerOffset + 12, mapSize);
            }

            BigEndian.Write16(fork, newMapOffset + 24, typeListStart - newMapOffset);
            BigEndian.Write16(fork, newMapOffset + 26, newNameListOffset);

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
    }
}
