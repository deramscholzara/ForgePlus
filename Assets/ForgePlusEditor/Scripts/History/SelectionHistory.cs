#if !NO_EDITING
using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.History
{
    // Records selecting and deselecting as undoable actions: once a frame (after LevelHistory records any pending edit),
    // what's selected is compared with how it was last recorded. What's selected is kept by kind and index, so it's found
    // again once the level's entities are made again.
    //
    // What changes the selection without being a choice of what to select isn't recorded, but taken as how the selection
    // now is: switching modes (which carries the selection over to what the new mode selects), opening a level, and
    // undoing or redoing (including the rebuild that may follow). Undoing or redoing a selection switches back to the mode
    // it was made in.
    [AutoStaticsCleanup]
    public static partial class SelectionHistory
    {
        private static List<(string Key, Func<ISelectable> Finder)> recorded = new List<(string, Func<ISelectable>)>();
        private static ModeManager.PrimaryModes recordedMode;
        private static bool isRetakeRequested = true;

        // While a selection is being made over many frames (such as by rectangle), which is recorded once it's done
        public static bool IsPaused { get; set; }

        // The selection, as it is once whatever's changing it now is done, is taken as it is (rather than recorded)
        public static void Retake()
        {
            isRetakeRequested = true;
        }

        // Once a frame (LevelHistory)
        public static void Sample()
        {
            var selectionManager = SelectionManager.Instance;
            if (!selectionManager || !LevelEntity_Level.Instance)
            {
                return;
            }

            // Not until the rebuild an edit (or undo) began is done, and its selection is chosen again
            if (IsPaused || LevelEditing.IsRebuildPending || UndoHistory.IsUndoingOrRedoing)
            {
                return;
            }

            var current = Capture(selectionManager);
            var mode = ModeManager.Instance.PrimaryMode;

            if (isRetakeRequested || mode != recordedMode)
            {
                isRetakeRequested = false;
                recorded = current;
                recordedMode = mode;
                return;
            }

            if (HasSameKeys(recorded, current))
            {
                return;
            }

            UndoHistory.Record(new SelectionChange(recorded, current, mode));
            recorded = current;
        }

        private static List<(string Key, Func<ISelectable> Finder)> Capture(SelectionManager selectionManager)
        {
            var captured = new List<(string, Func<ISelectable>)>();
            foreach (var selected in selectionManager.Selection)
            {
                if (LevelEditing.TryGetIdentity(selected, out var key, out var finder))
                {
                    captured.Add((key, finder));
                }
            }

            return captured;
        }

        private static bool HasSameKeys(List<(string Key, Func<ISelectable> Finder)> a, List<(string Key, Func<ISelectable> Finder)> b)
        {
            return a.Count == b.Count && new HashSet<string>(a.Select(entry => entry.Key)).SetEquals(b.Select(entry => entry.Key));
        }

        // Selects what was selected (what's still in the level), in the mode it was selected in
        private static void Apply(List<(string Key, Func<ISelectable> Finder)> selection, ModeManager.PrimaryModes mode)
        {
            if (ModeManager.Instance.PrimaryMode != mode)
            {
                ModeManager.Instance.PrimaryMode = mode;
            }

            SelectionManager.Instance.DeselectAll();

            var multiSelect = false;
            foreach (var (_, finder) in selection)
            {
                var selectable = finder();
                if (selectable != null)
                {
                    SelectionManager.Instance.SelectObject(selectable, multiSelect);
                    multiSelect = true;
                }
            }

            recorded = selection;
            recordedMode = mode;
        }

        private class SelectionChange : IUndoableAction
        {
            private readonly List<(string Key, Func<ISelectable> Finder)> before;
            private readonly List<(string Key, Func<ISelectable> Finder)> after;
            private readonly ModeManager.PrimaryModes mode;

            public SelectionChange(List<(string Key, Func<ISelectable> Finder)> before, List<(string Key, Func<ISelectable> Finder)> after, ModeManager.PrimaryModes mode)
            {
                this.before = before;
                this.after = after;
                this.mode = mode;

                Description = Strings.Get(Strings.Common, after.Count == 0 ? "History.Deselection" : "History.Selection");
            }

            public string Description { get; }

            public void Undo()
            {
                Apply(before, mode);
            }

            public void Redo()
            {
                Apply(after, mode);
            }
        }
    }
}
#endif
