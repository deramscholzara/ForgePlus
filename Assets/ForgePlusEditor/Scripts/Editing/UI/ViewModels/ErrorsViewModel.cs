using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;

namespace ForgePlus.UI
{
    // The open level's errors and warnings (LevelError), found again once after whatever could change them (errors first,
    // then warnings), and the Errors panel's filter of them. Warnings are shown unless Show Warnings is off (for the
    // session).
    public class ErrorsViewModel : BindableObject, IDisposable
    {
        // A fix that leads to another problem (which its own fix resolves) is followed by that one's, up to this many
        private const int MaximumFixes = 1000;

        // The Errors panel's tabs, in order
        private enum Filter
        {
            All,
            AutoFixable,
            Standard,
        }

        public event Action OnErrorsChanged;

        // After a fix changes the level
        public event Action OnFixApplied;

        private readonly List<LevelError> errors = new List<LevelError>();
        private bool isRefreshRequested = true;
        private int filterIndex = 0;
        private bool showWarnings = true;

        public ErrorsViewModel()
        {
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += OnLevelClosed;
        }

        // Errors (not warnings), which make the errors toggle flash
        [CreateProperty]
        public bool HasErrors
        {
            get
            {
                return errors.Any(error => !error.IsWarning);
            }
        }

        // Errors or warnings, either of which the Errors panel can be opened for
        [CreateProperty]
        public bool HasProblems
        {
            get
            {
                return errors.Count > 0;
            }
        }

        [CreateProperty]
        public bool ShowWarnings
        {
            get
            {
                return showWarnings;
            }
            set
            {
                if (showWarnings != value)
                {
                    showWarnings = value;
                    Notify(nameof(ShowWarnings));
                    NotifyAll();
                }
            }
        }

        // The index of the Filter
        [CreateProperty]
        public int FilterIndex
        {
            get
            {
                return filterIndex;
            }
            set
            {
                if (filterIndex != value && value >= 0)
                {
                    filterIndex = value;
                    Notify(nameof(FilterIndex));
                    NotifyAll();
                }
            }
        }

        public IReadOnlyList<LevelError> FilteredErrors
        {
            get
            {
                var shown = errors.Where(error => showWarnings || !error.IsWarning);

                switch ((Filter) filterIndex)
                {
                    case Filter.AutoFixable:
                        return shown.Where(error => error.IsAutoFixable).ToList();
                    case Filter.Standard:
                        return shown.Where(error => !error.IsAutoFixable).ToList();
                    default:
                        return shown.ToList();
                }
            }
        }

        // Where auto-fixable errors are listed
        [CreateProperty]
        public bool IsFixAllShown
        {
            get
            {
                return filterIndex != (int) Filter.Standard;
            }
        }

        [CreateProperty]
        public bool CanFixAll
        {
            get
            {
                return errors.Any(error => error.IsAutoFixable && (showWarnings || !error.IsWarning));
            }
        }

        // Once, as the frame ends
        public void RequestRefresh()
        {
            isRefreshRequested = true;
        }

        public void RefreshIfRequested()
        {
            if (isRefreshRequested)
            {
                isRefreshRequested = false;
                Refresh();
            }
        }

        public void Fix(LevelError error)
        {
            if (error.IsAutoFixable)
            {
                error.Fix();
                OnFixApplied?.Invoke();
            }

            RequestRefresh();
        }

        // Each fix changes what the others refer to (such as the groups' numbers), so the errors are found again
        // before each
        public void FixAll()
        {
            string lastFixed = null;
            for (var fixCount = 0; fixCount < MaximumFixes; fixCount++)
            {
                // Done when none is left, or when one's fix didn't resolve it (hidden warnings aren't fixed)
                var error = FindErrors().FirstOrDefault(found => found.IsAutoFixable && (showWarnings || !found.IsWarning));
                if (error == null || error.Description == lastFixed)
                {
                    break;
                }

                error.Fix();
                lastFixed = error.Description;
            }

            if (lastFixed != null)
            {
                OnFixApplied?.Invoke();
            }

            Refresh();
        }

        public void Dispose()
        {
            MapsLoading.Instance.OnLevelOpened -= OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed -= OnLevelClosed;
        }

        private void Refresh()
        {
            errors.Clear();
            errors.AddRange(FindErrors());

            NotifyAll();
        }

        // Errors before warnings (each kept in the order they're found)
        private static List<LevelError> FindErrors()
        {
            var level = LevelEntity_Level.Instance;
            if (!level || LevelEditing.IsRebuildPending)
            {
                return new List<LevelError>();
            }

            return TerminalErrors.Find(level.Level).Concat(GeometryErrors.Find(level))
                                 .OrderBy(error => error.IsWarning)
                                 .ToList();
        }

        private void NotifyAll()
        {
            Notify(nameof(HasErrors));
            Notify(nameof(HasProblems));
            Notify(nameof(IsFixAllShown));
            Notify(nameof(CanFixAll));
            OnErrorsChanged?.Invoke();
        }

        private void OnLevelOpened(string levelName)
        {
            RequestRefresh();
        }

        private void OnLevelClosed()
        {
            RequestRefresh();
        }
    }
}
