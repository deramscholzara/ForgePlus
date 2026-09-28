using ForgePlus.CameraNavigation;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.LevelManipulation
{
    [RequireComponent(typeof(Button))]
    public class Button_FrameSelected : MonoBehaviour
    {
        [SerializeField]
        private EditorCamera editorCamera = null;

        private Button button;

        public void OnClick()
        {
            editorCamera.FrameSelected();
        }

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void Update()
        {
            button.interactable = SelectionManager.Instance.SelectedObject != null;
        }
    }
}
