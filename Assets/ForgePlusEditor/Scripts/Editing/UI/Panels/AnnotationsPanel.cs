using ForgePlus.DataFileIO;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using RuntimeCore.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Every annotation in the level, each as its inspector, in the inspector column. Clicking one selects it and frames
    // the camera on it.
    public class AnnotationsPanel : UIPanel
    {
        private readonly List<KeyValuePair<LevelEntity_Annotation, Inspector_Annotation>> entries = new List<KeyValuePair<LevelEntity_Annotation, Inspector_Annotation>>();

        private ScrollView scrollView;
        private VisualElement noAnnotations;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/Annotations";
            }
        }

        protected override void OnLoaded()
        {
            scrollView = Root.Find<ScrollView>("annotations");
            noAnnotations = Root.Q("no-annotations");

            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += RemoveEntries;
            SelectionManager.Instance.OnSelectionChanged += ShowSelection;
        }

        protected override void OnUnloading()
        {
            MapsLoading.Instance.OnLevelOpened -= OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed -= RemoveEntries;

            var selectionManager = SelectionManager.Instance;
            if (selectionManager)
            {
                selectionManager.OnSelectionChanged -= ShowSelection;
            }

            RemoveEntries();
        }

        private void OnLevelOpened(string levelName)
        {
            RemoveEntries();

            var level = LevelEntity_Level.Instance;
            if (level)
            {
                foreach (var annotation in level.Annotations.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                {
                    var inspector = new Inspector_Annotation(annotation);
                    inspector.Load(scrollView.contentContainer);
                    inspector.Root.AddToClassList("fp-annotation");
                    inspector.Root.RegisterCallback<ClickEvent>(clickEvent => OnEntryClicked(annotation, clickEvent));

                    entries.Add(new KeyValuePair<LevelEntity_Annotation, Inspector_Annotation>(annotation, inspector));
                }
            }

            noAnnotations.style.display = entries.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            ShowSelection();
        }

        private void RemoveEntries()
        {
            foreach (var entry in entries)
            {
                entry.Value.Unload();
            }

            entries.Clear();
        }

        // Clicking in a field of the selected annotation (to edit it) leaves the camera where it is
        private static void OnEntryClicked(LevelEntity_Annotation annotation, ClickEvent clickEvent)
        {
            if ((clickEvent.target as VisualElement).IsInTextInputField() && SelectionManager.Instance.GetIsSelected(annotation))
            {
                return;
            }

            SelectionManager.Instance.SelectObject(annotation, multiSelect: false);
            ForgePlusUI.Instance.EditorCamera.FrameSelected();
        }

        private void ShowSelection()
        {
            foreach (var entry in entries)
            {
                var isSelected = SelectionManager.Instance.GetIsSelected(entry.Key);
                entry.Value.Root.EnableInClassList("fp-annotation--selected", isSelected);

                if (isSelected)
                {
                    scrollView.ScrollTo(entry.Value.Root);
                }
            }
        }
    }
}
