// Port of Aleph One: Source_Files/shell.cpp (the data search path, and loading the base MML scripts)
//
// Not ported: everything else (starting up, the main loop, menus, preferences and the like).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Scripting.LifecycleManagement;

namespace AlephOne
{
    // Puts a folder at the front of the search path while it's in use (FileHandler.cpp)
    public sealed class ScopedSearchPath : IDisposable
    {
        private readonly string d;

        public ScopedSearchPath(string dir)
        {
            d = dir;
            shell.data_search_path.Insert(0, dir);
        }

        public void Dispose()
        {
            shell.data_search_path.Remove(d);
        }
    }

    [NoAutoStaticsCleanup]
    public static class shell
    {
        // Where relative paths (in MML, and plugins') are looked for, in order
        public static readonly List<string> data_search_path = new List<string>();

        private static bool _ParseMMLDirectory(string dir, bool load_menu_mml_only)
        {
            // Get sorted list of files in directory
            string[] de;
            try
            {
                de = Directory.GetFiles(dir);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return false;
            }

            Array.Sort(de, StringComparer.Ordinal);

            // Parse each file
            foreach (var i in de)
            {
                var name = Path.GetFileName(i);
                if (name.EndsWith("~"))
                    continue;
                // people stick Lua scripts in Scripts/
                if (name.EndsWith(".lua"))
                    continue;

                // Parse file
                XML_MakeRoot.ParseMMLFromFile(new FileSpecifier(i), load_menu_mml_only);
            }

            return true;
        }

        // ForgePlus: Aleph One parses the MML and Scripts folders in each of the search path's folders; ForgePlus is given
        // the scenario's scripts folders
        public static void LoadBaseMMLScripts(IEnumerable<string> script_directories, bool load_menu_mml_only)
        {
            foreach (var path in script_directories.Where(directory => !string.IsNullOrEmpty(directory)))
            {
                _ParseMMLDirectory(path, load_menu_mml_only);
            }
        }
    }
}
