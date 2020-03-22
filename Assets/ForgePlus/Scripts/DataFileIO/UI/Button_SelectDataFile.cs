using SimpleFileBrowser;
using System.Collections;
using UnityEngine;
using System;
using ForgePlus.DataFileIO.Extensions;

namespace ForgePlus.DataFileIO
{
    public class Button_SelectDataFile : MonoBehaviour
    {
        private const string defaultPathText = "select a file...";

        [SerializeField]
        private TMPro.TextMeshProUGUI filePathDisplayText = null;

        [SerializeField]
        private DataFileTypes dataFileType = DataFileTypes.Unspecified;

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
            filePathDisplayText.text = string.IsNullOrEmpty(path) ? defaultPathText : path;
        }
    }
}
