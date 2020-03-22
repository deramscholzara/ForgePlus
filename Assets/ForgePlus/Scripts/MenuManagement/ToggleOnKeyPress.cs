using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.CommonUI
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleOnKeyPress : MonoBehaviour
    {
        // TODO: Convert this to use Unity's new input system.
        [SerializeField]
        private KeyCode[] keys = new KeyCode[] { };

        [SerializeField]
        private Toggle toggle = null;

        private void Reset()
        {
            toggle = GetComponent<Toggle>();
        }

        private void Update()
        {
            foreach (var key in keys)
            {
                if (Input.GetKeyDown(key))
                {
                    toggle.isOn = !toggle.isOn;
                }
            }
        }
    }
}
