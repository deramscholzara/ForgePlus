// Port of Aleph One: Source_Files/XML/XML_LevelScript.h, XML_LevelScript.cpp (the MML a map gives its levels: its level
// script's, and each level's embedded MML)
//
// Not ported: the level script's Lua, music, load screens, movies and end screens, the default levels' MML
// (parse_mml_default_levels, which only has music and load screens), and embedded Lua (the LUAS chunk).
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.map;

namespace AlephOne
{
    [NoAutoStaticsCleanup]
    public static class XML_LevelScript
    {
        // The pseudo-levels a level script's commands can be for
        private const int Restore = -3;
        private const int Default = -2;
        private const int End = -1;

        // The level script's MML resources, for each level
        private static readonly Dictionary<int, List<short>> LevelScripts = new Dictionary<int, List<short>>();

        // Loads all those in resource 128 in a map file (or some appropriate equivalent)
        public static void LoadLevelScripts()
        {
            // Get rid of the previous level script
            LevelScripts.Clear();

            // The script is stored at a special resource ID;
            // simply quit if it could not be found
            var ScriptRsrc = new LoadedResource();
            if (!images.get_text_resource_from_scenario(128, ScriptRsrc)) return;

            // Load the script
            try
            {
                var root = InfoTree.load_xml(ScriptRsrc.GetPointer(), ScriptRsrc.GetLength()).get_child("marathon_levels");
                parse_levels_xml(root);
            }
            catch (InfoTree.parse_error e)
            {
                Logging.logError($"Error parsing map script: {e.Message}");
            }
            catch (InfoTree.path_error e)
            {
                Logging.logError($"Error parsing map script: {e.Message}");
            }
        }

        // ForgePlus: forgets the level script, as there's no map to have one
        public static void UnloadLevelScripts()
        {
            LevelScripts.Clear();
        }

        // reset values to engine defaults first, then load the base stuff (from the Scripts folder and whatnot).
        // ForgePlus: given the scripts' folders, rather than looking in the search path's
        public static void ResetLevelScript(IEnumerable<string> script_directories)
        {
            XML_MakeRoot.ResetAllMMLValues();
            shell.LoadBaseMMLScripts(script_directories, false);
            Plugins.instance().load_mml(false);
        }

        // Runs a script for some level
        // runs level-specific MML...
        public static void RunLevelScript(int LevelIndex)
        {
            GeneralRunScript(Default);
            GeneralRunScript(LevelIndex);
        }

        // The level's embedded MML (its MMLS chunk): a 2-byte header, then each script's flags, name, length and text
        public static void RunScriptChunks(byte[] mmls_chunk)
        {
            if (mmls_chunk == null)
            {
                return;
            }

            var offset = 2;
            while (offset < mmls_chunk.Length)
            {
                if (offset + 8 + LEVEL_NAME_LENGTH > mmls_chunk.Length)
                    break;

                // flags (4 bytes) and name, then the length
                var length_offset = offset + 4 + LEVEL_NAME_LENGTH;
                var length = (uint) ((mmls_chunk[length_offset] << 24) | (mmls_chunk[length_offset + 1] << 16) |
                                     (mmls_chunk[length_offset + 2] << 8) | mmls_chunk[length_offset + 3]);
                offset += 8 + LEVEL_NAME_LENGTH;

                if (offset + length > mmls_chunk.Length)
                    break;

                if (length != 0)
                {
                    var data = new byte[length];
                    System.Array.Copy(mmls_chunk, offset, data, 0, length);
                    XML_MakeRoot.ParseMMLFromData(data, data.Length);
                }

                offset += (int) length;
            }

            // Not ported: the LUAS chunk's Lua
        }

        // Search for level script and then run it
        private static void GeneralRunScript(int LevelIndex)
        {
            // Find the pointer to the current script
            if (!LevelScripts.TryGetValue(LevelIndex, out var CurrScript)) return;

            foreach (var RsrcID in CurrScript)
            {
                // First, try to load a resource (only for scripts)
                var ScriptRsrc = new LoadedResource();
                if (!images.get_text_resource_from_scenario(RsrcID, ScriptRsrc))
                    continue;

                // Skip if not loaded
                if (ScriptRsrc.GetLength() <= 0) continue;

                // Set to the MML root parser
                XML_MakeRoot.ParseMMLFromData(ScriptRsrc.GetPointer(), ScriptRsrc.GetLength());

                // Not ported: Lua, music and load screens
            }
        }

        private static void parse_levels_xml(InfoTree root)
        {
            foreach (var lev in root.children_named("level"))
            {
                short index = 0;
                if (lev.read_indexed("index", ref index, short.MaxValue + 1))
                {
                    parse_level_commands(lev, index);
                }
            }

            foreach (var child in root.children_named("end"))
            {
                parse_level_commands(child, End);
            }
            foreach (var child in root.children_named("default"))
            {
                parse_level_commands(child, Default);
            }
            foreach (var child in root.children_named("restore"))
            {
                parse_level_commands(child, Restore);
            }

            // Not ported: end_screens
        }

        private static void parse_level_commands(InfoTree root, int index)
        {
            // Find or create command list for this level
            if (!LevelScripts.TryGetValue(index, out var ls_ptr))
            {
                ls_ptr = new List<short>();
                LevelScripts[index] = ls_ptr;
            }

            foreach (var child in root.children_named("mml"))
            {
                short RsrcID = 0;
                if (!child.read_attr_bounded("resource", ref RsrcID, 0, short.MaxValue))
                    continue;

                ls_ptr.Add(RsrcID);
            }

            // Not ported: lua, music, random_order, movie and load_screen
        }
    }
}
