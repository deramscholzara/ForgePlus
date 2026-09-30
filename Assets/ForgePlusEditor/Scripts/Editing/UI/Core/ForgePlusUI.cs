using ForgePlus.ApplicationGeneral;
using ForgePlus.CameraNavigation;
using ForgePlus.LevelManipulation;
using System.Collections.Generic;
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

        private readonly List<PanelSlot> slots = new List<PanelSlot>();

        private VisualElement root;
        private bool isVisible = true;

        private PanelSlot visualizationSlot;
        private PanelSlot inspectorSlot;
        private PanelSlot menuSlot;
        private PanelSlot terminalSlot;
        private PanelSlot toolModesSlot;
        private PanelSlot paletteSlot;

        public EditorViewModel Editor { get; private set; }

        public SettingsViewModel Settings { get; private set; }

        public TerminalsViewModel Terminals { get; private set; }

        public EditorCamera EditorCamera
        {
            get
            {
                return editorCamera;
            }
        }

        // Whether a text field has focus (so hotkeys are typed instead)
        public bool IsEditingText
        {
            get
            {
                var focusedElement = root?.focusController?.focusedElement as VisualElement;

                for (var element = focusedElement; element != null; element = element.parent)
                {
                    if (element.ClassListContains(TextField.ussClassName))
                    {
                        return true;
                    }
                }

                return false;
            }
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

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            Editor = new EditorViewModel();
            Settings = new SettingsViewModel();
            Terminals = new TerminalsViewModel();

            var levelName = root.Q<Label>("level-name");
            levelName.Bind("text", Editor, nameof(EditorViewModel.LevelName));
            levelName.BindShown(Editor, nameof(EditorViewModel.IsLevelOpen));

            CreateSlot("header-slot").Show<HeaderPanel>();
            CreateSlot("manipulation-slot").Show<ManipulationPanel>();
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

            ModeManager.Instance.OnPrimaryModeChanged += OnPrimaryModeChanged;
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
        }

        private void Update()
        {
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

        private void OnMenuOpenChanged()
        {
            ShowMenu(Editor.MenuOpen);

            // The camera can't be navigated while the menu is open
            editorCamera.OnInputBlockerChanged(Editor.MenuOpen);
        }

        private void ShowMenu(bool isOpen)
        {
            if (isOpen)
            {
                menuSlot.Show<MenuPanel>();
            }
            else
            {
                menuSlot.Hide();
            }

            ShowTerminal();
        }

        // The page being read takes the menu's place, so it's hidden while the menu is open
        private void ShowTerminal()
        {
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Terminals && !Editor.MenuOpen)
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
            // Terminals aren't selected, so their mode shows the terminal and group being previewed in place of the
            // inspectors
            if (primaryMode == ModeManager.PrimaryModes.Terminals)
            {
                inspectorSlot.Show<TerminalDetailsPanel>();
            }
            else
            {
                inspectorSlot.Show<InspectorColumnPanel>();
            }

            ShowTerminal();

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
                case ModeManager.PrimaryModes.Terminals:
                    paletteSlot.Show<TerminalStylesPanel>();
                    break;
                default:
                    paletteSlot.Hide();
                    break;
            }
        }
    }
}
