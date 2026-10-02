using ForgePlus.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A part of the UI with its own layout asset (under Resources), which is loaded while the panel is shown,
    // and unloaded, with its elements and bindings, when it's hidden
    public abstract class UIPanel
    {
        private VisualTreeAsset layout;

        public VisualElement Root { get; private set; }

        protected abstract string LayoutPath { get; }

        public void Load(VisualElement parent)
        {
            layout = Resources.Load<VisualTreeAsset>(LayoutPath);

            if (!layout)
            {
                Debug.LogError($"UI layout \"{LayoutPath}\" was not found in Resources.");
                return;
            }

            Root = layout.Instantiate();
            Root.pickingMode = PickingMode.Ignore;
            Root.AddToClassList("fp-panel");
            Root.IgnoreLayoutPicking();
            parent.Add(Root);

            // Before the panel's own setup, which may show text of its own in place of the layout's
            Strings.Localize(Root);

            OnLoaded();
        }

        public void Unload()
        {
            if (Root == null)
            {
                return;
            }

            OnUnloading();

            Root.ClearAllBindings();
            Root.RemoveFromHierarchy();
            Root = null;

            Resources.UnloadAsset(layout);
            layout = null;
        }

        protected virtual void OnLoaded()
        {
        }

        protected virtual void OnUnloading()
        {
        }

        // Templates used for elements the panel creates itself (such as list items)
        protected static VisualTreeAsset LoadTemplate(string templateName)
        {
            return Resources.Load<VisualTreeAsset>($"UI/Templates/{templateName}");
        }
    }
}
