using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.DataFileIO
{
    public class Button_SaveLevel : MonoBehaviour
    {
        [SerializeField]
        private Button button = null;

        public void OnClick()
        {
            MapsLoading.Instance.Save();
        }

        private void Start()
        {
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += OnLevelClosed;
        }

        private void OnLevelOpened(string levelName)
        {
            button.interactable = true;
        }

        private void OnLevelClosed()
        {
            button.interactable = false;
        }
    }
}
