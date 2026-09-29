using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A place in the layout that shows one panel at a time, and takes no space while it shows none
    public sealed class PanelSlot
    {
        private readonly VisualElement container;

        public UIPanel Panel { get; private set; }

        public PanelSlot(VisualElement container)
        {
            this.container = container;
            container.pickingMode = PickingMode.Ignore;
            container.style.display = DisplayStyle.None;
        }

        public void Show(UIPanel panel)
        {
            Hide();

            Panel = panel;
            panel.Load(container);

            container.style.display = DisplayStyle.Flex;
        }

        // Keeps the panel that's already shown if it's the same type
        public void Show<T>() where T : UIPanel, new()
        {
            if (!(Panel is T))
            {
                Show(new T());
            }
        }

        public void Hide()
        {
            if (Panel == null)
            {
                return;
            }

            Panel.Unload();
            Panel = null;

            container.style.display = DisplayStyle.None;
        }
    }
}
