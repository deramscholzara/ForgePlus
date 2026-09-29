using ForgePlus.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace ForgePlus.ApplicationGeneral
{
    // A titled list of options, which completes with the chosen option (or null for Cancel)
    public class ObjectSelectorDialog : UIPanel
    {
        // Continues after the click that completes it, rather than inside it
        private readonly TaskCompletionSource<string> selection = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly string title;
        private readonly IList<string> options;
        private readonly IList<string> optionLabels;

        public ObjectSelectorDialog(string title, IList<string> options, IList<string> optionLabels)
        {
            this.title = title;
            this.options = options;
            this.optionLabels = optionLabels;
        }

        public Task<string> Selection
        {
            get
            {
                return selection.Task;
            }
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Dialogs/ObjectSelectorDialog";
            }
        }

        protected override void OnLoaded()
        {
            Root.Q<Label>("title").text = title;

            var optionsContainer = Root.Q("options");
            var buttonTemplate = LoadTemplate("Button");

            for (var i = 0; i < options.Count; i++)
            {
                var label = optionLabels != null && optionLabels.Count > i ?
                            optionLabels[i] :
                            options[i];

                AddOption(optionsContainer, buttonTemplate, label, options[i]);
            }

            AddOption(optionsContainer, buttonTemplate, "Cancel", null);
        }

        protected override void OnUnloading()
        {
            // Unloaded before a choice was made (such as when the UI is torn down)
            selection.TrySetResult(null);
        }

        private void AddOption(VisualElement container, VisualTreeAsset buttonTemplate, string label, string option)
        {
            var instance = buttonTemplate.Instantiate();
            instance.AddToClassList("fp-dialog__option");

            var button = instance.Q<Button>();
            button.text = label;
            button.clicked += () => selection.TrySetResult(option);

            container.Add(instance);
        }
    }
}
