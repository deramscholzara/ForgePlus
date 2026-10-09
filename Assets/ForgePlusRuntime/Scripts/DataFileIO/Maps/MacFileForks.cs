using AlephOne;
using ForgePlus.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using static AlephOne.SDL_rwops;

namespace ForgePlus.DataFileIO
{
    // A Mac file's resource fork and Finder information, found where Aleph One finds them (resource_manager.open_res_file):
    // in a MacBinary or AppleSingle file, or a sidecar. Saving writes only the data fork, so these carry the rest through.
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
        private const int AppleSingleHeaderSize = 26;
        private const int AppleSingleEntrySize = 12;
        private const uint AppleSingleDataForkId = 1;
        private const uint AppleSingleResourceForkId = 2;
        private const uint AppleSingleRealNameId = 3;

        // macOS's
        private const string NamedForkSuffix = "/..namedfork/rsrc";

        // In the order Aleph One looks for them (it looks in the file itself after the first two)
        private static readonly string[] SidecarSuffixes = { ".rsrc", ".resources", NamedForkSuffix };

        private string sourceFileName;

        // Its header, resource fork, and whatever follows the forks (such as a Get Info comment)
        private byte[] macBinaryHeader;
        private byte[] macBinaryResourceFork;
        private byte[] macBinaryTrailer;

        // Its header (up to its entry count), and its entries in order (the data fork's without its bytes)
        private byte[] appleSingleHeader;
        private readonly List<KeyValuePair<uint, byte[]>> appleSingleEntries = new List<KeyValuePair<uint, byte[]>>();

        // Each sidecar file's suffix and contents
        private readonly List<KeyValuePair<string, byte[]>> sidecars = new List<KeyValuePair<string, byte[]>>();

        // Null when the resource fork is in the file itself
        private string resourceForkSidecar;

        private readonly HashSet<(uint Type, short Id)> removedResources = new HashSet<(uint Type, short Id)>();

        public Containers Container { get; private set; }

        // The one Aleph One uses (or null, if it has none)
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

                return resourceForkSidecar == NamedForkSuffix
                           ? Strings.Get(Strings.Common, "Runtime.MacFileForks.Source.NamedFork", sourceFileName)
                           : sourceFileName + resourceForkSidecar;
            }
        }

        // Whether SetResourceRemoved affects the saved resource fork
        public bool CanEditResources { get; private set; }

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

        // Wraps the data fork saved at the path in this file's container (if it had one) and writes its sidecars next to
        // it. The resource fork is saved as edited, or as the one given instead; an empty one saves no resources at all.
        public void WriteAround(string path, byte[] resourceFork = null)
        {
            var savesNoResources = resourceFork != null && resourceFork.Length == 0;
            var replacement = CanEditResources ? resourceFork : null;

            if (Container != Containers.None)
            {
                var dataFork = File.ReadAllBytes(path);
                var bytes = Container == Containers.MacBinary
                                ? BuildMacBinary(dataFork, path, savesNoResources, replacement)
                                : BuildAppleSingle(dataFork, path, savesNoResources, replacement);

                var temporaryPath = path + ".forks";
                File.WriteAllBytes(temporaryPath, bytes);
                File.Replace(temporaryPath, path, null);
            }

            if (savesNoResources)
            {
                // No sidecars, so exit
                return;
            }

            foreach (var sidecar in sidecars)
            {
                var contents = sidecar.Key == resourceForkSidecar && CanEditResources ? SavedResourceFork(sidecar.Value, replacement) : sidecar.Value;
                if (contents.Length == 0)
                {
                    continue;
                }

                File.WriteAllBytes(path + sidecar.Key, contents);
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

        // The resources left out
        public HashSet<(uint Type, short Id)> RemovedResources
        {
            get
            {
                return removedResources;
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
            var entryCount = BigEndian.Read16(bytes, AppleSingleHeaderSize - 2);

            Container = Containers.AppleSingle;
            appleSingleHeader = Slice(bytes, 0, AppleSingleHeaderSize - 2);

            for (var i = 0; i < entryCount; i++)
            {
                var entry = AppleSingleHeaderSize + i * AppleSingleEntrySize;
                var id = (uint) BigEndian.Read32(bytes, entry);
                var contents = id == AppleSingleDataForkId ? null : Slice(bytes, BigEndian.Read32(bytes, entry + 4), BigEndian.Read32(bytes, entry + 8));

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
                if (sidecar.Key != NamedForkSuffix)
                {
                    candidates.Add((sidecar.Value, sidecar.Key));
                }
            }

            candidates.Add((fileBytes, null));

            foreach (var sidecar in sidecars)
            {
                if (sidecar.Key == NamedForkSuffix)
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

                    // A sidecar's can be edited unless it's wrapped in a container of its own; the file's only when it's in one
                    CanEditResources = candidate.Sidecar != null ? forkBytes == candidate.Bytes : Container != Containers.None;
                    return;
                }
            }
        }

        // Empty without resources; as edited when it's the one Aleph One uses, and otherwise (a sidecar's is) as it was
        private byte[] ContainerResourceFork(byte[] resourceFork, bool savesNoResources, byte[] replacement)
        {
            if (savesNoResources)
            {
                return new byte[0];
            }

            return resourceForkSidecar == null ? SavedResourceFork(resourceFork, replacement) : resourceFork;
        }

        private byte[] SavedResourceFork(byte[] resourceFork, byte[] replacement)
        {
            if (replacement != null)
            {
                return replacement;
            }

            return removedResources.Count == 0 ? resourceFork : ResourceFork.Without(removedResources);
        }

        // As resource_manager's read_map finds it
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

        // The header as it was (keeping the Finder's information), with the saved file's name and fork lengths
        private byte[] BuildMacBinary(byte[] dataFork, string path, bool savesNoResources, byte[] replacement)
        {
            var resourceFork = ContainerResourceFork(macBinaryResourceFork, savesNoResources, replacement);
            var header = (byte[]) macBinaryHeader.Clone();

            var name = csstrings.utf8_to_mac_roman(MacFileName(csstrings.mac_roman_to_utf8(Slice(header, 2, header[1])), path));
            var nameLength = Math.Min(name.Length, 63);
            Array.Clear(header, 1, 64);
            header[1] = (byte) nameLength;
            Array.Copy(name, 0, header, 2, nameLength);

            BigEndian.Write32(header, 83, dataFork.Length);
            BigEndian.Write32(header, 87, resourceFork.Length);

            BigEndian.Write16(header, 124, resource_manager.macbinary_crc(header));

            var bytes = new byte[MacBinaryHeaderSize + PaddedTo128(dataFork.Length) + PaddedTo128(resourceFork.Length) + macBinaryTrailer.Length];
            Array.Copy(header, bytes, MacBinaryHeaderSize);
            Array.Copy(dataFork, 0, bytes, MacBinaryHeaderSize, dataFork.Length);

            var resourceStart = MacBinaryHeaderSize + PaddedTo128(dataFork.Length);
            Array.Copy(resourceFork, 0, bytes, resourceStart, resourceFork.Length);
            Array.Copy(macBinaryTrailer, 0, bytes, resourceStart + PaddedTo128(resourceFork.Length), macBinaryTrailer.Length);

            return bytes;
        }

        // The entries in their order, laid out one after another, with the saved data fork and file name
        private byte[] BuildAppleSingle(byte[] dataFork, string path, bool savesNoResources, byte[] replacement)
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
                var resourceFork = ContainerResourceFork(entries[resourceForkIndex].Value, savesNoResources, replacement);
                entries[resourceForkIndex] = new KeyValuePair<uint, byte[]>(AppleSingleResourceForkId, resourceFork);
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
            BigEndian.Write16(bytes, AppleSingleHeaderSize - 2, entries.Count);

            var offset = AppleSingleHeaderSize + entries.Count * AppleSingleEntrySize;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = AppleSingleHeaderSize + i * AppleSingleEntrySize;
                BigEndian.Write32(bytes, entry, (int) entries[i].Key);
                BigEndian.Write32(bytes, entry + 4, offset);
                BigEndian.Write32(bytes, entry + 8, entries[i].Value.Length);

                Array.Copy(entries[i].Value, 0, bytes, offset, entries[i].Value.Length);
                offset += entries[i].Value.Length;
            }

            return bytes;
        }

        // Its own name if it's saved under its file's name; otherwise the saved file's (with its extension only if the
        // Mac name had its file's, as "Map" for "Map.sceA" doesn't)
        private string MacFileName(string originalName, string path)
        {
            var savedFileName = Path.GetFileName(path);

            if (string.Equals(savedFileName, sourceFileName, StringComparison.OrdinalIgnoreCase))
            {
                return originalName;
            }

            return originalName == sourceFileName ? savedFileName : Path.GetFileNameWithoutExtension(path);
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
    }
}
