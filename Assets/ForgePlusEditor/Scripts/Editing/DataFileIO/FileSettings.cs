using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO.Extensions;
using SFB;
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

        private event Action<DataFileTypes, string> OnPathChanged_Sender;

        public event Action<DataFileTypes, string> OnPathChanged
        {
            add
            {
                OnPathChanged_Sender += value;
                value.Invoke(DataFileTypes.Maps, GetFilePath(DataFileTypes.Maps));
                value.Invoke(DataFileTypes.Shapes, GetFilePath(DataFileTypes.Shapes));

                // TODO: uncomment these when ready for them.
                ////value.Invoke(DataFileTypes.Physics, GetFilePath(DataFileTypes.Physics));
                ////value.Invoke(DataFileTypes.Sounds, GetFilePath(DataFileTypes.Sounds));
                ////value.Invoke(DataFileTypes.Images, GetFilePath(DataFileTypes.Images));
            }
            remove { OnPathChanged_Sender -= value; }
        }

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

            OnPathChanged_Sender?.Invoke(type, filePath);

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

        private void ShowSelectionBrowserCoroutine(DataFileTypes type)
        {
            UIBlocking.Instance.Block();

            var initialDirectory = GetFilePath(type);
            initialDirectory = string.IsNullOrEmpty(initialDirectory)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : Path.GetDirectoryName(initialDirectory);

            StandaloneFileBrowser.OpenFilePanelAsync(
                title: $"Choose {type} file",
                directory: initialDirectory,
                type.FileExtension(),
                multiselect: false,
                cb: openPaths => HandleSelectionBrowserResponse(openPaths, type));

            UIBlocking.Instance.Unblock();
        }

        private void HandleSelectionBrowserResponse(string[] openPaths, DataFileTypes type)
        {
            if (openPaths.Length > 0)
            {
                var openPath = openPaths[0];
                if (!string.IsNullOrEmpty(openPath) && !string.IsNullOrWhiteSpace(openPath))
                {
                    UpdateFilePath(type, filePath: openPath, loadFile: true);
                }
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