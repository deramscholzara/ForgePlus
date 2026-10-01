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

        private const string MapOnlyOption = "MapOnly";
        private const string PhysicsOption = "Physics";
        private const string ResourcesOption = "Resources";
        private const string ResourcesAndPhysicsOption = "ResourcesAndPhysics";

        // Just the open level, in a map file of its own, with what it uses besides its map (its embedded physics, and its
        // resources from the map file), as chosen in a dialog (unless it's cancelled)
        public async void Save()
        {
            var option = await DialogManager.Instance.DisplayQueuedDialog(
                title: "Save with...",
                message: "Physics: the physics saved in the level. Resources: what it uses from the map file's resources (its chapter screen, its terminals' pictures, its part of the level script, and the end screens).",
                options: new[] { MapOnlyOption, PhysicsOption, ResourcesOption, ResourcesAndPhysicsOption },
                optionLabels: new[] { "Map Only", "Physics", "Resources", "Resources and Physics" },
                checkboxLabel: null);

            if (option.Option == null)
            {
                return;
            }

            var withPhysics = option.Option == PhysicsOption || option.Option == ResourcesAndPhysicsOption;
            var withResources = option.Option == ResourcesOption || option.Option == ResourcesAndPhysicsOption;

            ShowSelectionBrowserCoroutine(merged: false, path => SaveLevelToUnmergedMapFile(path, withPhysics, withResources));
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

            var keepChecksum = choice == MergedSaveChecksums.Keep;
            ShowSelectionBrowserCoroutine(merged: true, path => SaveMergedMapFile(path, keepChecksum));
        }

        private void ShowSelectionBrowserCoroutine(bool merged, Action<string> save)
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
                cb: savePath => HandleSelectionBrowserResponse(savePath, type, save));
        }

        private void HandleSelectionBrowserResponse(string savePath, DataFileTypes type, Action<string> save)
        {
            if (!string.IsNullOrEmpty(savePath) && !string.IsNullOrWhiteSpace(savePath))
            {
                var path = savePath;
                path = path.Replace(type.FileExtensionWithPeriod().ToLowerInvariant(), type.FileExtensionWithPeriod());

                try
                {
                    save(path);

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

        public void SaveLevelToUnmergedMapFile(string savePath, bool withPhysics, bool withResources)
        {
            if (data == null)
            {
                throw new IOException($"Tried saving Level with no MapsData loaded.");
            }

            data.SaveCurrentLevel(savePath, withPhysics, withResources);
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
