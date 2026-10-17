using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public class HeaderPanel : UIPanel
    {
        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/Header";
            }
        }

        protected override void OnLoaded()
        {
            var editor = ForgePlusUI.Instance.Editor;

            Root.Find<Toggle>("menu").BindValue(editor, nameof(EditorViewModel.MenuOpen));
            Root.Find<RadioButtonGroup>("primary-modes").BindValue(editor, nameof(EditorViewModel.PrimaryModeIndex));

            // Either mouse wheel scrolls the modes sideways (where they don't fit), before the scroll area's own handling
            var modesScroll = Root.Find<ScrollView>("modes-scroll");
            modesScroll.RegisterCallback<WheelEvent>(wheelEvent => ScrollModes(modesScroll, wheelEvent), TrickleDown.TrickleDown);

            // Checked while one of its modes is the mode, and pressing it opens their menu (rather than toggling it)
            var otherModes = Root.Find<Toggle>("other-modes");
            otherModes.Bind("value", editor, nameof(EditorViewModel.IsOtherModeSelected));
            otherModes.OpensOnPress(() => ShowOtherModesMenu(otherModes), replacesOwnHandling: true);

            // The triangle a dropdown shows on its right (as More Display Options' does)
            var arrow = new VisualElement { pickingMode = PickingMode.Ignore };
            arrow.AddToClassList(BasePopupField<string, string>.arrowUssClassName);
            arrow.AddToClassList("fp-header__other-arrow");
            otherModes.Add(arrow);

            var save = Root.Find<Button>("save");
            save.BindEnabled(editor, nameof(EditorViewModel.IsLevelOpen));
            save.clicked += OnSave;

            var saveMerged = Root.Find<Button>("save-merged");
            saveMerged.BindEnabled(editor, nameof(EditorViewModel.IsLevelOpen));
            saveMerged.clicked += OnSaveMerged;

            Root.Find<Button>("quit").clicked += OnQuit;
        }

        // By whichever of the wheel's directions moved more, as far as a scroll area's wheel scrolls it
        private static void ScrollModes(ScrollView modesScroll, WheelEvent wheelEvent)
        {
            var delta = Mathf.Abs(wheelEvent.delta.x) > Mathf.Abs(wheelEvent.delta.y) ? wheelEvent.delta.x : wheelEvent.delta.y;
            var offset = modesScroll.scrollOffset;
            offset.x += delta * modesScroll.mouseWheelScrollSize;
            offset.y = 0f;
            modesScroll.scrollOffset = offset;

            wheelEvent.StopPropagation();
        }

        // Each of the Other modes (as named on their own tabs before), the current one checked
        private static void ShowOtherModesMenu(Toggle anchor)
        {
            var menu = new GenericDropdownMenu();

            foreach (var mode in EditorViewModel.OtherModes)
            {
                var selected = mode;
                menu.AddItem(Strings.Get(Strings.Common, $"Header.{mode}.Control.Text"),
                             ModeManager.Instance.PrimaryMode == mode,
                             () => ModeManager.Instance.PrimaryMode = selected);
            }

            menu.DropDown(anchor.worldBound, anchor, DropdownMenuSizeMode.Auto);
        }

        private static void OnSave()
        {
            MapsLoading.Instance.Save();
        }

        private static void OnSaveMerged()
        {
            MapsLoading.Instance.SaveMerged();
        }

        private static void OnQuit()
        {
            // TODO: Check if save is necessary
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
