using ForgePlus.DataFileIO;
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

            var save = Root.Find<Button>("save");
            save.BindEnabled(editor, nameof(EditorViewModel.IsLevelOpen));
            save.clicked += OnSave;

            var saveMerged = Root.Find<Button>("save-merged");
            saveMerged.BindEnabled(editor, nameof(EditorViewModel.IsLevelOpen));
            saveMerged.clicked += OnSaveMerged;

            Root.Find<Button>("quit").clicked += OnQuit;
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
