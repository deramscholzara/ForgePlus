using TMPro;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class Label_LevelName : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI label;

        private void Reset()
        {
            label = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            label.enabled = false;

            LevelData.OnLevelOpened += OnLevelOpened;
            LevelData.OnLevelClosed += OnLevelClosed;
        }

        private void OnLevelOpened(string levelName)
        {
            label.text = levelName;
            label.enabled = true;
        }

        private void OnLevelClosed()
        {
            label.enabled = false;
        }
    }
}
