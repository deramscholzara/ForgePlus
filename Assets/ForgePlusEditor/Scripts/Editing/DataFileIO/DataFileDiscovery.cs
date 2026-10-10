using AlephOne;
using ForgePlus.DataFileIO.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.SDL_rwops;

namespace ForgePlus.DataFileIO
{
    // Finds the first Maps, Shapes, Physics and Sounds files in a folder, searching it one depth at a time (shallowest
    // first). A file is identified by its extension (Marathon 2 and Infinity's, or Marathon 1's), or (without one, as Mac
    // files often are) by its contents: its data fork's header, or (for Marathon 1's shapes and sounds, which are kept as
    // resources) its resource fork's resources.
    [NoAutoStaticsCleanup]
    public static class DataFileDiscovery
    {
        public static readonly DataFileTypes[] SearchedTypes = { DataFileTypes.Maps, DataFileTypes.Shapes, DataFileTypes.Physics, DataFileTypes.Sounds };

        private const int MacBinaryHeaderSize = 128;

        // Enough for a MacBinary header, or an AppleSingle header and its entries
        private const int ContainerProbeSize = 1024;

        // wad.h: wad_header
        private const int WadHeaderSize = 128;
        private const int MaximumWadVersion = 4;
        private const int OldDirectoryEntrySize = 8;
        private const int OldEntryHeaderSize = 12;
        private const int MaximumWadTags = 256;

        // shape_definitions.h: MAXIMUM_COLLECTIONS collection_headers of 32 bytes, and COLLECTION_VERSION
        private const int ShapesCollectionCount = 32;
        private const int ShapesCollectionHeaderSize = 32;
        private const int ShapesCollectionVersion = 3;

        // sound_definitions.h: sound_file_header
        private const string SoundsFileTag = "snd2";

        // Geometry every level has, and the physics models (Marathon 2 and Infinity's, then Marathon's)
        private static readonly HashSet<string> MapTags = new HashSet<string> { "PNTS", "EPNT", "LINS", "POLY", "SIDS" };
        private static readonly HashSet<string> M1PhysicsTags = new HashSet<string> { "mons", "effe", "proj", "phys", "weap" };
        private static readonly HashSet<string> PhysicsTags = new HashSet<string>(M1PhysicsTags) { "MNpx", "FXpx", "PRpx", "PXpx", "WPpx" };

        // import_definitions.cpp: a Marathon 1 physics file's chunks (a tag, 4 unused bytes, and a count and size of
        // definitions), up to one for each tag
        private const int M1PhysicsChunkHeaderSize = 12;

        // shapes.cpp: Marathon 1 shapes files keep collections in .256 resources (128 + the collection), and are
        // recognized by the first. Marathon 1 sounds files are 'snd ' resources, which applications (with code), and
        // images and scenario resource files (with pictures and terminals) can have too.
        private static readonly uint M1CollectionResourceType = cstypes.FOUR_CHARS_TO_INT('.', '2', '5', '6');
        private const short M1FirstCollectionResourceId = 128;
        private static readonly uint[] NotSoundsResourceTypes =
        {
            cstypes.FOUR_CHARS_TO_INT('C', 'O', 'D', 'E'),
            MacResourceFork.PictType,
            MacResourceFork.ClutType,
            MacResourceFork.TextType,
            cstypes.FOUR_CHARS_TO_INT('t', 'e', 'r', 'm'),
        };

        // resource_manager.cpp: the sidecar files a resource fork can be in
        private static readonly string[] ResourceForkSidecarSuffixes = { ".rsrc", ".resources" };

        // A resource fork's header: its data's and map's offsets and lengths
        private const int ResourceForkHeaderSize = 16;

        // A saved game's wads also have a level's geometry, and its players. Marathon 1's levels have their players too
        // (as game_wad.cpp's Marathon 1 data version shows), but there are no saved games with its data version.
        private const string SavedGameTag = "plyr";
        private const int M1DataVersion = 0;

        // The first file found of each type (types that aren't found are left out)
        public static Dictionary<DataFileTypes, string> Find(string rootDirectory)
        {
            var found = new Dictionary<DataFileTypes, string>();
            var directories = new List<string> { rootDirectory };

            while (directories.Count > 0 && found.Count < SearchedTypes.Length)
            {
                var files = directories.SelectMany(EnumerateFiles).ToList();

                // At each depth, files named for their type come before files identified by their contents
                foreach (var file in files)
                {
                    if (TryGetTypeFromExtension(file, out var type))
                    {
                        Add(found, type, file);
                    }
                }

                foreach (var file in files)
                {
                    if (found.Count < SearchedTypes.Length && !HasExtension(file) && TryGetTypeFromContents(file, out var type))
                    {
                        Add(found, type, file);
                    }
                }

                directories = directories.SelectMany(EnumerateDirectories).ToList();
            }

            return found;
        }

        private static void Add(Dictionary<DataFileTypes, string> found, DataFileTypes type, string file)
        {
            if (!found.ContainsKey(type))
            {
                found[type] = file;
            }
        }

        // In name order, as file browsers list them (so "Map.sceA" comes before "Map(backup).sceA")
        private static IEnumerable<string> EnumerateFiles(string directory)
        {
            try
            {
                return Directory.GetFiles(directory)
                                .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
                                .ThenBy(Path.GetExtension, StringComparer.OrdinalIgnoreCase)
                                .ToList();
            }
            catch (Exception exception) when (IsAccessException(exception))
            {
                return Enumerable.Empty<string>();
            }
        }

        // Not through links, which could lead back up the folder
        private static IEnumerable<string> EnumerateDirectories(string directory)
        {
            try
            {
                return Directory.GetDirectories(directory)
                                .Where(subdirectory => (File.GetAttributes(subdirectory) & FileAttributes.ReparsePoint) == 0)
                                .OrderBy(subdirectory => subdirectory, StringComparer.OrdinalIgnoreCase)
                                .ToList();
            }
            catch (Exception exception) when (IsAccessException(exception))
            {
                return Enumerable.Empty<string>();
            }
        }

        private static bool TryGetTypeFromExtension(string file, out DataFileTypes type)
        {
            var extension = Path.GetExtension(file);

            foreach (var searchedType in SearchedTypes)
            {
                if (searchedType.FileExtensions().Any(typeExtension => string.Equals(extension, "." + typeExtension, StringComparison.OrdinalIgnoreCase)))
                {
                    type = searchedType;
                    return true;
                }
            }

            type = DataFileTypes.Unspecified;
            return false;
        }

        // Mac names often have periods that aren't extensions (as "Map 1.1" does), so only letters (and digits) after a
        // period count as one
        private static bool HasExtension(string file)
        {
            var extension = Path.GetExtension(file);

            return extension.Length > 1 &&
                   extension.Skip(1).All(char.IsLetterOrDigit) &&
                   extension.Skip(1).Any(char.IsLetter);
        }

        private static bool TryGetTypeFromContents(string file, out DataFileTypes type)
        {
            type = DataFileTypes.Unspecified;

            try
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var dataFork = FindDataFork(stream);

                    if (IsSounds(stream, dataFork))
                    {
                        type = DataFileTypes.Sounds;
                    }
                    else if (TryGetWadTags(stream, dataFork, out var tags, out var dataVersion))
                    {
                        if (tags.Contains(SavedGameTag) && dataVersion != M1DataVersion)
                        {
                            return false;
                        }

                        if (tags.Overlaps(MapTags))
                        {
                            type = DataFileTypes.Maps;
                        }
                        else if (tags.Overlaps(PhysicsTags))
                        {
                            type = DataFileTypes.Physics;
                        }
                    }
                    else if (IsShapes(stream, dataFork))
                    {
                        type = DataFileTypes.Shapes;
                    }
                    else if (IsM1Physics(stream, dataFork))
                    {
                        type = DataFileTypes.Physics;
                    }
                    else if (MayHaveResourceFork(file, stream, dataFork))
                    {
                        type = TypeFromResources(file);
                    }
                }
            }
            catch (Exception exception) when (IsAccessException(exception))
            {
                return false;
            }

            return type != DataFileTypes.Unspecified;
        }

        // The data fork's offset and length, in a MacBinary or AppleSingle file as Aleph One finds them (FileHandler's
        // OpenedFile), or else the whole file
        private static (long Offset, long Length) FindDataFork(FileStream stream)
        {
            var probe = ReadAt(stream, 0, (int) Math.Min(ContainerProbeSize, stream.Length));
            var file = SDL_RWFromConstMem(probe, probe.Length);

            try
            {
                if (resource_manager.is_applesingle(file, false, out var offset, out var length))
                {
                    return (offset, length);
                }

                if (resource_manager.is_macbinary(file, out var dataLength, out _))
                {
                    return (MacBinaryHeaderSize, dataLength);
                }
            }
            finally
            {
                SDL_RWclose(file);
            }

            return (0, stream.Length);
        }

        // sound_file_header: its version (0 or 1), then its tag
        private static bool IsSounds(FileStream stream, (long Offset, long Length) dataFork)
        {
            var header = ReadAt(stream, dataFork.Offset, 8, dataFork);
            if (header == null)
            {
                return false;
            }

            var version = ReadBE32(header, 0);

            return (version == 0 || version == 1) && Tag(header, 4) == SoundsFileTag;
        }

        // The tags in the first wad (as wad.cpp reads a wad's directory and entries), if this is a wad file
        private static bool TryGetWadTags(FileStream stream, (long Offset, long Length) dataFork, out HashSet<string> tags, out int dataVersion)
        {
            tags = null;
            dataVersion = -1;

            var header = ReadAt(stream, dataFork.Offset, WadHeaderSize, dataFork);
            if (header == null)
            {
                return false;
            }

            int version = ReadBE16(header, 0);
            dataVersion = ReadBE16(header, 2);
            var directoryOffset = ReadBE32(header, 72);
            int wadCount = ReadBE16(header, 76);
            int applicationDataSize = ReadBE16(header, 78);
            int entryHeaderSize = ReadBE16(header, 80);
            int directoryEntryBaseSize = ReadBE16(header, 82);

            if (version < 0 || version > MaximumWadVersion || wadCount <= 0 || directoryOffset < WadHeaderSize)
            {
                return false;
            }

            var hasDirectoryEntries = version >= 1;
            var directoryEntrySize = hasDirectoryEntries ? directoryEntryBaseSize + applicationDataSize : OldDirectoryEntrySize;
            entryHeaderSize = hasDirectoryEntries ? entryHeaderSize : OldEntryHeaderSize;

            if (directoryEntrySize < OldDirectoryEntrySize || entryHeaderSize < OldEntryHeaderSize)
            {
                return false;
            }

            var directoryEntry = ReadAt(stream, dataFork.Offset + directoryOffset, OldDirectoryEntrySize, dataFork);
            if (directoryEntry == null)
            {
                return false;
            }

            var wadOffset = ReadBE32(directoryEntry, 0);
            var wadLength = ReadBE32(directoryEntry, 4);

            if (wadOffset < WadHeaderSize || wadLength < entryHeaderSize || wadOffset + (long) wadLength > dataFork.Length)
            {
                return false;
            }

            tags = new HashSet<string>();

            var entryOffset = 0;
            for (var i = 0; i < MaximumWadTags; i++)
            {
                if (entryOffset < 0 || entryOffset + entryHeaderSize > wadLength)
                {
                    return false;
                }

                var entryHeader = ReadAt(stream, dataFork.Offset + wadOffset + entryOffset, OldEntryHeaderSize, dataFork);
                if (entryHeader == null)
                {
                    return false;
                }

                tags.Add(Tag(entryHeader, 0));

                var nextOffset = ReadBE32(entryHeader, 4);
                if (nextOffset == 0)
                {
                    break;
                }

                // Each entry follows the one before it
                if (nextOffset <= entryOffset)
                {
                    return false;
                }

                entryOffset = nextOffset;
            }

            return true;
        }

        // The collection headers each give a collection's place in the file (or none), and a collection starts with its
        // version. Shapes files have no tag to recognize them by, so this checks every header.
        private static bool IsShapes(FileStream stream, (long Offset, long Length) dataFork)
        {
            var headersSize = ShapesCollectionCount * ShapesCollectionHeaderSize;
            var headers = ReadAt(stream, dataFork.Offset, headersSize, dataFork);
            if (headers == null)
            {
                return false;
            }

            var firstCollectionOffset = -1L;

            for (var i = 0; i < ShapesCollectionCount; i++)
            {
                var header = i * ShapesCollectionHeaderSize;

                // The 8-bit collection, then the 16-bit one (which only Marathon 2 and Infinity's own shapes have)
                for (var depth = 0; depth < 2; depth++)
                {
                    long offset = ReadBE32(headers, header + 4 + depth * 8);
                    long length = ReadBE32(headers, header + 8 + depth * 8);

                    if (offset == -1)
                    {
                        continue;
                    }

                    if (offset < headersSize || length <= 0 || offset + length > dataFork.Length)
                    {
                        return false;
                    }

                    if (firstCollectionOffset < 0)
                    {
                        firstCollectionOffset = offset;
                    }
                }
            }

            if (firstCollectionOffset < 0)
            {
                return false;
            }

            var collection = ReadAt(stream, dataFork.Offset + firstCollectionOffset, 2, dataFork);

            return collection != null && ReadBE16(collection, 0) == ShapesCollectionVersion;
        }

        // Chunks of Marathon 1 physics definitions, one after another to the data fork's end (as import_m1_physics_data
        // reads them)
        private static bool IsM1Physics(FileStream stream, (long Offset, long Length) dataFork)
        {
            var position = 0L;
            var chunks = 0;

            while (position < dataFork.Length)
            {
                var header = ReadAt(stream, dataFork.Offset + position, M1PhysicsChunkHeaderSize, dataFork);
                if (header == null || !M1PhysicsTags.Contains(Tag(header, 0)) || ++chunks > M1PhysicsTags.Count)
                {
                    return false;
                }

                var count = (ushort) ReadBE16(header, 8);
                var size = (ushort) ReadBE16(header, 10);

                position += M1PhysicsChunkHeaderSize + (long) count * size;
            }

            return chunks > 0 && position == dataFork.Length;
        }

        // In a MacBinary or AppleSingle file, a sidecar, or the file itself (as a resource fork saved on its own), as
        // Aleph One's open_res_file looks for one. Reading a resource fork reads the whole file, so this checks first.
        private static bool MayHaveResourceFork(string file, FileStream stream, (long Offset, long Length) dataFork)
        {
            var isInContainer = dataFork.Offset != 0 || dataFork.Length != stream.Length;
            if (isInContainer || ResourceForkSidecarSuffixes.Any(suffix => File.Exists(file + suffix)))
            {
                return true;
            }

            var header = ReadAt(stream, 0, ResourceForkHeaderSize, dataFork);
            if (header == null)
            {
                return false;
            }

            long dataOffset = ReadBE32(header, 0);
            long mapOffset = ReadBE32(header, 4);
            long dataLength = ReadBE32(header, 8);
            long mapLength = ReadBE32(header, 12);

            return dataOffset >= ResourceForkHeaderSize && mapOffset >= ResourceForkHeaderSize && dataLength >= 0 && mapLength > 0 &&
                   dataOffset + dataLength <= stream.Length && mapOffset + mapLength <= stream.Length;
        }

        // Marathon 1's shapes (with its first collection, as shapes.cpp checks) and sounds
        private static DataFileTypes TypeFromResources(string file)
        {
            MacResourceFork resourceFork;

            try
            {
                resourceFork = MacFileForks.Read(file).ResourceFork;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IndexOutOfRangeException)
            {
                // Not a resource fork after all
                return DataFileTypes.Unspecified;
            }

            if (resourceFork == null)
            {
                return DataFileTypes.Unspecified;
            }

            if (resourceFork.Has(M1CollectionResourceType, M1FirstCollectionResourceId))
            {
                return DataFileTypes.Shapes;
            }

            var types = resourceFork.Types.Select(type => type.Key).ToList();
            if (types.Contains(MacResourceFork.SoundType) && !types.Intersect(NotSoundsResourceTypes).Any())
            {
                return DataFileTypes.Sounds;
            }

            return DataFileTypes.Unspecified;
        }

        // Null if it isn't all within the data fork
        private static byte[] ReadAt(FileStream stream, long position, int count, (long Offset, long Length) dataFork)
        {
            if (position < dataFork.Offset || position + count > dataFork.Offset + dataFork.Length || position + count > stream.Length)
            {
                return null;
            }

            return ReadAt(stream, position, count);
        }

        private static byte[] ReadAt(FileStream stream, long position, int count)
        {
            var bytes = new byte[count];
            stream.Position = position;

            var read = 0;
            while (read < count)
            {
                var readNow = stream.Read(bytes, read, count - read);
                if (readNow <= 0)
                {
                    break;
                }

                read += readNow;
            }

            return bytes;
        }

        private static short ReadBE16(byte[] bytes, int offset)
        {
            return (short) ((bytes[offset] << 8) | bytes[offset + 1]);
        }

        private static int ReadBE32(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        private static string Tag(byte[] bytes, int offset)
        {
            return Encoding.ASCII.GetString(bytes, offset, 4);
        }

        private static bool IsAccessException(Exception exception)
        {
            return exception is IOException || exception is UnauthorizedAccessException || exception is NotSupportedException || exception is System.Security.SecurityException;
        }
    }
}
