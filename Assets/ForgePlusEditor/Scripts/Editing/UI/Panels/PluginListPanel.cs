using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Localization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The scenario's plugins, in the Files tab's Plugins pop-up, each turned on or off as in Aleph One's Plugins menu.
    // Plugins that aren't for the scenario are grayed out, and those that are on but not in use (as another's HUD,
    // stats, theme or Lua takes their place) say so.
    public class PluginListPanel : UIPanel
    {
        private readonly List<(Toggle Toggle, Plugin Plugin)> toggles = new List<(Toggle Toggle, Plugin Plugin)>();

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/PluginList";
            }
        }

        protected override void OnLoaded()
        {
            var container = Root.Q("plugins");
            var plugins = MmlLoading.Instance.Plugins;

            for (var i = 0; i < plugins.Count; i++)
            {
                var plugin = plugins[i];

                var toggle = new Toggle();
                toggle.AddToClassList("fp-toggle");
                toggle.AddToClassList("fp-toggle--wide-short");
                toggle.AddToClassList("fp-view-option");
                if (i == plugins.Count - 1)
                {
                    toggle.AddToClassList("fp-view-option--last");
                }

                toggle.RegisterValueChangedCallback(changeEvent => MmlLoading.Instance.SetPluginEnabled(plugin, changeEvent.newValue));

                container.Add(toggle);
                toggles.Add((toggle, plugin));
            }

            Refresh();
            MmlLoading.Instance.OnPluginsChanged += Refresh;
        }

        protected override void OnUnloading()
        {
            MmlLoading.Instance.OnPluginsChanged -= Refresh;
        }

        // Whether each is on and in use changes as others are turned on or off
        private void Refresh()
        {
            foreach (var (toggle, plugin) in toggles)
            {
                var isCompatible = plugin.compatible();
                var isOverridden = plugin.enabled && isCompatible && !MmlLoading.Instance.IsInUse(plugin);

                toggle.SetValueWithoutNotify(plugin.enabled);
                toggle.SetEnabled(isCompatible);
                toggle.text = isOverridden ? Strings.Get(Strings.Menu, "FilesTab.Plugin.NotInUse.Text", plugin.name) : plugin.name;
                toggle.tooltip = Tooltip(plugin, isCompatible, isOverridden);
            }
        }

        private static string Tooltip(Plugin plugin, bool isCompatible, bool isOverridden)
        {
            var lines = new List<string>();

            if (!string.IsNullOrEmpty(plugin.description))
            {
                lines.Add(plugin.description);
            }

            if (!string.IsNullOrEmpty(plugin.version))
            {
                lines.Add(Strings.Get(Strings.Menu, "FilesTab.Plugin.Version.Text", plugin.version));
            }

            if (!isCompatible)
            {
                lines.Add(Strings.Get(Strings.Menu, "FilesTab.Plugin.NotForScenario.Text"));
            }
            else if (isOverridden)
            {
                lines.Add(Strings.Get(Strings.Menu, "FilesTab.Plugin.Overridden.Text"));
            }

            if (plugin.mmls.Count == 0 && plugin.shapes_patches.Count == 0)
            {
                lines.Add(Strings.Get(Strings.Menu, "FilesTab.Plugin.NothingShown.Text"));
            }

            lines.Add(plugin.directory);

            return string.Join("\n\n", lines.Where(line => !string.IsNullOrEmpty(line)));
        }
    }
}
