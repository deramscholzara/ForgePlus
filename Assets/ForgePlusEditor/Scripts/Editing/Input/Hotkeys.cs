using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
    public static class Hotkeys
    {
        public static bool GetKey(KeyCode key)
        {
            return Input.GetKey(key) && !IsTypingInInputField();
        }

        public static bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key) && !IsTypingInInputField();
        }

        public static bool IsTypingInInputField()
        {
            var selectedGameObject = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (!selectedGameObject)
            {
                return false;
            }

            var tmpInputField = selectedGameObject.GetComponent<TMP_InputField>();
            if (tmpInputField && tmpInputField.isFocused)
            {
                return true;
            }

            var inputField = selectedGameObject.GetComponent<InputField>();
            return inputField && inputField.isFocused;
        }
    }
}
