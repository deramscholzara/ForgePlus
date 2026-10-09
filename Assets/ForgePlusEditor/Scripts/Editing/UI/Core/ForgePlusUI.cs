using ForgePlus.ApplicationGeneral;
using ForgePlus.CameraNavigation;
using ForgePlus.History;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using ForgePlus.Sound;
using System.Collections.Generic;
using Unity.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The editor's UI: fills the layout's slots with the panels that apply to the current mode and menu state,
    // and shows or hides the whole UI and the menu (ForgePlusInput.Interface)
    [RequireComponent(typeof(UIDocument))]
    public class ForgePlusUI : SingletonMonoBehaviour<ForgePlusUI>
    {
        [SerializeField]
        private EditorCamera editorCamera = null;

        [SerializeField]
        private LocalizationSettings localizationSettings = null;

        private readonly List<PanelSlot> slots = new List<PanelSlot>();

        private VisualElement root;
        private bool isVisible = true;

        private PanelSlot visualizationSlot;
        private PanelSlot inspectorSlot;
        private PanelSlot menuSlot;
        private PanelSlot terminalSlot;
        private PanelSlot toolModesSlot;
        private PanelSlot paletteSlot;

        private bool isBuilt;

        // The open mode's string table, and the mode whose panels were last asked for (while its table loads)
        private string modeTable;
        private ModeManager.PrimaryModes shownMode = ModeManager.PrimaryModes.None;

        public EditorViewModel Editor { get; private set; }

        public WorldLabels WorldLabels { get; private set; }

        public SoundVisualization SoundVisualization { get; private set; }

        public SettingsViewModel Settings { get; private set; }

        public TerminalsViewModel Terminals { get; private set; }

        public ErrorsViewModel Errors { get; private set; }

        public EditorCamera EditorCamera
        {
            get
            {
                return editorCamera;
            }
        }

        // Whether a text or number field has focus (so hotkeys are typed instead)
        public bool IsEditingText
        {
            get
            {
                return (root?.focusController?.focusedElement as VisualElement).IsInTextInputField();
            }
        }

        // In Terminals mode, the preview takes the place of the menu (or the Errors panel)
        public void ShowTerminalPreview()
        {
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Terminals)
            {
                Editor.MenuOpen = false;
                Editor.ErrorsOpen = false;
            }
        }

        public void ShowTerminal(int terminalIndex)
        {
            Terminals.TerminalIndex = terminalIndex;
            ShowTerminalPreview();
        }

        // Whether a screen position (pixels, from the bottom left) is over part of the UI that takes the pointer
        public bool IsPointerOverUI(Vector2 screenPosition)
        {
            var panel = root?.panel;
            if (panel == null || !isVisible)
            {
                return false;
            }

            var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));

            return panel.Pick(panelPosition) != null;
        }

        // Built once the text it shows everywhere (the Common and Menu string tables) is loaded
        private async void Start()
        {
            await Strings.InitializeAsync(localizationSettings);

            if (!this)
            {
                // Destroyed while the strings loaded, so exit
                return;
            }

            root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            WorldLabels = new WorldLabels(root);
            SoundVisualization = new SoundVisualization(root);
            // Kept alive by its callbacks on the root
            new TooltipLayer(root);

            // The level's sounds are heard at the camera (where its AudioListener is)
            var soundPlayback = editorCamera.gameObject.AddComponent<LevelSoundPlayback>();
            soundPlayback.IsListening = SettingsManager.Instance.PlayLevelAudioEnabled;

            gameObject.AddComponent<LevelHistory>();

            Editor = new EditorViewModel();
            Settings = new SettingsViewModel();
            Terminals = new TerminalsViewModel();
            Terminals.OnTerminalChanged += SelectionManager.Instance.ShowTerminalSides;
            Errors = new ErrorsViewModel();
            Terminals.OnTerminalEdited += Errors.RequestRefresh;
            Errors.OnFixApplied += Terminals.ReloadTerminal;

            var levelName = root.Q<Label>("level-name");
            levelName.Bind("text", Editor, nameof(EditorViewModel.LevelName));
            levelName.BindShown(Editor, nameof(EditorViewModel.IsLevelOpen));

            CreateSlot("header-slot").Show<HeaderPanel>();
            CreateSlot("manipulation-slot").Show<ManipulationPanel>();
            CreateSlot("errors-toggle-slot").Show<ErrorsTogglePanel>();
            CreateSlot("history-slot").Show<HistoryPanel>();
            CreateSlot("view-options-slot").Show<ViewOptionsPanel>();
            root.Q("mode-settings").AddToClassList("fp-mode-settings--loaded");

            visualizationSlot = CreateSlot("visualization-slot");
            inspectorSlot = CreateSlot("inspector-slot");
            menuSlot = CreateSlot("menu-slot");
            terminalSlot = CreateSlot("terminal-slot");
            toolModesSlot = CreateSlot("tool-modes-slot");
            paletteSlot = CreateSlot("palette-slot");

            ShowMenu(Editor.MenuOpen);
            if (Editor.MenuOpen)
            {
                editorCamera.OnInputBlockerChanged(true);
            }

            Editor.OnMenuOpenChanged += OnMenuOpenChanged;
            Editor.OnErrorsOpenChanged += OnMenuOpenChanged;

            ModeManager.Instance.OnPrimaryModeChanged += OnPrimaryModeChanged;

            isBuilt = true;
        }

        private void OnDestroy()
        {
            var modeManager = ModeManager.Instance;
            if (modeManager)
            {
                modeManager.OnPrimaryModeChanged -= OnPrimaryModeChanged;
            }

            foreach (var slot in slots)
            {
                slot.Hide();
            }

            slots.Clear();

            Editor?.Dispose();
            Settings?.Dispose();
            Terminals?.Dispose();
            Errors?.Dispose();
        }

        // After the camera moves (in its Update)
        private void LateUpdate()
        {
            if (!isBuilt)
            {
                return;
            }

            WorldLabels.UpdatePositions();
            SoundVisualization.Update();

            // Once a frame at most, after what may have changed them
            Errors.RefreshIfRequested();
        }

        private void Update()
        {
            if (!isBuilt)
            {
                return;
            }

            if (Hotkeys.WasPressed(ForgePlusInput.Interface.ToggleUI))
            {
                SetVisible(!isVisible);
            }

            // Toggling the menu also shows the UI, if it was hidden
            if (Hotkeys.WasPressed(ForgePlusInput.Interface.ToggleMenu))
            {
                SetVisible(true);
                Editor.MenuOpen = !Editor.MenuOpen;
            }

            // Ctrl+Shift+Z redoes too
            if (Hotkeys.WasPressed(ForgePlusInput.Interface.Redo) || Hotkeys.WasPressed(ForgePlusInput.Interface.Undo) && Hotkeys.IsShiftPressed)
            {
                UndoHistory.Redo();
            }
            else if (Hotkeys.WasPressed(ForgePlusInput.Interface.Undo))
            {
                UndoHistory.Undo();
            }
        }

        private PanelSlot CreateSlot(string slotName)
        {
            var slot = new PanelSlot(root.Q(slotName));
            slots.Add(slot);

            return slot;
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // The menu's, or the Errors panel's
        private void OnMenuOpenChanged()
        {
            ShowMenu(Editor.MenuOpen);

            // The camera can't be navigated while the menu (or the Errors panel) is open
            editorCamera.OnInputBlockerChanged(Editor.MenuOpen || Editor.ErrorsOpen);
        }

        // The Errors panel takes the menu's place
        private void ShowMenu(bool isOpen)
        {
            if (isOpen)
            {
                menuSlot.Show<MenuPanel>();
            }
            else if (Editor.ErrorsOpen)
            {
                menuSlot.Show<ErrorsPanel>();
            }
            else
            {
                menuSlot.Hide();
            }

            ShowTerminalSlot();
        }

        // The page being read takes the menu's place, so it's hidden while the menu (or the Errors panel) is open
        private void ShowTerminalSlot()
        {
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Terminals && !Editor.MenuOpen && !Editor.ErrorsOpen)
            {
                terminalSlot.Show<TerminalPanel>();
            }
            else
            {
                terminalSlot.Hide();
            }
        }

        // Each mode shows only the panels that apply to it (None, while no level is open, shows none of them)
        private void OnPrimaryModeChanged(ModeManager.PrimaryModes primaryMode)
        {
            // Terminals mode opens on its preview (switching to it as a level is opened isn't entering it)
            var isEnteringTerminals = primaryMode == ModeManager.PrimaryModes.Terminals &&
                                      shownMode != ModeManager.PrimaryModes.Terminals &&
                                      shownMode != ModeManager.PrimaryModes.None;

            if (isEnteringTerminals && Editor.MenuOpen)
            {
                Editor.MenuOpen = false;
            }

            ShowModeWhenItsStringsLoad(primaryMode);
        }

        // A mode's panels are shown once its string table is loaded (as it's entered), and the last mode's is released
        private async void ShowModeWhenItsStringsLoad(ModeManager.PrimaryModes primaryMode)
        {
            shownMode = primaryMode;

            var table = Strings.TableFor(primaryMode);
            if (table != modeTable)
            {
                Strings.ReleaseTable(modeTable);
                modeTable = table;
            }

            if (table != null && !Strings.IsLoaded(table))
            {
                await Strings.LoadTableAsync(table);

                if (!this || shownMode != primaryMode)
                {
                    // Destroyed, or another mode was entered, while its strings loaded, so exit
                    return;
                }

                // Inspectors made while they loaded (such as the level's, as Level mode selects it) show them now
                InspectorPanel.Instance.RefreshAllInspectors();
            }

            ShowModePanels(primaryMode);
        }

        private void ShowModePanels(ModeManager.PrimaryModes primaryMode)
        {
            // Terminals aren't selected, so their mode shows the terminal and group being previewed in place of the
            // inspectors; annotations are listed, and the map file (not in the level) has its own inspector
            switch (primaryMode)
            {
                case ModeManager.PrimaryModes.Terminals:
                    inspectorSlot.Show<TerminalDetailsPanel>();
                    break;
                case ModeManager.PrimaryModes.Map:
                    inspectorSlot.Show<MapPanel>();
                    break;
                case ModeManager.PrimaryModes.Annotations:
                    inspectorSlot.Show<AnnotationsPanel>();
                    break;
                default:
                    inspectorSlot.Show<InspectorColumnPanel>();
                    break;
            }

            ShowTerminalSlot();

            switch (primaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    visualizationSlot.Show(new LayoutPanel("UI/Panels/VisualizationGeometry"));
                    break;
                case ModeManager.PrimaryModes.Objects:
                    visualizationSlot.Show(new LayoutPanel("UI/Panels/VisualizationObjects"));
                    break;
                default:
                    visualizationSlot.Hide();
                    break;
            }

            // Reloaded for each mode, since each has its own tool modes
            if (primaryMode != ModeManager.PrimaryModes.None && ModeManager.Instance.AvailableSecondaryModes.Count > 1)
            {
                toolModesSlot.Show(new ToolModesPanel());
            }
            else
            {
                toolModesSlot.Hide();
            }

            switch (primaryMode)
            {
                case ModeManager.PrimaryModes.Textures:
                    paletteSlot.Show<TexturePalettePanel>();
                    break;
                case ModeManager.PrimaryModes.Lights:
                    paletteSlot.Show<LightPalettePanel>();
                    break;
                case ModeManager.PrimaryModes.Media:
                    paletteSlot.Show<MediaPalettePanel>();
                    break;
                case ModeManager.PrimaryModes.Sounds:
                    paletteSlot.Show<SoundPalettePanel>();
                    break;
                case ModeManager.PrimaryModes.Heights:
                    paletteSlot.Show<HeightPalettePanel>();
                    break;
                case ModeManager.PrimaryModes.Terminals:
                    paletteSlot.Show<TerminalStylesPanel>();
                    break;
                case ModeManager.PrimaryModes.Annotations:
                    paletteSlot.Show<PolygonPalettePanel>();
                    break;
                case ModeManager.PrimaryModes.Platforms:
                    paletteSlot.Show<PlatformPalettePanel>();
                    break;
                default:
                    paletteSlot.Hide();
                    break;
            }
        }
    }
}
