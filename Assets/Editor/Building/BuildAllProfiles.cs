using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace ForgePlus.EditorTools
{
    /// <summary>
    /// Builds the project's Build Profile assets (all, or those for one platform) into "Desktop/ForgePlusBuilds/ForgePlus_[Profile Name]",
    /// removes the "do not ship" debug folders, and zips each build (just the .app, for Mac) with Unix permissions recorded
    /// (which Windows' built-in zip and 7-Zip omit, leaving Mac apps unable to launch).
    /// </summary>
    internal static class BuildAllProfiles
    {
        private const string BuildsFolderName = "ForgePlusBuilds";
        private const string ProductFileName = "ForgePlus";
        private const string CustomProfileSuffix = " (Custom)";
        private const string FolderPrefix = "ForgePlus_";

        // Unity names its debug-symbol folders "*_BurstDebugInformation_DoNotShip" and "*_BackUpThisFolder_ButDontShipItWithYourGame".
        private static readonly string[] DoNotShipMarkers = { "DoNotShip", "DontShip" };

        private const uint CentralDirectorySignature = 0x02014b50;
        private const uint EndOfCentralDirectorySignature = 0x06054b50;
        private const uint Zip64LocatorSignature = 0x07064b50;
        private const int EndOfCentralDirectorySize = 22;
        private const int Zip64LocatorSize = 20;
        private const byte UnixHostSystem = 3;
        private const uint UnixFolderMode = 0x41ED; // drwxr-xr-x
        private const uint UnixExecutableMode = 0x81ED; // -rwxr-xr-x
        private const uint UnixFileMode = 0x81A4; // -rw-r--r--
        private const uint DosFolderAttribute = 0x10;

        [MenuItem("Build/Build All Profiles")]
        private static void BuildAll()
        {
            BuildProfiles("Build All Profiles", target => true);
        }

        [MenuItem("Build/Individual/Windows")]
        private static void BuildWindows()
        {
            BuildProfiles("Build Windows", target => target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64);
        }

        // Includes every Mac profile (e.g. both Mac and Mac-IntelMono).
        [MenuItem("Build/Individual/Mac")]
        private static void BuildMac()
        {
            BuildProfiles("Build Mac", target => target == BuildTarget.StandaloneOSX);
        }

        [MenuItem("Build/Individual/Linux")]
        private static void BuildLinux()
        {
            BuildProfiles("Build Linux", target => target == BuildTarget.StandaloneLinux64);
        }

        private static void BuildProfiles(string title, Func<BuildTarget, bool> includeTarget)
        {
            var profiles = AssetDatabase.FindAssets($"t:{nameof(BuildProfile)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<BuildProfile>)
                .Where(profile => profile != null && includeTarget(GetBuildTarget(profile)))
                .OrderBy(profile => profile.name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (profiles.Count == 0)
            {
                EditorUtility.DisplayDialog(title, "No matching Build Profile assets were found.", "OK");
                return;
            }

            var buildsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), BuildsFolderName);

            if (!EditorUtility.DisplayDialog(
                    title,
                    $"Build these profiles into \"{buildsRoot}\"?\n\n{string.Join("\n", profiles.Select(GetFolderName))}\n\nExisting builds and zips with these names will be replaced.",
                    "Build",
                    "Cancel"))
            {
                return;
            }

            Directory.CreateDirectory(buildsRoot);

            var summary = new StringBuilder();
            foreach (var profile in profiles)
            {
                summary.AppendLine($"{GetFolderName(profile)}: {BuildAndZip(profile, buildsRoot)}");
            }

            Debug.Log($"{nameof(BuildAllProfiles)}: Finished.\n{summary}");
            EditorUtility.RevealInFinder(buildsRoot);
            EditorUtility.DisplayDialog(title, summary.ToString(), "OK");
        }

        private static string BuildAndZip(BuildProfile profile, string buildsRoot)
        {
            var folderName = GetFolderName(profile);
            var target = GetBuildTarget(profile);
            var executableName = ProductFileName + GetExecutableExtension(target);
            var outputFolder = Path.Combine(buildsRoot, folderName);
            var zipFile = Path.Combine(buildsRoot, folderName + ".zip");

            // Start clean so stale files from a previous build don't end up in the zip.
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, recursive: true);
            }

            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }

            var options = new BuildPlayerWithProfileOptions
            {
                buildProfile = profile,
                locationPathName = Path.Combine(outputFolder, executableName),
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"{nameof(BuildAllProfiles)}: Build of '{profile.name}' {report.summary.result} with {report.summary.totalErrors} error(s).");
                return $"build {report.summary.result.ToString().ToLowerInvariant()}";
            }

            DeleteDoNotShipFolders(outputFolder);

            // Mac zips hold just the .app at their root, so unzipping yields the app itself; others hold the whole build folder.
            var zipError = target == BuildTarget.StandaloneOSX
                ? Zip(outputFolder, executableName, zipFile)
                : Zip(buildsRoot, folderName, zipFile);
            if (zipError != null)
            {
                Debug.LogError($"{nameof(BuildAllProfiles)}: Zipping '{folderName}' failed: {zipError}");
                return "built, but zipping failed (see Console)";
            }

            return "built and zipped";
        }

        private static string GetFolderName(BuildProfile profile)
        {
            var platformName = profile.name.EndsWith(CustomProfileSuffix, StringComparison.Ordinal)
                ? profile.name.Substring(0, profile.name.Length - CustomProfileSuffix.Length)
                : profile.name;

            return FolderPrefix + platformName;
        }

        // BuildProfile only exposes its target internally.
        private static BuildTarget GetBuildTarget(BuildProfile profile)
        {
            return (BuildTarget)new SerializedObject(profile).FindProperty("m_BuildTarget").intValue;
        }

        private static string GetExecutableExtension(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                case BuildTarget.StandaloneOSX:
                    return ".app";
                case BuildTarget.StandaloneLinux64:
                    return ".x86_64";
                default:
                    return string.Empty;
            }
        }

        private static void DeleteDoNotShipFolders(string outputFolder)
        {
            var doNotShipFolders = Directory.GetDirectories(outputFolder, "*", SearchOption.AllDirectories)
                .Where(folder => DoNotShipMarkers.Any(marker => Path.GetFileName(folder).IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            foreach (var folder in doNotShipFolders)
            {
                // A nested match may already be gone with its parent.
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, recursive: true);
                }
            }
        }

        /// <summary>
        /// Zips the folder "[parentFolder]/[folderName]" into zipFile, with that folder at the zip's root.
        /// Returns null on success, otherwise the error.
        /// </summary>
        internal static string Zip(string parentFolder, string folderName, string zipFile)
        {
            var sourceFolder = Path.Combine(parentFolder, folderName);

            try
            {
                var folders = Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories);
                var files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);

                using (var archive = new ZipArchive(File.Create(zipFile), ZipArchiveMode.Create))
                {
                    // Explicit folder entries, so empty folders survive and get their own permissions.
                    archive.CreateEntry(folderName + "/");
                    foreach (var folder in folders)
                    {
                        archive.CreateEntry(GetEntryName(parentFolder, folder) + "/");
                    }

                    for (var i = 0; i < files.Length; i++)
                    {
                        var entryName = GetEntryName(parentFolder, files[i]);
                        EditorUtility.DisplayProgressBar("Build", $"Zipping {entryName}", (float)i / files.Length);

                        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                        entry.LastWriteTime = File.GetLastWriteTime(files[i]);

                        using (var source = File.OpenRead(files[i]))
                        using (var destination = entry.Open())
                        {
                            source.CopyTo(destination);
                        }
                    }
                }

                SetUnixAttributes(zipFile);
                return null;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static string GetEntryName(string root, string path)
        {
            return path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
        }

        /// <summary>
        /// ZipArchive marks entries as made on Windows, which macOS and Linux read as "no Unix permissions recorded",
        /// leaving Mac app binaries non-executable. This rewrites each central directory record to say it was made on
        /// Unix, with folders and executables at 755 and everything else at 644.
        /// </summary>
        private static void SetUnixAttributes(string zipFile)
        {
            using (var stream = new FileStream(zipFile, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(stream))
            {
                var (centralDirectoryOffset, entryCount) = ReadCentralDirectoryLocation(stream, reader);

                stream.Position = centralDirectoryOffset;
                for (long i = 0; i < entryCount; i++)
                {
                    var recordStart = stream.Position;
                    if (reader.ReadUInt32() != CentralDirectorySignature)
                    {
                        throw new InvalidDataException("Unexpected zip central directory layout.");
                    }

                    stream.Position = recordStart + 28;
                    var nameLength = reader.ReadUInt16();
                    var extraLength = reader.ReadUInt16();
                    var commentLength = reader.ReadUInt16();

                    stream.Position = recordStart + 46;
                    var name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

                    var isFolder = name.EndsWith("/", StringComparison.Ordinal);
                    var unixMode = isFolder ? UnixFolderMode : IsExecutable(name) ? UnixExecutableMode : UnixFileMode;
                    var externalAttributes = (unixMode << 16) | (isFolder ? DosFolderAttribute : 0u);

                    // The high byte of "version made by" is the host system.
                    stream.Position = recordStart + 5;
                    stream.WriteByte(UnixHostSystem);

                    stream.Position = recordStart + 38;
                    stream.Write(BitConverter.GetBytes(externalAttributes), 0, 4);

                    stream.Position = recordStart + 46 + nameLength + extraLength + commentLength;
                }
            }
        }

        private static (long offset, long entryCount) ReadCentralDirectoryLocation(FileStream stream, BinaryReader reader)
        {
            // The end of central directory record is last in the file, followed only by a comment of up to 64KB.
            var searchStart = Math.Max(0, stream.Length - EndOfCentralDirectorySize - ushort.MaxValue);
            for (var position = stream.Length - EndOfCentralDirectorySize; position >= searchStart; position--)
            {
                stream.Position = position;
                if (reader.ReadUInt32() != EndOfCentralDirectorySignature)
                {
                    continue;
                }

                stream.Position = position + 10;
                long entryCount = reader.ReadUInt16();
                stream.Position = position + 16;
                long offset = reader.ReadUInt32();

                // Large zips keep the real values in the Zip64 end of central directory record.
                if ((entryCount == ushort.MaxValue || offset == uint.MaxValue) && position >= Zip64LocatorSize)
                {
                    stream.Position = position - Zip64LocatorSize;
                    if (reader.ReadUInt32() == Zip64LocatorSignature)
                    {
                        stream.Position = position - Zip64LocatorSize + 8;
                        var zip64RecordStart = reader.ReadInt64();

                        stream.Position = zip64RecordStart + 32;
                        entryCount = reader.ReadInt64();
                        stream.Position = zip64RecordStart + 48;
                        offset = reader.ReadInt64();
                    }
                }

                return (offset, entryCount);
            }

            throw new InvalidDataException("Zip end of central directory record not found.");
        }

        private static bool IsExecutable(string entryName)
        {
            // Mac bundle executables live directly in "Contents/MacOS"; the Linux player is "ForgePlus.x86_64".
            var parentEnd = entryName.LastIndexOf('/');
            var parent = parentEnd >= 0 ? entryName.Substring(0, parentEnd) : string.Empty;

            return parent.EndsWith(".app/Contents/MacOS", StringComparison.Ordinal) ||
                   entryName.EndsWith(".x86_64", StringComparison.Ordinal);
        }
    }
}
