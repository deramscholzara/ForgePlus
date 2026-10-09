#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.UI;
using RuntimeCore.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ForgePlus.History
{
    // Records the open level's (and its map file's) edits as undoable actions, by comparing the data with how it was
    // last recorded. That's checked when something marks an edit, and after clicks and key presses. Changes made while
    // dragging, holding a mouse button, typing, or in a dialog are recorded together once that ends.
    public class LevelHistory : SingletonMonoBehaviour<LevelHistory>, IUndoRecorder
    {
        public const string MapNameData = "MapsFile.Name";
        public const string RemovedResourcesData = "MapsFile.RemovedResources";

        private readonly List<TrackedData> trackedData = new List<TrackedData>();

        private bool isEditMarked;

        // Drags in progress
        private int gestureCount;

        public static void MarkEdited()
        {
            if (Instance)
            {
                Instance.isEditMarked = true;
            }
        }

        // For an edit made over many frames (such as a drag), recorded as one action when it ends
        public static void BeginGesture()
        {
            if (Instance)
            {
                Instance.gestureCount++;
            }
        }

        public static void EndGesture()
        {
            if (Instance)
            {
                Instance.gestureCount = Math.Max(0, Instance.gestureCount - 1);
                Instance.isEditMarked = true;
            }
        }

        // Undoing or redoing waits for it
        public bool IsEditInProgress
        {
            get
            {
                if (IsDraggingOrBlocked)
                {
                    return true;
                }

                var ui = ForgePlusUI.Instance;

                return AnyMouseButton(button => button.isPressed) || ui && ui.IsEditingText;
            }
        }

        private bool IsDraggingOrBlocked
        {
            get
            {
                return gestureCount > 0 || UIBlocking.Instance && UIBlocking.Instance.IsBlocking;
            }
        }

        public void RecordPendingEdit()
        {
            isEditMarked = false;

            if (trackedData.Count == 0 || UndoHistory.IsUndoingOrRedoing)
            {
                return;
            }

            var changes = new List<DataChange>();
            foreach (var data in trackedData)
            {
                var change = data.RecordChange();
                if (change != null)
                {
                    changes.Add(change);
                }
            }

            if (changes.Count > 0)
            {
                UndoHistory.Record(new LevelEdit(changes));
            }
        }

        private void OnEnable()
        {
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += OnLevelClosed;
            MapsLoading.Instance.OnSaveCompleted += OnSaveCompleted;
            UndoHistory.Recorder = this;
        }

        private void OnDisable()
        {
            var mapsLoading = MapsLoading.Instance;
            if (mapsLoading != null)
            {
                mapsLoading.OnLevelOpened -= OnLevelOpened;
                mapsLoading.OnLevelClosed -= OnLevelClosed;
                mapsLoading.OnSaveCompleted -= OnSaveCompleted;
            }

            if (UndoHistory.Recorder == (IUndoRecorder) this)
            {
                UndoHistory.Recorder = null;
            }
        }

        private void LateUpdate()
        {
            var mouseWasPressed = AnyMouseButton(button => button.wasPressedThisFrame);
            var mouseWasReleased = AnyMouseButton(button => button.wasReleasedThisFrame);
            var keyWasReleased = Keyboard.current != null && Keyboard.current.anyKey.wasReleasedThisFrame;

            if (!isEditMarked && !mouseWasPressed && !mouseWasReleased && !keyWasReleased)
            {
                return;
            }

            // A press ends what was typed, so it isn't recorded with what the click or drag does
            if (mouseWasPressed && !IsDraggingOrBlocked || !IsEditInProgress)
            {
                RecordPendingEdit();
            }
        }

        private static bool AnyMouseButton(Func<ButtonControl, bool> isMatch)
        {
            var mouse = Mouse.current;

            return mouse != null && (isMatch(mouse.leftButton) || isMatch(mouse.rightButton) || isMatch(mouse.middleButton));
        }

        // Rebuilding keeps the level's data (and history). What the build works out in the data isn't an edit.
        private void OnLevelOpened(string levelName)
        {
            if (MapsLoading.Instance.IsRebuildingLevel)
            {
                foreach (var data in trackedData)
                {
                    data.Capture();
                }

                return;
            }

            StartTracking();
        }

        private void OnLevelClosed()
        {
            if (MapsLoading.Instance.IsRebuildingLevel)
            {
                // The edit that's rebuilding it
                RecordPendingEdit();
                return;
            }

            StopTracking();
        }

        // Saving reloads the map file, which isn't an edit
        private void OnSaveCompleted()
        {
            RecordPendingEdit();

            foreach (var data in trackedData)
            {
                data.Capture();
            }
        }

        private void StartTracking()
        {
            StopTracking();

            var level = LevelEntity_Level.Instance ? LevelEntity_Level.Instance.Level : null;
            var mapsFile = MapsLoading.Instance.MapsFile;
            if (level == null || mapsFile == null)
            {
                return;
            }

            var captureStartTime = DateTime.Now;

            // Terminal text is decoded in place when first read (get_indexed_terminal_data), which isn't an edit
            for (short terminalIndex = 0; terminalIndex < level.map_terminal_text.Count; terminalIndex++)
            {
                computer_interface.get_indexed_terminal_data(level, terminalIndex);
            }

            foreach (var field in typeof(MapLevel).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    trackedData.Add(new TrackedList(field.Name, () => (IList) field.GetValue(level)));
                }
                else
                {
                    trackedData.Add(new TrackedValue(field.Name, () => field.GetValue(level), value => field.SetValue(level, value)));
                }
            }

            trackedData.Add(new TrackedValue(MapNameData, () => mapsFile.Name));
            trackedData.Add(new TrackedValue(RemovedResourcesData, () => mapsFile.Forks.RemovedResources));

            foreach (var data in trackedData)
            {
                data.Capture();
            }

            Debug.Log($"--- LevelHistory: Captured level state in timespan: {DateTime.Now - captureStartTime}");
        }

        private void StopTracking()
        {
            isEditMarked = false;
            gestureCount = 0;
            trackedData.Clear();

            UndoHistory.Clear();
        }

        private class LevelEdit : IUndoableAction
        {
            private readonly List<DataChange> changes;

            public LevelEdit(List<DataChange> changes)
            {
                this.changes = changes;
                Description = LevelEditDescriptions.Describe(changes);
            }

            public string Description { get; }

            public void Undo()
            {
                for (var i = changes.Count - 1; i >= 0; i--)
                {
                    changes[i].Restore(after: false);
                }

                LevelEditRefresh.Refresh(changes);
            }

            public void Redo()
            {
                foreach (var change in changes)
                {
                    change.Restore(after: true);
                }

                LevelEditRefresh.Refresh(changes);
            }
        }
    }
}
#endif
