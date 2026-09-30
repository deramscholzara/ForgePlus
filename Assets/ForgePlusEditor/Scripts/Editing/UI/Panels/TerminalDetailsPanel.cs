using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // What's stored for the terminal chosen from the level's terminals, and for the group chosen from its groups
    // (the group being previewed), in the inspector column
    public class TerminalDetailsPanel : UIPanel
    {
        private TerminalsViewModel terminals;
        private RadioButtonGroup terminalList;
        private RadioButtonGroup groupList;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/TerminalDetails";
            }
        }

        protected override void OnLoaded()
        {
            terminals = ForgePlusUI.Instance.Terminals;
            terminalList = Root.Q<RadioButtonGroup>("terminals");
            groupList = Root.Q<RadioButtonGroup>("groups");

            Root.BindInspectorFields(terminals);

            // What doesn't apply to the group is grayed out, rather than hidden, so the panel's layout stays put
            Root.Q("Permutation").BindEnabled(terminals, nameof(TerminalsViewModel.UsesPermutation));
            Root.Q("Text").BindEnabled(terminals, nameof(TerminalsViewModel.HasText));
            Root.Q("TextStyles").BindEnabled(terminals, nameof(TerminalsViewModel.HasText));
            Root.Q("details").BindEnabled(terminals, nameof(TerminalsViewModel.HasTerminals));

            AddTerminalToggles();
            AddGroupToggles();
            terminalList.BindValue(terminals, nameof(TerminalsViewModel.TerminalIndex));
            groupList.BindValue(terminals, nameof(TerminalsViewModel.GroupIndex));

            terminals.OnTerminalsChanged += AddTerminalToggles;
            terminals.OnTerminalChanged += AddGroupToggles;
        }

        protected override void OnUnloading()
        {
            terminals.OnTerminalsChanged -= AddTerminalToggles;
            terminals.OnTerminalChanged -= AddGroupToggles;
        }

        // A toggle for each terminal, numbered as control panels refer to them
        private void AddTerminalToggles()
        {
            AddToggles(terminalList, terminals.TerminalCount);
            terminalList.SetValueWithoutNotify(terminals.TerminalIndex);
        }

        // A toggle for each of the terminal's groups, numbered by their order in it
        private void AddGroupToggles()
        {
            AddToggles(groupList, terminals.GroupCount);
            groupList.SetValueWithoutNotify(terminals.GroupIndex);
        }

        private static void AddToggles(RadioButtonGroup list, int count)
        {
            list.Clear();

            var template = LoadTemplate("RadioToggle");
            for (var i = 0; i < count; i++)
            {
                var instance = template.Instantiate();
                instance.AddToClassList("fp-index-list__item");
                instance.Q<RadioButton>().text = i.ToString();
                list.Add(instance);
            }

            list.IgnoreLayoutPicking();
        }
    }
}
