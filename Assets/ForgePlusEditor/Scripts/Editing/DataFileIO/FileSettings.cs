using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO.Extensions;
using ForgePlus.Localization;
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

                // Files that are no longer there aren't shown
                value.Invoke(DataFileTypes.Maps, GetLoadableFilePath(DataFileTypes.Maps));
                value.Invoke(DataFileTypes.Shapes, GetLoadableFilePath(DataFileTypes.Shapes));
                value.Invoke(DataFileTypes.Physics, GetLoadableFilePath(DataFileTypes.Physics));
                value.Invoke(DataFileTypes.Sounds, GetLoadableFilePath(DataFileTypes.Sounds));

                // TODO: uncomment this when ready for it.
                ////value.Invoke(DataFileTypes.Images, GetFilePath(DataFileTypes.Images));
            }
            remove { OnPathChanged_Sender -= value; }
        }

        public void ShowSelectionBrowser(DataFileTypes type)
        {
            ShowSelectionBrowserCoroutine(type);
        }

        // Browses for a folder, and loads the first file of each type found in it (unloading the types it doesn't have)
        public void ShowDirectorySearchBrowser()
        {
            UIBlocking.Instance.Block();

            StandaloneFileBrowser.OpenFolderPanelAsync(
                title: Strings.Get(Strings.Menu, "FileBrowser.FindInDirectory.Title"),
                directory: InitialSearchDirectory(),
                multiselect: false,
                cb: HandleDirectorySearchBrowserResponse);
        }

        public string GetFilePath(DataFileTypes type)
        {
            return PlayerPrefs.GetString(GetPlayerPrefsKey(type), string.Empty);
        }

        // The path to load the type from, or empty if there's none. A file that's no longer there (moved, deleted, or on a
        // disconnected drive) has its path cleared, rather than failing to load.
        public string GetLoadableFilePath(DataFileTypes type)
        {
            var path = GetFilePath(type);

            if (!string.IsNullOrEmpty(path) && !File.Exists(path))
            {
                UpdateFilePath(type, filePath: string.Empty, loadFile: false);
                return string.Empty;
            }

            return path;
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
                    PhysicsLoading.Instance.UnloadFile();
                    break;
                case DataFileTypes.Sounds:
                    SoundsLoading.Instance.UnloadFile();
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
                title: Strings.Get(Strings.Menu, "FileBrowser.Open.Title", type.DisplayName()),
                directory: initialDirectory,
                type.OpenFileFilters(),
                multiselect: false,
                cb: openPaths => HandleSelectionBrowserResponse(openPaths, type));
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

        private void HandleDirectorySearchBrowserResponse(string[] openPaths)
        {
            try
            {
                var directory = openPaths.Length > 0 ? openPaths[0] : null;
                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                {
                    return;
                }

                var foundPaths = DataFileDiscovery.Find(directory);

                // Maps last, after the files its levels are shown with
                foreach (var type in new[] { DataFileTypes.Shapes, DataFileTypes.Physics, DataFileTypes.Sounds, DataFileTypes.Maps })
                {
                    if (foundPaths.TryGetValue(type, out var path))
                    {
                        UpdateFilePath(type, filePath: path, loadFile: true);
                    }
                    else
                    {
                        UnloadFile(type);
                    }
                }
            }
            finally
            {
                UIBlocking.Instance.Unblock();
            }
        }

        // The first loaded file's folder (or the desktop)
        private string InitialSearchDirectory()
        {
            foreach (var type in DataFileDiscovery.SearchedTypes)
            {
                var path = GetFilePath(type);
                if (!string.IsNullOrEmpty(path))
                {
                    return Path.GetDirectoryName(path);
                }
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
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
                    PhysicsLoading.Instance.LoadFile();
                    break;
                case DataFileTypes.Sounds:
                    SoundsLoading.Instance.LoadFile();
                    break;
                case DataFileTypes.Images:
                    Debug.LogWarning("Images loading not yet supported.");
                    break;
            }
        }
    }
}