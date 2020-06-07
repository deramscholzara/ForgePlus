using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    [RequireComponent(typeof(Canvas))]
    public class ToggleAllUI : MonoBehaviour
    {
        // TODO: Convert this to use Unity's new input system.
        [SerializeField]
        private KeyCode[] activateKeys = new KeyCode[] { KeyCode.BackQuote, KeyCode.Escape, KeyCode.Tab };

        [SerializeField]
        private KeyCode[] deactivateKeys = new KeyCode[] { KeyCode.BackQuote };

        private Canvas thisCanvas;

        private void Awake()
        {
            if (!thisCanvas)
            {
                thisCanvas = GetComponent<Canvas>();
            }
        }

        private void Update()
        {
            if (thisCanvas.enabled)
            {
                CheckInput(deactivateKeys);
            }
            else
            {
                CheckInput(activateKeys);
            }
        }

        private void CheckInput(KeyCode[] currentKeys)
        {
            foreach (var key in currentKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    thisCanvas.enabled = !thisCanvas.enabled;
                }
            }
        }
    }
}
