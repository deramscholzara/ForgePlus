using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A menu tab whose actions (such as loading a level) are shown in the menu's row of actions, beside its own
    public interface IMenuTab
    {
        VisualElement Actions { get; }
    }
}
