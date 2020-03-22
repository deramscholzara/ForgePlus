using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO.Extensions;
using SimpleFileBrowser;
using System;
using UnityEngine;


namespace ForgePlus.DataFileIO
{
    public enum DataFileTypes
    {
        Unspecified = -1,
        Maps,
        Shapes,
        Sounds,
        Physics,
        Images,
    }

    public class FileSettings : OnDemandSingletonMonoBehaviour<FileSettings>
    {
        private const string playerPrefsPrefix = "FilePath_";

        private bool dialogIsOpen { get; set; }

        public void ShowSelectionBrowser(DataFileTypes type, Action<string> callback)
        {
            if (!dialogIsOpen)
            {
                ShowSelectionBrowserCoroutine(type, callback);
            }
        }

        public string GetFilePathFromPlayerPrefs(DataFileTypes type)
        {
            return PlayerPrefs.GetString(GetPlayerPrefsKey(type), string.Empty);
        }

        private async void ShowSelectionBrowserCoroutine(DataFileTypes type, Action<string> callback)
        {
            UIBlocking.Instance.Block();
            dialogIsOpen = true;

            FileBrowser.SetFilters(showAllFilesFilter: true, new FileBrowser.Filter(type.ToString(), type.FileExtensionWithPeriod()));
            FileBrowser.SetDefaultFilter(type.FileExtensionWithPeriod());

            await FileBrowser.WaitForLoadDialog(false, null, $"Choose {type} file", "Select");

            if (FileBrowser.Success)
            {
                var result = FileBrowser.Result;

                PlayerPrefs.SetString(GetPlayerPrefsKey(type), result);

                callback(result);

                LoadFile(type);
            }

            dialogIsOpen = false;
            UIBlocking.Instance.Unblock();
        }

        private string GetPlayerPrefsKey(DataFileTypes type)
        {
            return string.Concat(playerPrefsPrefix, type);
        }

        private void LoadFile(DataFileTypes type)
        {
            switch (type)
            {
                case DataFileTypes.Maps:
                    MapsLoading.Instance.LoadFile();
                    break;
                case DataFileTypes.Shapes:
                    ShapesLoading.Instance.LoadFile();
                    break;
                case DataFileTypes.Physics:
                    Debug.LogWarning("Physics loading not yet supported.");
                    break;
                case DataFileTypes.Sounds:
                    Debug.LogWarning("Sounds loading not yet supported.");
                    break;
                case DataFileTypes.Images:
                    Debug.LogWarning("Images loading not yet supported.");
                    break;
            }
        }
    }
}
