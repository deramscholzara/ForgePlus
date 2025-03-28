﻿using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public class Button_SelectDataFile : MonoBehaviour
    {
        public const string defaultPathText = "select a file...";

        [SerializeField]
        private TMPro.TextMeshProUGUI filePathDisplayText = null;

        [SerializeField]
        private DataFileTypes dataFileType = DataFileTypes.Unspecified;

        [SerializeField]
        private Button_UnloadDataFile unloadButton = null;

        public void OnClick()
        {
            FileSettings.Instance.ShowSelectionBrowser(dataFileType);
        }

        private void OnPathUpdated(DataFileTypes type, string newPath)
        {
            if (type == dataFileType)
            {
                RefreshPath(newPath);
            }
        }

        private void RefreshPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                filePathDisplayText.text = defaultPathText;

                unloadButton.gameObject.SetActive(false);
            }
            else
            {
                filePathDisplayText.text = path.Replace("\\", "\\\\");

                unloadButton.gameObject.SetActive(true);
            }
        }

        private void OnEnable()
        {
            FileSettings.Instance.OnPathChanged += OnPathUpdated;
        }

        private void OnDisable()
        {
            FileSettings.Instance.OnPathChanged -= OnPathUpdated;
        }
    }
}
