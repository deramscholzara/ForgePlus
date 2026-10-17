#if !NO_EDITING
using ForgePlus.DataFileIO;
using ForgePlus.History;
using ForgePlus.Localization;
using RuntimeCore.Entities;
using System.Threading.Tasks;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    // Before an action that would lose the open level's unsaved edits (opening a level or map file, unloading the map
    // file, or quitting), asks whether to save them now (as a merged save of the map file), discard them, or cancel
    // the action
    [AutoStaticsCleanup]
    public static partial class UnsavedChanges
    {
        private const string SaveOption = "Save";
        private const string DiscardOption = "Discard";

#if !UNITY_EDITOR
        // Once quitting goes ahead, so closing the window doesn't ask again
        private static bool isQuitConfirmed;
#endif

        private static bool isAskingToQuit;

        // Including an edit that's still being recorded (such as text just typed)
        public static bool Exist
        {
            get
            {
                if (!LevelEntity_Level.Instance)
                {
                    return false;
                }

                UndoHistory.Recorder?.RecordPendingEdit();

                return UndoHistory.HasUnsavedChanges;
            }
        }

        // Whether to go ahead with the action: there's nothing unsaved, they were saved, or they're to be discarded
        public static async Task<bool> Confirm()
        {
            if (!Exist)
            {
                return true;
            }

            var result = await DialogManager.Instance.DisplayQueuedDialog(
                title: Strings.Get(Strings.Common, "Dialog.UnsavedChanges.Title"),
                message: Strings.Get(Strings.Common, "Dialog.UnsavedChanges.Message"),
                options: new[] { SaveOption, DiscardOption },
                optionLabels: new[]
                {
                    Strings.Get(Strings.Common, "Dialog.UnsavedChanges.Save"),
                    Strings.Get(Strings.Common, "Dialog.UnsavedChanges.Discard"),
                },
                checkboxLabel: null);

            switch (result.Option)
            {
                case SaveOption:
                    // Cancelling the save cancels the action
                    return await MapsLoading.Instance.SaveMerged();
                case DiscardOption:
                    return true;
                default:
                    return false;
            }
        }

        public static async void Quit()
        {
            if (isAskingToQuit)
            {
                return;
            }

            isAskingToQuit = true;

            try
            {
                if (!await Confirm())
                {
                    return;
                }
            }
            finally
            {
                isAskingToQuit = false;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            isQuitConfirmed = true;
            Application.Quit();
#endif
        }

#if !UNITY_EDITOR
        // Closing the window (rather than pressing Quit) asks too
        [RuntimeInitializeOnLoadMethod]
        private static void RegisterQuitCheck()
        {
            Application.wantsToQuit += OnWantsToQuit;
        }

        private static bool OnWantsToQuit()
        {
            if (isQuitConfirmed || !Exist)
            {
                return true;
            }

            Quit();

            return false;
        }
#endif
    }
}
#endif
