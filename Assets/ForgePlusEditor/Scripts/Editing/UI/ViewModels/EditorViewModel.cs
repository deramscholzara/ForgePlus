using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using System;
using Unity.Properties;

namespace ForgePlus.UI
{
    // The editor's state that the UI shows and changes: modes, the open level, the selection, axis locks, and the menu
    public class EditorViewModel : BindableObject, IDisposable
    {
        public event Action OnMenuOpenChanged;
        public event Action OnMenuTabChanged;

        private bool menuOpen = true;
        private int menuTabIndex = 0;
        private string levelName = null;

        public EditorViewModel()
        {
            ModeManager.Instance.OnPrimaryModeChanged += OnPrimaryModeChanged;
            ModeManager.Instance.OnSecondaryModeChanged += OnSecondaryModeChanged;
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += OnLevelClosed;
            SelectionManager.Instance.OnSelectionChanged += OnSelectionChanged;
            AxisLocks.Instance.OnChanged += OnAxisLocksChanged;
        }

        // Primary modes in their order in the header (Geometry is 0)
        [CreateProperty]
        public int PrimaryModeIndex
        {
            get
            {
                return (int)ModeManager.Instance.PrimaryMode - 1;
            }
            set
            {
                if (value >= 0)
                {
                    ModeManager.Instance.PrimaryMode = (ModeManager.PrimaryModes)(value + 1);
                }
            }
        }

        [CreateProperty]
        public ModeManager.SecondaryModes SecondaryMode
        {
            get
            {
                return ModeManager.Instance.SecondaryMode;
            }
            set
            {
                ModeManager.Instance.SecondaryMode = value;
            }
        }

        [CreateProperty]
        public bool IsLevelOpen
        {
            get
            {
                return !string.IsNullOrEmpty(levelName);
            }
        }

        [CreateProperty]
        public string LevelName
        {
            get
            {
                return levelName ?? string.Empty;
            }
        }

        [CreateProperty]
        public bool HasSelection
        {
            get
            {
                return SelectionManager.Instance.SelectedObject != null;
            }
        }

        [CreateProperty]
        public bool XLocked
        {
            get
            {
                return AxisLocks.Instance.XLocked;
            }
            set
            {
                AxisLocks.Instance.XLocked = value;
            }
        }

        [CreateProperty]
        public bool YLocked
        {
            get
            {
                return AxisLocks.Instance.YLocked;
            }
            set
            {
                AxisLocks.Instance.YLocked = value;
            }
        }

        [CreateProperty]
        public bool SnapToGrid
        {
            get
            {
                return AxisLocks.Instance.SnapToGridEnabled;
            }
            set
            {
                AxisLocks.Instance.SnapToGridEnabled = value;
            }
        }

        [CreateProperty]
        public bool MenuOpen
        {
            get
            {
                return menuOpen;
            }
            set
            {
                if (menuOpen != value)
                {
                    menuOpen = value;
                    Notify(nameof(MenuOpen));
                    OnMenuOpenChanged?.Invoke();
                }
            }
        }

        // Files, Inputs, Settings
        [CreateProperty]
        public int MenuTabIndex
        {
            get
            {
                return menuTabIndex;
            }
            set
            {
                if (menuTabIndex != value && value >= 0)
                {
                    menuTabIndex = value;
                    Notify(nameof(MenuTabIndex));
                    OnMenuTabChanged?.Invoke();
                }
            }
        }

        public void Dispose()
        {
            var modeManager = ModeManager.Instance;
            if (modeManager)
            {
                modeManager.OnPrimaryModeChanged -= OnPrimaryModeChanged;
                modeManager.OnSecondaryModeChanged -= OnSecondaryModeChanged;
            }

            MapsLoading.Instance.OnLevelOpened -= OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed -= OnLevelClosed;

            var selectionManager = SelectionManager.Instance;
            if (selectionManager)
            {
                selectionManager.OnSelectionChanged -= OnSelectionChanged;
            }

            var axisLocks = AxisLocks.Instance;
            if (axisLocks)
            {
                axisLocks.OnChanged -= OnAxisLocksChanged;
            }
        }

        private void OnPrimaryModeChanged(ModeManager.PrimaryModes primaryMode)
        {
            Notify(nameof(PrimaryModeIndex));
        }

        private void OnSecondaryModeChanged(ModeManager.SecondaryModes secondaryMode)
        {
            Notify(nameof(SecondaryMode));
        }

        private void OnLevelOpened(string openedLevelName)
        {
            levelName = openedLevelName;
            Notify(nameof(LevelName));
            Notify(nameof(IsLevelOpen));
        }

        private void OnLevelClosed()
        {
            OnLevelOpened(null);
        }

        private void OnSelectionChanged()
        {
            Notify(nameof(HasSelection));
        }

        private void OnAxisLocksChanged()
        {
            Notify(nameof(XLocked));
            Notify(nameof(YLocked));
            Notify(nameof(SnapToGrid));
        }
    }
}
