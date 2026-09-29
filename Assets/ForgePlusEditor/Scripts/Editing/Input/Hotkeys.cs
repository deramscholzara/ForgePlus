using ForgePlus.UI;
using UnityEngine.InputSystem;

namespace ForgePlus.ApplicationGeneral
{
    // Reads ForgePlusInput's actions, except while a text field has focus (when the keys are for typing)
    public static class Hotkeys
    {
        public static bool IsPressed(InputAction action)
        {
            return action.IsPressed() && !IsTypingInInputField();
        }

        public static bool WasPressed(InputAction action)
        {
            return action.WasPressedThisFrame() && !IsTypingInInputField();
        }

        public static float ReadAxis(InputAction action)
        {
            return IsTypingInInputField() ? 0f : action.ReadValue<float>();
        }

        private static bool IsTypingInInputField()
        {
            var ui = ForgePlusUI.Instance;

            return ui && ui.IsEditingText;
        }
    }
}
