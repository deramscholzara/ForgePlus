#if !NO_EDITING
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.History
{
    // The actions that can be undone (newest last) and redone (next to redo last). Recording an action forgets what
    // could be redone, and the oldest actions past the capacity are forgotten.
    [AutoStaticsCleanup]
    public static partial class UndoHistory
    {
        // How many actions can be undone (the Undo Steps setting, which SettingsManager applies)
        public const int MinimumCapacity = 1;
        public const int MaximumCapacity = 10000;
        public const int DefaultCapacity = 50;

        private static int capacity = DefaultCapacity;

        private static List<IUndoableAction> undoActions = new List<IUndoableAction>();
        private static List<IUndoableAction> redoActions = new List<IUndoableAction>();

        // The last data-changing action that had been done when the data was saved (null if there was none), and whether
        // that state can no longer be undone or redone back to (such as when the action before it was forgotten)
        private static IUndoableAction savedAction;
        private static bool isSavedStateUnreachable;

        // When what can be undone or redone changes
        public static event Action OnChanged;

        public static IUndoRecorder Recorder { get; set; }

        // Lowering it forgets the oldest actions that can be undone, then those furthest from being redone, so there
        // are never more actions (to undo and redo together) than it
        public static int Capacity
        {
            get
            {
                return capacity;
            }
            set
            {
                var newCapacity = Math.Clamp(value, MinimumCapacity, MaximumCapacity);
                if (newCapacity == capacity)
                {
                    return;
                }

                capacity = newCapacity;

                if (Trim())
                {
                    OnChanged?.Invoke();
                }
            }
        }

        public static bool CanUndo
        {
            get
            {
                return undoActions.Count > 0 && !IsUndoingOrRedoing && !IsBusy;
            }
        }

        public static bool CanRedo
        {
            get
            {
                return redoActions.Count > 0 && !IsUndoingOrRedoing && !IsBusy;
            }
        }

        // Null when there's none
        public static string UndoDescription
        {
            get
            {
                return undoActions.Count > 0 ? undoActions[undoActions.Count - 1].Description : null;
            }
        }

        public static string RedoDescription
        {
            get
            {
                return redoActions.Count > 0 ? redoActions[redoActions.Count - 1].Description : null;
            }
        }

        private static bool IsBusy
        {
            get
            {
                return Recorder != null && Recorder.IsEditInProgress;
            }
        }

        // While an action is being undone or redone (when the changes it makes aren't new actions)
        public static bool IsUndoingOrRedoing { get; private set; }

        // Whether the data has been edited since it was saved (or opened), going by the actions done since then. Selecting
        // isn't an edit, and undoing back to how it was saved leaves nothing unsaved.
        public static bool HasUnsavedChanges
        {
            get
            {
                return isSavedStateUnreachable || LastDataAction() != savedAction;
            }
        }

        // The data is now as it was saved
        public static void MarkSaved()
        {
            savedAction = LastDataAction();
            isSavedStateUnreachable = false;
        }

        private static IUndoableAction LastDataAction()
        {
            for (var i = undoActions.Count - 1; i >= 0; i--)
            {
                if (undoActions[i].ChangesData)
                {
                    return undoActions[i];
                }
            }

            return null;
        }

        public static void Record(IUndoableAction action)
        {
            if (action == null || IsUndoingOrRedoing)
            {
                return;
            }

            undoActions.Add(action);
            redoActions.Clear();
            Trim();

            OnChanged?.Invoke();
        }

        // Joined to the last action recorded, as one action (named after that one), such as the selection an edit makes;
        // recorded on its own if there's none
        public static void RecordWithLast(IUndoableAction action)
        {
            if (action == null || IsUndoingOrRedoing)
            {
                return;
            }

            if (undoActions.Count == 0)
            {
                Record(action);
                return;
            }

            var last = undoActions[undoActions.Count - 1];
            var joined = new JoinedAction(last, action);
            undoActions[undoActions.Count - 1] = joined;
            redoActions.Clear();

            // A selection joined to the action the data was saved after doesn't change the data
            if (last == savedAction && !action.ChangesData)
            {
                savedAction = joined;
            }

            OnChanged?.Invoke();
        }

        // Two actions as one: undone last to first, and redone first to last
        private class JoinedAction : IUndoableAction
        {
            private readonly IUndoableAction first;
            private readonly IUndoableAction second;

            public JoinedAction(IUndoableAction first, IUndoableAction second)
            {
                this.first = first;
                this.second = second;
            }

            public string Description
            {
                get
                {
                    return first.Description;
                }
            }

            public bool ChangesData
            {
                get
                {
                    return first.ChangesData || second.ChangesData;
                }
            }

            public void Undo()
            {
                second.Undo();
                first.Undo();
            }

            public void Redo()
            {
                first.Redo();
                second.Redo();
            }
        }

        // Returns whether any were forgotten
        private static bool Trim()
        {
            var excessUndo = undoActions.Count - capacity;
            if (excessUndo > 0)
            {
                ForgetSavedActionBefore(excessUndo);

                undoActions.RemoveRange(0, excessUndo);
            }

            var excessRedo = undoActions.Count + redoActions.Count - capacity;
            if (excessRedo > 0)
            {
                if (savedAction != null && redoActions.Take(excessRedo).Contains(savedAction))
                {
                    isSavedStateUnreachable = true;
                }

                redoActions.RemoveRange(0, excessRedo);
            }

            return excessUndo > 0 || excessRedo > 0;
        }

        // As the oldest actions that can be undone (up to count) are forgotten: saved after the last of them that changes
        // data, it's saved as the data is once every action left is undone. Saved before an edit among them, it can no
        // longer be undone back to.
        private static void ForgetSavedActionBefore(int count)
        {
            var savedIndex = savedAction == null ? -1 : undoActions.IndexOf(savedAction);
            if (savedAction != null && (savedIndex < 0 || savedIndex >= count))
            {
                // Saved after an action that's kept (or that can be redone)
                return;
            }

            for (var i = savedIndex + 1; i < count; i++)
            {
                if (undoActions[i].ChangesData)
                {
                    isSavedStateUnreachable = true;
                    return;
                }
            }

            savedAction = null;
        }

        public static void Undo()
        {
            if (IsBusy)
            {
                return;
            }

            Recorder?.RecordPendingEdit();

            if (CanUndo)
            {
                Move(undoActions, redoActions, action => action.Undo());
            }
        }

        public static void Redo()
        {
            if (IsBusy)
            {
                return;
            }

            Recorder?.RecordPendingEdit();

            if (CanRedo)
            {
                Move(redoActions, undoActions, action => action.Redo());
            }
        }

        // Forgets every action (for when what they changed is gone, such as when a level closes), with nothing unsaved
        public static void Clear()
        {
            savedAction = null;
            isSavedStateUnreachable = false;

            if (undoActions.Count == 0 && redoActions.Count == 0)
            {
                return;
            }

            undoActions.Clear();
            redoActions.Clear();

            OnChanged?.Invoke();
        }

        // An action that fails is forgotten (along with every other, since they may rely on its data)
        private static void Move(List<IUndoableAction> from, List<IUndoableAction> to, Action<IUndoableAction> perform)
        {
            var action = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);

            IsUndoingOrRedoing = true;

            try
            {
                perform(action);
                to.Add(action);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                undoActions.Clear();
                redoActions.Clear();

                // The data may be partly changed, so how it was saved is out of reach
                isSavedStateUnreachable = true;
            }
            finally
            {
                IsUndoingOrRedoing = false;
            }

            // What it leaves selected (once any rebuild it began is done) isn't a new choice to record
            SelectionHistory.Retake();

            OnChanged?.Invoke();
        }
    }
}
#endif
