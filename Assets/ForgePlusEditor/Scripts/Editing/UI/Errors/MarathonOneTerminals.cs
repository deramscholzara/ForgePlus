using AlephOne;
using ForgePlus.DataFileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForgePlus.UI
{
    // Marathon 1's terminals aren't in its levels: a control panel whose terminal number is past the level's own terminals
    // opens the 'term' resource numbered 1000 + 10 × the level's index + that number (computer_interface.cpp:
    // get_indexed_terminal_data), from the scenario's external resources (Marathon 1's application, beside its map file),
    // or else from Marathon 1's shapes file
    public static class MarathonOneTerminals
    {
        private static readonly uint TerminalResourceType = cstypes.FOUR_CHARS_TO_INT('t', 'e', 'r', 'm');

        // The external resources file last read, when it was last changed, and its terminals' resource numbers
        private static string cachedPath;
        private static DateTime cachedWriteTime;
        private static HashSet<int> cachedTerminals = new HashSet<int>();

        public static bool IsMarathonOneLevel(MapLevel level)
        {
            return level.loaded_wad != null && level.loaded_wad.data_version == editor.MARATHON_ONE_DATA_VERSION;
        }

        // Whether the level's control panels can open the terminal (true while there's nowhere to look, as it can't be
        // told)
        public static bool Exists(int levelIndex, short terminalNumber)
        {
            var resourceNumber = 1000 + levelIndex * 10 + terminalNumber;

            var externalResources = ExternalResourcesTerminals(out var hasExternalResources);
            if (externalResources.Contains(resourceNumber) ||
                shapes.m1_shapes_file_has_resource(TerminalResourceType, (short) resourceNumber))
            {
                return true;
            }

            return !hasExternalResources && !shapes.shapes_file_is_m1();
        }

        private static HashSet<int> ExternalResourcesTerminals(out bool hasExternalResources)
        {
            var path = FindExternalResources();
            hasExternalResources = path != null;
            if (path == null)
            {
                return new HashSet<int>();
            }

            var writeTime = File.GetLastWriteTimeUtc(path);
            if (path == cachedPath && writeTime == cachedWriteTime)
            {
                return cachedTerminals;
            }

            cachedPath = path;
            cachedWriteTime = writeTime;
            cachedTerminals = ReadTerminalNumbers(path);

            return cachedTerminals;
        }

        // Marathon 1's application (Aleph One's external resources for the scenario), beside the map file
        private static string FindExternalResources()
        {
            var mapPath = FileSettings.Instance.GetLoadableFilePath(DataFileTypes.Maps);
            var directory = string.IsNullOrEmpty(mapPath) ? null : Path.GetDirectoryName(mapPath);
            if (directory == null || !Directory.Exists(directory))
            {
                return null;
            }

            return Directory.EnumerateFiles(directory, "*.appl").OrderBy(file => file, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        }

        private static HashSet<int> ReadTerminalNumbers(string path)
        {
            var numbers = new HashSet<int>();

            var file = resource_manager.open_res_file(new FileSpecifier(path));
            if (file == null)
            {
                return numbers;
            }

            var previous = resource_manager.cur_res_file();
            try
            {
                resource_manager.use_res_file(file);

                // Every number a level's terminals could have (1000 to 1000 + 10 × the most levels, + 9)
                for (var number = 1000; number < 1000 + 10 * 256; number++)
                {
                    if (resource_manager.has_1_resource(TerminalResourceType, (short) number))
                    {
                        numbers.Add(number);
                    }
                }
            }
            finally
            {
                resource_manager.use_res_file(previous);
                resource_manager.close_res_file(file);
            }

            return numbers;
        }
    }
}
