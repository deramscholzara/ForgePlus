#if !NO_EDITING
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.History
{
    // The actions that can be undone (newest last) and redone (next to redo last). Recording an action forgets what
    // could be redone, and the oldest actions past the capacity are forgotten.
    [AutoStaticsCleanup]
    public static class UndoHistory
    {
        // How many actions can be undone (the Undo Steps setting, which SettingsManager applies)
        public const int MinimumCapacity = 1;
        public const int MaximumCapacity = 10000;
        public const int DefaultCapacity = 50;

        private static int capacity = DefaultCapacity;

        private static List<IUndoableAction> undoActions = new List<IUndoableAction>();
        private static List<IUndoableAction> redoActions = new List<IUndoableAction>();

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

        // Returns whether any were forgotten
        private static bool Trim()
        {
            var excessUndo = undoActions.Count - capacity;
            if (excessUndo > 0)
            {
                undoActions.RemoveRange(0, excessUndo);
            }

            var excessRedo = undoActions.Count + redoActions.Count - capacity;
            if (excessRedo > 0)
            {
                redoActions.RemoveRange(0, excessRedo);
            }

            return excessUndo > 0 || excessRedo > 0;
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

        // Forgets every action (for when what they changed is gone, such as when a level closes)
        public static void Clear()
        {
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
            }
            finally
            {
                IsUndoingOrRedoing = false;
            }

            OnChanged?.Invoke();
        }
    }
}
#endif
