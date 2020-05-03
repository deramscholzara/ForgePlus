using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO.Extensions;
using ForgePlus.LevelManipulation;
using SimpleFileBrowser;
using System;
using System.IO;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public class MapsSaving : OnDemandSingleton<MapsSaving>
    {
        public MapsData CurrentMapsData { private get; set; }

        public delegate void OnSaveCompleteDelegate();
        public event OnSaveCompleteDelegate OnSaveCompleted;

        public void Save()
        {
            ShowSelectionBrowserCoroutine();
        }

        private async void ShowSelectionBrowserCoroutine()
        {
            UIBlocking.Instance.Block();

            var type = DataFileTypes.Maps;

            FileBrowser.SetFilters(showAllFilesFilter: false, new FileBrowser.Filter(type.ToString(), type.FileExtensionWithPeriod()));
            FileBrowser.SetDefaultFilter(type.FileExtensionWithPeriod());

            var initialPath = FileSettings.Instance.GetFilePath(type);
            var initialDirectory = Path.GetDirectoryName(initialPath);
            initialPath = Path.Combine(initialDirectory, FPLevel.Instance.Level.Name);

            await FileBrowser.WaitForSaveDialog(
                folderMode: false,
                initialPath: initialDirectory,
                initialFileName: FPLevel.Instance.Level.Name,
                title: $"Choose {type} save location",
                saveButtonText: "Save");

            if (FileBrowser.Success)
            {
                var path = FileBrowser.Result;
                path = path.Replace(type.FileExtensionWithPeriod().ToLower(), type.FileExtensionWithPeriod());

                try
                {
                    SaveLevelToUnmergedMapFile(path);

                    FileSettings.Instance.UpdateFilePath(type, filePath: path, loadFile: false);

                    OnSaveCompleted?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Attempt to save file at path \"{path}\" failed with exception: {exception}");
                }
            }

            UIBlocking.Instance.Unblock();
        }

        private void SaveLevelToUnmergedMapFile(string savePath)
        {
            if (CurrentMapsData == null)
            {
                throw new IOException($"Tried saving Level with no MapsData loaded.");
            }

            CurrentMapsData.SaveCurrentLevel(savePath);
        }
    }
}
