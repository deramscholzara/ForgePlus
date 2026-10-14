using AlephOne;
using RuntimeCore.Entities;
using RuntimeCore.Materials;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    // The MML the scenario's scripts and plugins (and a level's own) change Aleph One's definitions with, applied as Aleph
    // One applies them when a level starts (XML_LevelScript.cpp: ResetLevelScript, RunLevelScript, RunScriptChunks): back
    // to the definitions' defaults, then the Scripts folder's MML, then each plugin's in use, then the map's level script's
    // for the level, then the level's own. Plugins are found as Aleph One finds them (a Plugin.xml in any folder in the
    // Plugins folder), and each is on (or off) as it was last set to be, or as its Plugin.xml says to start.
    public class MmlLoading : OnDemandSingleton<MmlLoading>
    {
        private const string PluginStatesPlayerPrefsKey = "PluginStates";

        // Aleph One's OpenGL renderer is the one ForgePlus shows levels like, so shapes patches that require it apply
        public const bool IsOpenGL = true;

        private bool isInitialized;

        // Of the texture substitutions last applied, so changes to them can be found
        private string appliedTextureSubstitutions = string.Empty;

        // When the plugins (or whether each is on) change
        public event Action OnPluginsChanged;

        public IReadOnlyList<Plugin> Plugins
        {
            get
            {
                EnsureInitialized();

                return AlephOne.Plugins.instance().plugins;
            }
        }

        public void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            FindPlugins();
            Apply(level: null, levelIndex: -1);
        }

        // Finds the plugins again and reapplies everything (as the Plugins or Scripts folder changed), reopening the open
        // level with it
        public void Reload()
        {
            isInitialized = true;

            FindPlugins();
            ApplyToOpenLevel();

            OnPluginsChanged?.Invoke();
        }

        // Whether the plugin is in use: it's on, it's for this scenario, and it isn't overridden by another (only one of
        // the plugins with a HUD, stats or a theme is used, the last of them)
        public bool IsInUse(Plugin plugin)
        {
            AlephOne.Plugins.instance().ensure_validated();

            return plugin.valid();
        }

        public void SetPluginEnabled(Plugin plugin, bool isEnabled)
        {
            if (plugin.enabled == isEnabled)
            {
                return;
            }

            if (isEnabled)
            {
                AlephOne.Plugins.instance().enable(plugin.directory);
            }
            else
            {
                AlephOne.Plugins.instance().disable(plugin.directory);
            }

            SavePluginStates();
            ApplyToOpenLevel();

            OnPluginsChanged?.Invoke();
        }

        // Before a level is built (LevelData), with its level script's MML and its own
        public void ApplyForLevel(MapLevel level, int levelIndex)
        {
            EnsureInitialized();

            Apply(level, levelIndex);
        }

        private void ApplyToOpenLevel()
        {
            if (LevelEntity_Level.Instance)
            {
                // The level is built again (keeping its edits), which applies the MML for it
                MapsLoading.Instance.RebuildLevel();
            }
            else
            {
                Apply(level: null, levelIndex: -1);
            }
        }

        private void Apply(MapLevel level, int levelIndex)
        {
            SetSearchPath();

            // The scenario (which plugins can require) is read again from the scripts, which are parsed before the plugins
            // are chosen
            Scenario.reset();
            AlephOne.Plugins.instance().invalidate();

            XML_LevelScript.ResetLevelScript(ScriptsFolders());

            if (level != null)
            {
                XML_LevelScript.LoadLevelScripts();
                XML_LevelScript.RunLevelScript(levelIndex);

                if (level.loaded_wad != null && level.loaded_wad.preserved_chunks.TryGetValue(tags.MMLS_TAG, out var mmlsChunk))
                {
                    XML_LevelScript.RunScriptChunks(mmlsChunk);
                }
            }
            else
            {
                XML_LevelScript.UnloadLevelScripts();
            }

            LogMessages();

            // Plugins' shapes patches are applied as the shapes load
            ShapesLoading.Instance.ReloadIfShapesPatchesChanged();

            var textureSubstitutions = DescribeTextureSubstitutions();
            if (textureSubstitutions != appliedTextureSubstitutions)
            {
                appliedTextureSubstitutions = textureSubstitutions;

                MmlTextures.Clear();
                MaterialGeneration_Sprites.ClearCollection();

                if (!LevelEntity_Level.Instance)
                {
                    MaterialGeneration_Geometry.ClearCollection();
                }
            }
        }

        // Relative paths are looked for in the scenario's folders: those the Scripts and Plugins folders are in
        private void SetSearchPath()
        {
            shell.data_search_path.Clear();

            foreach (var folder in new[] { DataFileTypes.Scripts, DataFileTypes.Plugins }.Select(FolderPath))
            {
                var scenarioFolder = string.IsNullOrEmpty(folder) ? null : Path.GetDirectoryName(folder);
                if (!string.IsNullOrEmpty(scenarioFolder) && !shell.data_search_path.Contains(scenarioFolder))
                {
                    shell.data_search_path.Add(scenarioFolder);
                }
            }
        }

        private IEnumerable<string> ScriptsFolders()
        {
            var folder = FolderPath(DataFileTypes.Scripts);
            return string.IsNullOrEmpty(folder) ? Enumerable.Empty<string>() : new[] { folder };
        }

        private static string FolderPath(DataFileTypes type)
        {
            return FileSettings.Instance.GetLoadableFilePath(type);
        }

        private void FindPlugins()
        {
            AlephOne.Plugins.instance().enumerate(FolderPath(DataFileTypes.Plugins));

            var states = LoadPluginStates();
            foreach (var plugin in AlephOne.Plugins.instance().plugins)
            {
                if (states.TryGetValue(plugin.directory, out var isEnabled))
                {
                    plugin.enabled = isEnabled;
                }
            }

            AlephOne.Plugins.instance().invalidate();

            LogMessages();
        }

        // Each plugin turned on or off is remembered by its folder (as Aleph One remembers them), so plugins of other
        // scenarios keep theirs too
        private static Dictionary<string, bool> LoadPluginStates()
        {
            var states = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in PlayerPrefs.GetString(PluginStatesPlayerPrefsKey, string.Empty).Split('\n'))
            {
                var separator = line.IndexOf('\t');
                if (separator > 0)
                {
                    states[line.Substring(separator + 1)] = line.Substring(0, separator) == "1";
                }
            }

            return states;
        }

        private static void SavePluginStates()
        {
            var states = LoadPluginStates();
            foreach (var plugin in AlephOne.Plugins.instance().plugins)
            {
                // Only those that differ from how they start
                if (plugin.enabled != plugin.auto_enable)
                {
                    states[plugin.directory] = plugin.enabled;
                }
                else
                {
                    states.Remove(plugin.directory);
                }
            }

            PlayerPrefs.SetString(PluginStatesPlayerPrefsKey, string.Join("\n", states.Select(state => (state.Value ? "1" : "0") + "\t" + state.Key)));
            PlayerPrefs.Save();
        }

        private static string DescribeTextureSubstitutions()
        {
            var description = new StringBuilder();
            foreach (var (collection, clut, bitmap, options) in OGL_Subst_Texture_Def.OGL_AllTextureOptions())
            {
                description.Append(collection).Append('.').Append(clut).Append('.').Append(bitmap).Append('=')
                           .Append(options.NormalColors.GetPath()).Append('|').Append(options.NormalMask.GetPath()).Append('\n');
            }

            return description.ToString();
        }

        private static void LogMessages()
        {
            foreach (var (isError, message) in Logging.take_messages())
            {
                if (isError)
                {
                    Debug.LogWarning($"MML: {message}");
                }
                else
                {
                    Debug.Log($"MML: {message}");
                }
            }
        }
    }
}
