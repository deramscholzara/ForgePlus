#if !NO_EDITING
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO.Extensions;
using ForgePlus.Extensions;
using RuntimeCore.Entities;
using SFB;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgePlus.DataFileIO
{
    public partial class MapsLoading : FileLoadingBase<MapsLoading, MapsData, MapsFile>
    {
        public event Action OnSaveCompleted;

        private const string KeepChecksumOption = "Keep";
        private const string RegenerateChecksumOption = "Regenerate";

        // Just the open level, in a map file of its own
        public void Save()
        {
            ShowSelectionBrowserCoroutine(merged: false, keepChecksum: false);
        }

        // Every level of the map file, with the open level as edited, in one map file. Its checksum is kept or
        // regenerated as the settings say, or as chosen in a dialog that asks (unless it's cancelled).
        public async void SaveMerged()
        {
            var choice = SettingsManager.Instance.MergedSaveChecksum;

            if (choice == MergedSaveChecksums.Ask)
            {
                var result = await DialogManager.Instance.DisplayQueuedDialog(
                    title: "Save Merged",
                    message: "Keep the map file's checksum, so saved games, films and physics files made for it still find it (even though its levels may have changed), or regenerate it for the saved file (also naming the map after the saved file)?",
                    options: new[] { KeepChecksumOption, RegenerateChecksumOption },
                    optionLabels: new[] { "Keep Checksum", "Regenerate Checksum" },
                    checkboxLabel: "Remember my choice (see Settings)");

                if (result.Option == null)
                {
                    return;
                }

                choice = result.Option == KeepChecksumOption ? MergedSaveChecksums.Keep : MergedSaveChecksums.Regenerate;

                if (result.IsChecked)
                {
                    SettingsManager.Instance.MergedSaveChecksum = choice;
                }
            }

            ShowSelectionBrowserCoroutine(merged: true, keepChecksum: choice == MergedSaveChecksums.Keep);
        }

        private void ShowSelectionBrowserCoroutine(bool merged, bool keepChecksum)
        {
            UIBlocking.Instance.Block();

            var type = DataFileTypes.Maps;
            var initialPath = FileSettings.Instance.GetFilePath(type);
            var initialDirectory = Path.GetDirectoryName(initialPath);
            var defaultName = merged ? Path.GetFileNameWithoutExtension(initialPath) : LevelEntity_Level.Instance.Level.GetLevelName();

            StandaloneFileBrowser.SaveFilePanelAsync(
                title: merged ? $"Choose merged {type} save location" : $"Choose {type} save location",
                directory: initialDirectory,
                defaultName: defaultName,
                type.FileExtension(),
                cb: savePath => HandleSelectionBrowserResponse(savePath, type, merged, keepChecksum));
        }

        private void HandleSelectionBrowserResponse(string savePath, DataFileTypes type, bool merged, bool keepChecksum)
        {
            if (!string.IsNullOrEmpty(savePath) && !string.IsNullOrWhiteSpace(savePath))
            {
                var path = savePath;
                path = path.Replace(type.FileExtensionWithPeriod().ToLowerInvariant(), type.FileExtensionWithPeriod());

                try
                {
                    if (merged)
                    {
                        SaveMergedMapFile(path, keepChecksum);
                    }
                    else
                    {
                        SaveLevelToUnmergedMapFile(path);
                    }

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

        public void SaveMergedMapFile(string savePath, bool keepChecksum)
        {
            if (data == null)
            {
                throw new IOException($"Tried saving merged levels with no MapsData loaded.");
            }

            data.SaveMerged(savePath, keepChecksum);
        }
    }
}
#endif
