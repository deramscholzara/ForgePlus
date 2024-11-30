#if !NO_EDITING
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO.Extensions;
using RuntimeCore.Entities;
using SFB;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Weland;

namespace ForgePlus.DataFileIO
{
    public partial class MapsLoading : FileLoadingBase<MapsLoading, MapsData, MapFile>
    {
        public event Action OnSaveCompleted;

        public void Save()
        {
            ShowSelectionBrowserCoroutine();
        }

        private void ShowSelectionBrowserCoroutine()
        {
            UIBlocking.Instance.Block();

            var type = DataFileTypes.Maps;
            var initialPath = FileSettings.Instance.GetFilePath(type);
            var initialDirectory = Path.GetDirectoryName(initialPath);
            
            StandaloneFileBrowser.SaveFilePanelAsync(
                title: $"Choose {type} save location",
                directory: initialDirectory,
                defaultName: LevelEntity_Level.Instance.Level.Name,
                type.FileExtension(),
                cb: savePath => HandleSelectionBrowserResponse(savePath, type));
        }

        private void HandleSelectionBrowserResponse(string savePath, DataFileTypes type)
        {
            if (!string.IsNullOrEmpty(savePath) && !string.IsNullOrWhiteSpace(savePath))
            {
                var path = savePath;
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
            if (data == null)
            {
                throw new IOException($"Tried saving Level with no MapsData loaded.");
            }

            data.SaveCurrentLevel(savePath);
        }
    }
}
#endif
