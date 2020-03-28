using SimpleFileBrowser;
using System.Collections;
using UnityEngine;
using System;
using ForgePlus.DataFileIO.Extensions;

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
            FileSettings.Instance.ShowSelectionBrowser(dataFileType, OnPathUpdated);
        }

        private void OnEnable()
        {
            OnPathUpdated(FileSettings.Instance.GetFilePathFromPlayerPrefs(dataFileType));
        }

        private void OnPathUpdated(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                filePathDisplayText.text = defaultPathText;

                unloadButton.gameObject.SetActive(false);
            }
            else
            {
                filePathDisplayText.text = path;

                unloadButton.gameObject.SetActive(true);
            }
        }
    }
}
