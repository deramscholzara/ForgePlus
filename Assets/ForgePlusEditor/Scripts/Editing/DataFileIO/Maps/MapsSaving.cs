#if !NO_EDITING
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO.Extensions;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using RuntimeCore.Entities;
using SFB;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgePlus.DataFileIO
{
    public partial class MapsLoading : FileLoadingBase<MapsLoading, MapsData, MapsFile>
    {
        private const string KeepChecksumOption = "Keep";
        private const string RegenerateChecksumOption = "Regenerate";

        private const string MapOnlyOption = "MapOnly";
        private const string PhysicsOption = "Physics";
        private const string ResourcesOption = "Resources";
        private const string ResourcesAndPhysicsOption = "ResourcesAndPhysics";

        public event Action OnSaveCompleted;

        // Just the open level, in a map file of its own, with what it uses besides its map (its embedded physics, and its
        // resources from the map file), as chosen in a dialog. Completes with whether it was saved (rather than cancelled).
        public async Task<bool> Save()
        {
            var option = await DialogManager.Instance.DisplayQueuedDialog(
                title: Strings.Get(Strings.Common, "Dialog.SaveWith.Title"),
                message: Strings.Get(Strings.Common, "Dialog.SaveWith.Message"),
                options: new[] { MapOnlyOption, PhysicsOption, ResourcesOption, ResourcesAndPhysicsOption },
                optionLabels: new[]
                {
                    Strings.Get(Strings.Common, "Dialog.SaveWith.MapOnly"),
                    Strings.Get(Strings.Common, "Dialog.SaveWith.Physics"),
                    Strings.Get(Strings.Common, "Dialog.SaveWith.Resources"),
                    Strings.Get(Strings.Common, "Dialog.SaveWith.ResourcesAndPhysics"),
                },
                checkboxLabel: null);

            if (option.Option == null)
            {
                return false;
            }

            var withPhysics = option.Option == PhysicsOption || option.Option == ResourcesAndPhysicsOption;
            var withResources = option.Option == ResourcesOption || option.Option == ResourcesAndPhysicsOption;

            return await ShowSelectionBrowser(merged: false, path => SaveLevelToUnmergedMapFile(path, withPhysics, withResources));
        }

        // Every level of the map file, with the open level as edited, in one map file. Its checksum is kept or
        // regenerated as the settings say, or as chosen in a dialog that asks. Completes with whether it was saved (rather than
        // cancelled).
        public async Task<bool> SaveMerged()
        {
            var choice = SettingsManager.Instance.MergedSaveChecksum;

            if (choice == MergedSaveChecksums.Ask)
            {
                var result = await DialogManager.Instance.DisplayQueuedDialog(
                    title: Strings.Get(Strings.Common, "Dialog.SaveMerged.Title"),
                    message: Strings.Get(Strings.Common, "Dialog.SaveMerged.Message"),
                    options: new[] { KeepChecksumOption, RegenerateChecksumOption },
                    optionLabels: new[]
                    {
                        Strings.Get(Strings.Common, "Dialog.SaveMerged.KeepChecksum"),
                        Strings.Get(Strings.Common, "Dialog.SaveMerged.RegenerateChecksum"),
                    },
                    checkboxLabel: Strings.Get(Strings.Common, "Dialog.SaveMerged.RememberChoice"));

                if (result.Option == null)
                {
                    return false;
                }

                choice = result.Option == KeepChecksumOption ? MergedSaveChecksums.Keep : MergedSaveChecksums.Regenerate;

                if (result.IsChecked)
                {
                    SettingsManager.Instance.MergedSaveChecksum = choice;
                }
            }

            var keepChecksum = choice == MergedSaveChecksums.Keep;
            return await ShowSelectionBrowser(merged: true, path => SaveMergedMapFile(path, keepChecksum));
        }

        // Completes with whether it was saved (rather than cancelled, or failed)
        private Task<bool> ShowSelectionBrowser(bool merged, Action<string> save)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            UIBlocking.Instance.Block();

            var type = DataFileTypes.Maps;
            var initialPath = FileSettings.Instance.GetFilePath(type);
            var initialDirectory = Path.GetDirectoryName(initialPath);
            var defaultName = merged ? Path.GetFileNameWithoutExtension(initialPath) : LevelEntity_Level.Instance.Level.GetLevelName();

            StandaloneFileBrowser.SaveFilePanelAsync(
                title: Strings.Get(Strings.Common, merged ? "Saving.Location.Merged.Title" : "Saving.Location.Title", type.DisplayName()),
                directory: initialDirectory,
                defaultName: defaultName,
                type.FileExtension(),
                cb: savePath => completion.TrySetResult(HandleSelectionBrowserResponse(savePath, type, save)));

            return completion.Task;
        }

        private bool HandleSelectionBrowserResponse(string savePath, DataFileTypes type, Action<string> save)
        {
            var isSaved = false;

            if (!string.IsNullOrEmpty(savePath) && !string.IsNullOrWhiteSpace(savePath))
            {
                var path = savePath;
                path = path.Replace(type.FileExtensionWithPeriod().ToLowerInvariant(), type.FileExtensionWithPeriod());

                try
                {
                    save(path);

                    FileSettings.Instance.UpdateFilePath(type, filePath: path, loadFile: false);

                    OnSaveCompleted?.Invoke();

                    isSaved = true;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Attempt to save file at path \"{path}\" failed with exception: {exception}");
                }
            }

            UIBlocking.Instance.Unblock();

            return isSaved;
        }

        private void SaveLevelToUnmergedMapFile(string savePath, bool withPhysics, bool withResources)
        {
            if (data == null)
            {
                throw new IOException($"Tried saving Level with no MapsData loaded.");
            }

            data.SaveCurrentLevel(savePath, withPhysics, withResources);
        }

        private void SaveMergedMapFile(string savePath, bool keepChecksum)
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
