using ForgePlus.UI;
using UnityEngine.InputSystem;

namespace ForgePlus.ApplicationGeneral
{
    // Reads ForgePlusInput's actions, except while a text field has focus (when the keys are for typing), or while the
    // Inputs tab is waiting for a new binding (when the key or button pressed is the binding)
    public static class Hotkeys
    {
        public static bool IsPressed(InputAction action)
        {
            return action.IsPressed() && !IsBlocked();
        }

        public static bool WasPressed(InputAction action)
        {
            return action.WasPressedThisFrame() && !IsBlocked();
        }

        // Ctrl or Command, held for a shortcut (whose key then isn't its own hotkey)
        public static bool IsShortcutModifierPressed
        {
            get
            {
                var keyboard = Keyboard.current;

                return keyboard != null && (keyboard.ctrlKey.isPressed || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed);
            }
        }

        public static float ReadAxis(InputAction action)
        {
            return IsBlocked() ? 0f : action.ReadValue<float>();
        }

        private static bool IsBlocked()
        {
            var ui = ForgePlusUI.Instance;

            return ui && ui.IsEditingText || InputBindings.IsRebinding;
        }
    }
}
