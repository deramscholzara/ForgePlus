using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The secondary modes (tools) that the current primary mode can use; ForgePlusUI reloads it when that changes
    public class ToolModesPanel : UIPanel
    {
        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/ToolModes";
            }
        }

        protected override void OnLoaded()
        {
            var modes = ModeManager.Instance.AvailableSecondaryModes;
            var group = Root.Q<RadioButtonGroup>("tool-modes");
            var template = LoadTemplate("RadioToggle");

            for (var i = 0; i < modes.Count; i++)
            {
                var instance = template.Instantiate();
                instance.AddToClassList("fp-tool-modes__mode");

                if (i == modes.Count - 1)
                {
                    instance.AddToClassList("fp-tool-modes__mode--last");
                }

                instance.Q<RadioButton>().text = ModeLabel(modes[i]);
                group.Add(instance);
            }

            Root.IgnoreLayoutPicking();

            // The group's value is the mode's position among the available ones
            var binding = group.BindValue(ForgePlusUI.Instance.Editor, nameof(EditorViewModel.SecondaryMode));
            binding.sourceToUiConverters.AddConverter((ref ModeManager.SecondaryModes mode) => IndexOf(modes, mode));
            binding.uiToSourceConverters.AddConverter((ref int index) => index >= 0 && index < modes.Count ? modes[index] : ModeManager.SecondaryModes.Selection);
        }

        private static int IndexOf(IReadOnlyList<ModeManager.SecondaryModes> modes, ModeManager.SecondaryModes mode)
        {
            for (var i = 0; i < modes.Count; i++)
            {
                if (modes[i] == mode)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string ModeLabel(ModeManager.SecondaryModes mode)
        {
            switch (mode)
            {
                case ModeManager.SecondaryModes.Painting:
                    return Strings.Get(Strings.Common, "ToolModes.Paint");
                case ModeManager.SecondaryModes.Editing:
                    return Strings.Get(Strings.Common, "ToolModes.Edit");
                default:
                    return Strings.Get(Strings.Common, "ToolModes.Select");
            }
        }
    }
}
