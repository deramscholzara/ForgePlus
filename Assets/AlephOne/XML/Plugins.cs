// Port of Aleph One: Source_Files/XML/Plugins.h, Plugins.cpp (finding plugins, choosing which are in use, and loading
// their MML and shapes patches)
//
// Not ported: plugins in zip files, Steam Workshop items, sounds patches (which aren't ported), map patches (resources
// that replace a map's, for a map with a matching checksum), and the Lua scripts and themes plugins can have (which are
// only read, as they decide which plugins are in use).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Scripting.LifecycleManagement;

namespace AlephOne
{
    public class ScenarioInfo
    {
        public string name = string.Empty;
        public string scenario_id = string.Empty;
        public string version = string.Empty;
    }

    public class ShapesPatch
    {
        public bool requires_opengl;
        public string path = string.Empty;
    }

    public class SoloLuaWriteAccess
    {
        // excludes all other Lua
        public const uint world = 0x01;

        // excludes all other "exclusive" Lua of the same type
        public const uint fog = 0x02;
        public const uint music = 0x04;
        public const uint overlays = 0x08;
        public const uint exclusive_mask = world | fog | music | overlays;

        // allowed, and multiples allowed, with any types except world
        public const uint ephemera = 0x10;
        public const uint sound = 0x20;

        private readonly uint m_flags;

        public SoloLuaWriteAccess() : this(world) { }

        public SoloLuaWriteAccess(uint flags)
        {
            m_flags = flags;
        }

        public bool is_excluded(uint flags)
        {
            return (flags & get_exclusive_flags()) != 0;
        }

        public uint get_flags() { return m_flags; }

        public uint get_exclusive_flags()
        {
            if ((m_flags & world) != 0)
            {
                return exclusive_mask;
            }
            else
            {
                return m_flags & exclusive_mask;
            }
        }
    }

    public class Plugin
    {
        public string directory = string.Empty;
        public string name = string.Empty;
        public string description = string.Empty;
        public string version = string.Empty;
        public List<string> mmls = new List<string>();
        public string hud_lua = string.Empty;
        public string solo_lua = string.Empty;
        public SoloLuaWriteAccess solo_lua_write_access = new SoloLuaWriteAccess();
        public string stats_lua = string.Empty;
        public string theme = string.Empty;
        public string required_version = string.Empty;
        public List<ShapesPatch> shapes_patches = new List<ShapesPatch>();
        public List<ScenarioInfo> required_scenarios = new List<ScenarioInfo>();
        public List<string> sounds_patches = new List<string>();

        public bool auto_enable;
        public bool enabled;
        public bool overridden;
        public bool overridden_solo;

        public bool compatible()
        {
            if (required_version.Length > 0 && string.CompareOrdinal(Plugins.A1_DATE_VERSION, required_version) < 0)
                return false;
            if (required_scenarios.Count == 0)
                return true;

            var scenName = Scenario.instance().GetName();
            var scenID = Scenario.instance().GetID();
            var scenVers = Scenario.instance().GetVersion();

            foreach (var it in required_scenarios)
            {
                if ((it.name.Length == 0 || it.name == scenName) &&
                    (it.scenario_id.Length == 0 || it.scenario_id == scenID) &&
                    (it.version.Length == 0 || it.version == scenVers))
                    return true;
            }

            return false;
        }

        // ForgePlus: network_preferences->allow_stats is taken to be on
        public bool allowed()
        {
            return true;
        }

        public bool valid()
        {
            if (!enabled)
                return false;
            if (!Plugins.use_solo_lua && Plugins.instance().mode() == Plugins.GameMode.kMode_Solo)
                return !overridden_solo;
            return !overridden;
        }
    }

    [NoAutoStaticsCleanup]
    public class Plugins
    {
        // The Aleph One release this port's plugins are checked against (alephversion.h)
        public const string A1_DATE_VERSION = "20250829";

        // ForgePlus: environment_preferences->use_solo_lua's default
        public const bool use_solo_lua = false;

        public enum GameMode
        {
            kMode_Menu,
            kMode_Solo,
            kMode_Net,
        }

        private static Plugins m_instance;

        private readonly List<Plugin> m_plugins = new List<Plugin>();
        private bool m_validated;

        // ForgePlus: levels are shown as a solo game would show them
        private GameMode m_mode = GameMode.kMode_Solo;

        private Plugins() { }

        public static Plugins instance()
        {
            if (m_instance == null)
            {
                m_instance = new Plugins();
            }

            return m_instance;
        }

        public IReadOnlyList<Plugin> plugins
        {
            get
            {
                return m_plugins;
            }
        }

        public void invalidate() { m_validated = false; }
        public void set_mode(GameMode mode) { m_mode = mode; }
        public GameMode mode() { return m_mode; }

        // ForgePlus: forgets every plugin (Aleph One only finds them once, at startup)
        public void clear()
        {
            m_plugins.Clear();
            m_validated = false;
        }

        public bool disable(string path)
        {
            foreach (var it in m_plugins)
            {
                if (it.directory == path)
                {
                    it.enabled = false;
                    m_validated = false;
                    return true;
                }
            }

            return false;
        }

        public bool enable(string path)
        {
            foreach (var p in m_plugins)
            {
                if (p.directory == path)
                {
                    p.enabled = true;
                    m_validated = false;
                    return true;
                }
            }

            return false;
        }

        private static void load_mmls(Plugin plugin, bool load_menu_mml_only)
        {
            using (new ScopedSearchPath(plugin.directory))
            {
                foreach (var it in plugin.mmls)
                {
                    var file = new FileSpecifier();
                    if (file.SetNameWithPath(it))
                    {
                        XML_MakeRoot.ParseMMLFromFile(file, load_menu_mml_only);
                    }
                    else
                    {
                        Logging.logWarning($"{plugin.name} Plugin: {it} not found; ignoring");
                    }
                }
            }
        }

        public void load_mml(bool load_menu_mml_only)
        {
            validate();

            foreach (var it in m_plugins)
            {
                if (it.valid())
                {
                    load_mmls(it, load_menu_mml_only);
                }
            }
        }

        public void load_shapes_patches(bool is_opengl)
        {
            validate();

            foreach (var it in m_plugins)
            {
                if (it.valid())
                {
                    using (new ScopedSearchPath(it.directory))
                    {
                        foreach (var shapes_patch in it.shapes_patches)
                        {
                            if (is_opengl || !shapes_patch.requires_opengl)
                            {
                                var file = new FileSpecifier();
                                if (file.SetNameWithPath(shapes_patch.path))
                                {
                                    var ofile = new OpenedFile();
                                    if (file.Open(ofile))
                                    {
                                        shapes.load_shapes_patch(ofile.GetRWops(), false);
                                        ofile.Close();
                                    }
                                }
                                else
                                {
                                    Logging.logWarning($"{it.name} Plugin: {shapes_patch.path} not found; ignoring");
                                }
                            }
                        }
                    }
                }
            }
        }

        // ForgePlus: the shapes patches load_shapes_patches would load, to tell whether they've changed
        public List<string> find_shapes_patches(bool is_opengl)
        {
            validate();

            var paths = new List<string>();
            foreach (var it in m_plugins.Where(plugin => plugin.valid()))
            {
                foreach (var shapes_patch in it.shapes_patches.Where(patch => is_opengl || !patch.requires_opengl))
                {
                    var file = new FileSpecifier();
                    if (file.SetNameWithPath(shapes_patch.path, it.directory))
                    {
                        paths.Add(file.GetPath());
                    }
                }
            }

            return paths;
        }

        public Plugin find_hud_lua()
        {
            validate();
            for (var i = m_plugins.Count - 1; i >= 0; i--)
            {
                if (m_plugins[i].hud_lua.Length > 0 && m_plugins[i].valid())
                {
                    return m_plugins[i];
                }
            }

            return null;
        }

        public List<Plugin> find_solo_lua()
        {
            validate();
            return m_plugins.Where(plugin => plugin.solo_lua.Length > 0 && plugin.valid()).ToList();
        }

        public Plugin find_stats_lua()
        {
            validate();
            for (var i = m_plugins.Count - 1; i >= 0; i--)
            {
                if (m_plugins[i].stats_lua.Length > 0 && m_plugins[i].valid())
                {
                    return m_plugins[i];
                }
            }

            return null;
        }

        public Plugin find_theme()
        {
            validate();
            for (var i = m_plugins.Count - 1; i >= 0; i--)
            {
                if (m_plugins[i].theme.Length > 0 && m_plugins[i].valid())
                {
                    return m_plugins[i];
                }
            }

            return null;
        }

        private static bool plugin_file_exists(Plugin Data, string Path)
        {
            var f = new FileSpecifier(System.IO.Path.Combine(Data.directory, Path));
            return f.Exists();
        }

        private void ParsePlugin(string file_name)
        {
            byte[] file_data;
            try
            {
                file_data = File.ReadAllBytes(file_name);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return;
            }

            var current_plugin_directory = Path.GetDirectoryName(file_name);

            try
            {
                var root = InfoTree.load_xml(file_data, file_data.Length).get_child("plugin");

                var Data = new Plugin();
                Data.directory = current_plugin_directory;
                Data.enabled = true;
                Data.auto_enable = true;
                root.read_attr("auto_enable", ref Data.auto_enable);
                Data.enabled = Data.auto_enable;
                root.read_attr("name", ref Data.name);
                root.read_attr("version", ref Data.version);
                root.read_attr("description", ref Data.description);
                root.read_attr("minimum_version", ref Data.required_version);

                if (root.read_attr("hud_lua", ref Data.hud_lua) &&
                    !plugin_file_exists(Data, Data.hud_lua))
                    Data.hud_lua = string.Empty;

                var solo_luas = root.children_named("solo_lua").ToList();
                if (solo_luas.Count == 1)
                {
                    foreach (var solo_lua in solo_luas)
                    {
                        if (solo_lua.read_attr("file", ref Data.solo_lua) &&
                            !plugin_file_exists(Data, Data.solo_lua))
                        {
                            Data.solo_lua = string.Empty;
                        }

                        var write_accesses = solo_lua.children_named("write_access").ToList();
                        if (write_accesses.Count > 0)
                        {
                            uint flags = 0;
                            foreach (var write_access_tree in write_accesses)
                            {
                                var write_access = write_access_tree.get_value(string.Empty);
                                if (write_access == "ephemera")
                                {
                                    flags |= SoloLuaWriteAccess.ephemera;
                                }
                                else if (write_access == "fog")
                                {
                                    flags |= SoloLuaWriteAccess.fog;
                                }
                                else if (write_access == "music")
                                {
                                    flags |= SoloLuaWriteAccess.music;
                                }
                                else if (write_access == "overlays")
                                {
                                    flags |= SoloLuaWriteAccess.overlays;
                                }
                                else if (write_access == "sound")
                                {
                                    flags |= SoloLuaWriteAccess.sound;
                                }
                                else if (write_access == "world")
                                {
                                    flags |= SoloLuaWriteAccess.world;
                                }
                            }

                            Data.solo_lua_write_access = new SoloLuaWriteAccess(flags);
                        }
                    }
                }
                else if (solo_luas.Count == 0)
                {
                    // check the legacy attribute
                    if (root.read_attr("solo_lua", ref Data.solo_lua) &&
                        !plugin_file_exists(Data, Data.solo_lua))
                    {
                        Data.solo_lua = string.Empty;
                    }
                }
                else
                {
                    Logging.logError($"There were parsing errors in {Path.GetFileName(current_plugin_directory)} Plugin.xml: only one solo_lua tag is allowed");
                }

                if (root.read_attr("stats_lua", ref Data.stats_lua) &&
                    !plugin_file_exists(Data, Data.stats_lua))
                    Data.stats_lua = string.Empty;

                if (root.read_attr("theme_dir", ref Data.theme) &&
                    !plugin_file_exists(Data, Data.theme + "/theme2.mml"))
                    Data.theme = string.Empty;

                foreach (var tree in root.children_named("mml"))
                {
                    var mml_path = string.Empty;
                    if (tree.read_attr("file", ref mml_path) &&
                        plugin_file_exists(Data, mml_path))
                        Data.mmls.Add(mml_path);
                }

                foreach (var tree in root.children_named("shapes_patch"))
                {
                    var patch = new ShapesPatch();
                    tree.read_attr("file", ref patch.path);
                    tree.read_attr("requires_opengl", ref patch.requires_opengl);
                    if (plugin_file_exists(Data, patch.path))
                        Data.shapes_patches.Add(patch);
                }

                foreach (var tree in root.children_named("sounds_patch"))
                {
                    var sound_patch = string.Empty;
                    tree.read_attr("file", ref sound_patch);
                    if (plugin_file_exists(Data, sound_patch))
                        Data.sounds_patches.Add(sound_patch);
                }

                foreach (var tree in root.children_named("scenario"))
                {
                    var info = new ScenarioInfo();
                    tree.read_attr("name", ref info.name);
                    if (info.name.Length > 31)
                        info.name = info.name.Substring(0, 31);
                    tree.read_attr("id", ref info.scenario_id);
                    if (info.scenario_id.Length > 23)
                        info.scenario_id = info.scenario_id.Substring(0, 23);
                    tree.read_attr("version", ref info.version);
                    if (info.version.Length > 7)
                        info.version = info.version.Substring(0, 7);
                    if (info.name.Length > 0 || info.scenario_id.Length > 0)
                        Data.required_scenarios.Add(info);
                }

                // Not ported: map_patch

                if (Data.name.Length > 0)
                {
                    Data.mmls.Sort(StringComparer.Ordinal);
                    if (Data.theme.Length > 0)
                    {
                        Data.hud_lua = string.Empty;
                        Data.solo_lua = string.Empty;
                        Data.shapes_patches.Clear();
                        Data.sounds_patches.Clear();
                    }

                    m_plugins.Add(Data);
                }
            }
            catch (Exception e) when (e is InfoTree.parse_error || e is InfoTree.path_error)
            {
                Logging.logError($"There were parsing errors in {Path.GetFileName(current_plugin_directory)} Plugin.xml: {e.Message}");
            }
        }

        private bool ParseDirectory(string dir)
        {
            string[] de;
            try
            {
                de = Directory.GetFileSystemEntries(dir);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return false;
            }

            foreach (var file in de)
            {
                var name = Path.GetFileName(file);
                if (name == "Plugin.xml")
                {
                    ParsePlugin(file);
                }
                else if (Directory.Exists(file) && name[0] != '.')
                {
                    ParseDirectory(file);
                }

                // Not ported: plugins in zip files
            }

            return true;
        }

        // ForgePlus: Aleph One looks in the Plugins folder of each of the search path's folders; ForgePlus is given the
        // scenario's plugins folder
        public void enumerate(string plugins_directory)
        {
            m_plugins.Clear();

            if (!string.IsNullOrEmpty(plugins_directory))
            {
                ParseDirectory(plugins_directory);
            }

            // std::sort by name (which isn't stable)
            var sorted = m_plugins.OrderBy(plugin => plugin.name, StringComparer.Ordinal).ToList();
            m_plugins.Clear();
            m_plugins.AddRange(sorted);

            m_validated = false;
        }

        // enforce all-or-nothing loading of plugins which contain
        // an "only one-at-a-time" item, like a Lua script or theme
        private void validate()
        {
            if (m_validated)
                return;
            m_validated = true;

            // determine active plugins including solo Lua
            uint solo_lua_flags = 0;
            var found_hud_lua = false;
            var found_stats_lua = false;
            var found_theme = false;
            for (var i = m_plugins.Count - 1; i >= 0; i--)
            {
                var rit = m_plugins[i];
                rit.overridden_solo = false;
                if (!rit.enabled || !rit.compatible() || !rit.allowed() ||
                    (rit.solo_lua.Length > 0 &&
                     rit.solo_lua_write_access.is_excluded(solo_lua_flags)) ||
                    (found_hud_lua && rit.hud_lua.Length > 0) ||
                    (found_stats_lua && rit.stats_lua.Length > 0) ||
                    (found_theme && rit.theme.Length > 0))
                {
                    rit.overridden_solo = true;
                    continue;
                }

                if (rit.solo_lua.Length > 0)
                {
                    solo_lua_flags |= rit.solo_lua_write_access.get_exclusive_flags();
                }

                if (rit.hud_lua.Length > 0)
                    found_hud_lua = true;
                if (rit.stats_lua.Length > 0)
                    found_stats_lua = true;
                if (rit.theme.Length > 0)
                    found_theme = true;
            }

            // determine active plugins excluding solo Lua
            found_hud_lua = false;
            found_stats_lua = false;
            found_theme = false;
            for (var i = m_plugins.Count - 1; i >= 0; i--)
            {
                var rit = m_plugins[i];
                rit.overridden = false;
                if (!rit.enabled || !rit.compatible() || !rit.allowed() ||
                    (rit.solo_lua.Length > 0) ||
                    (found_hud_lua && rit.hud_lua.Length > 0) ||
                    (found_stats_lua && rit.stats_lua.Length > 0) ||
                    (found_theme && rit.theme.Length > 0))
                {
                    rit.overridden = true;
                    continue;
                }

                if (rit.hud_lua.Length > 0)
                    found_hud_lua = true;
                if (rit.stats_lua.Length > 0)
                    found_stats_lua = true;
                if (rit.theme.Length > 0)
                    found_theme = true;
            }
        }

        // ForgePlus: validates (as everything that uses the plugins does first), so valid() can be asked of each
        public void ensure_validated()
        {
            validate();
        }
    }
}
