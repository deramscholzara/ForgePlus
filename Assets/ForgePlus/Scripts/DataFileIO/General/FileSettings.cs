using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO.Extensions;
using SimpleFileBrowser;
using System;
using System.IO;
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

        public event Action<DataFileTypes, string> OnPathChanged;

        public void ShowSelectionBrowser(DataFileTypes type)
        {
            ShowSelectionBrowserCoroutine(type);
        }

        public string GetFilePath(DataFileTypes type)
        {
            return PlayerPrefs.GetString(GetPlayerPrefsKey(type), string.Empty);
        }

        public void UpdateFilePath(DataFileTypes type, string filePath, bool loadFile)
        {
            PlayerPrefs.SetString(GetPlayerPrefsKey(type), filePath);

            OnPathChanged?.Invoke(type, filePath);

            if (loadFile)
            {
                try
                {
                    LoadFile(type); // TODO: add an arg to auto-load the first level (for saving)
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Attempt to load file at path \"{filePath}\" failed with exception: {exception}");

                    UnloadFile(type);
                }
            }
        }

        public void UnloadFile(DataFileTypes type)
        {
            switch (type)
            {
                case DataFileTypes.Maps:
                    MapsLoading.Instance.UnloadFile();
                    break;
                case DataFileTypes.Shapes:
                    ShapesLoading.Instance.UnloadFile();
                    break;
                case DataFileTypes.Physics:
                    Debug.LogWarning("Physics unloading not yet supported.");
                    break;
                case DataFileTypes.Sounds:
                    Debug.LogWarning("Sounds unloading not yet supported.");
                    break;
                case DataFileTypes.Images:
                    Debug.LogWarning("Images unloading not yet supported.");
                    break;
            }

            UpdateFilePath(type, filePath: string.Empty, loadFile: false);
        }

        private async void ShowSelectionBrowserCoroutine(DataFileTypes type)
        {
            UIBlocking.Instance.Block();

            FileBrowser.SetFilters(showAllFilesFilter: true, new FileBrowser.Filter(type.ToString(), type.FileExtensionWithPeriod()));
            FileBrowser.SetDefaultFilter(type.FileExtensionWithPeriod());

            var initialDirectory = GetFilePath(type);
            initialDirectory = string.IsNullOrEmpty(initialDirectory) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : Path.GetDirectoryName(initialDirectory);

            await FileBrowser.WaitForLoadDialog(
                folderMode: false,
                initialPath: initialDirectory,
                title: $"Choose {type} file",
                loadButtonText: "Load");

            if (FileBrowser.Success)
            {
                var path = FileBrowser.Result;

                UpdateFilePath(type, filePath: path, loadFile: true);
            }

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
