using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using RuntimeCore.Entities;
using System;
using Unity.Properties;

namespace ForgePlus.UI
{
    // The editor's state that the UI shows and changes: modes, the open level, the selection, axis locks, and the menu
    // and the Errors panel (which take the same place, so only one is open at a time)
    public class EditorViewModel : BindableObject, IDisposable
    {
        public event Action OnMenuOpenChanged;
        public event Action OnMenuTabChanged;
        public event Action OnErrorsOpenChanged;

        private bool menuOpen = true;
        private bool errorsOpen = false;
        private int menuTabIndex = 0;
        private string levelName = null;

        public EditorViewModel()
        {
            ModeManager.Instance.OnPrimaryModeChanged += OnPrimaryModeChanged;
            ModeManager.Instance.OnSecondaryModeChanged += OnSecondaryModeChanged;
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += OnLevelClosed;
            MapsLoading.Instance.OnLevelNamesChanged += OnLevelNamesChanged;
            SelectionManager.Instance.OnSelectionChanged += OnSelectionChanged;
            AxisLocks.Instance.OnChanged += OnAxisLocksChanged;
            UndoHistory.OnChanged += OnUndoHistoryChanged;
            ForgePlusUI.Instance.EditorCamera.OnOrthographicChanged += OnOrthographicChanged;
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
        public bool CanUndo
        {
            get
            {
                return UndoHistory.CanUndo;
            }
        }

        [CreateProperty]
        public bool CanRedo
        {
            get
            {
                return UndoHistory.CanRedo;
            }
        }

        [CreateProperty]
        public string UndoTooltip
        {
            get
            {
                var description = UndoHistory.UndoDescription;

                return description != null ?
                       Strings.Get(Strings.Common, "History.Undo.Tooltip", description) :
                       Strings.Get(Strings.Common, "History.Undo.Tooltip.Nothing");
            }
        }

        [CreateProperty]
        public string RedoTooltip
        {
            get
            {
                var description = UndoHistory.RedoDescription;

                return description != null ?
                       Strings.Get(Strings.Common, "History.Redo.Tooltip", description) :
                       Strings.Get(Strings.Common, "History.Redo.Tooltip.Nothing");
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

        // The camera's top-down view (on for this session only)
        [CreateProperty]
        public bool Orthographic
        {
            get
            {
                return ForgePlusUI.Instance.EditorCamera.IsOrthographic;
            }
            set
            {
                ForgePlusUI.Instance.EditorCamera.IsOrthographic = value;
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
                    if (value)
                    {
                        ErrorsOpen = false;
                    }

                    menuOpen = value;
                    Notify(nameof(MenuOpen));
                    OnMenuOpenChanged?.Invoke();
                }
            }
        }

        [CreateProperty]
        public bool ErrorsOpen
        {
            get
            {
                return errorsOpen;
            }
            set
            {
                if (errorsOpen != value)
                {
                    if (value)
                    {
                        MenuOpen = false;
                    }

                    errorsOpen = value;
                    Notify(nameof(ErrorsOpen));
                    OnErrorsOpenChanged?.Invoke();
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
            MapsLoading.Instance.OnLevelNamesChanged -= OnLevelNamesChanged;

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

            UndoHistory.OnChanged -= OnUndoHistoryChanged;

            var ui = ForgePlusUI.Instance;
            if (ui && ui.EditorCamera)
            {
                ui.EditorCamera.OnOrthographicChanged -= OnOrthographicChanged;
            }
        }

        private void OnOrthographicChanged()
        {
            Notify(nameof(Orthographic));
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

        // The open level renamed
        private void OnLevelNamesChanged()
        {
            OnLevelOpened(LevelEntity_Level.Instance ? LevelEntity_Level.Instance.Level.GetLevelName() : null);
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

        private void OnUndoHistoryChanged()
        {
            Notify(nameof(CanUndo));
            Notify(nameof(CanRedo));
            Notify(nameof(UndoTooltip));
            Notify(nameof(RedoTooltip));
        }
    }
}
