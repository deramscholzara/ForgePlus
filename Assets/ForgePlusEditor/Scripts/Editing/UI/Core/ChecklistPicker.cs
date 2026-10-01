using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A list of items to check and uncheck (such as the polygons that play a sound), with Select All and Select None,
    // opened over the whole UI below a field (PopupLayer). It stays open while items are checked, until clicking
    // outside it (or pressing escape) closes it.
    public static class ChecklistPicker
    {
        private const float MinimumWidth = 240f;
        private const float MaximumHeight = 400f;

        public static void Show(VisualElement anchor, IList<string> labels, Func<int, bool> isChecked, Action<int, bool> setChecked)
        {
            var box = PopupLayer.Open(anchor, "fp-checklist-picker", MinimumWidth, MaximumHeight);
            if (box == null)
            {
                return;
            }

            var buttonTemplate = LoadTemplate("Button");
            var toggleTemplate = LoadTemplate("Toggle");
            var toggles = new List<Toggle>();

            void ShowChecks()
            {
                for (var i = 0; i < toggles.Count; i++)
                {
                    toggles[i].SetValueWithoutNotify(isChecked(i));
                }
            }

            void SetAll(bool isSet)
            {
                for (var i = 0; i < toggles.Count; i++)
                {
                    if (isChecked(i) != isSet)
                    {
                        setChecked(i, isSet);
                    }
                }

                ShowChecks();
            }

            var actions = new VisualElement();
            actions.AddToClassList("fp-checklist-picker__actions");
            box.Add(actions);

            foreach (var (text, isSet) in new[] { ("Select All", true), ("Select None", false) })
            {
                var instance = buttonTemplate.Instantiate();
                instance.AddToClassList("fp-checklist-picker__action");

                var button = instance.Q<Button>();
                button.text = text;
                button.clicked += () => SetAll(isSet);

                actions.Add(instance);
            }

            var items = new VisualElement();
            items.AddToClassList("fp-palette__swatches");
            box.Add(items);

            for (var i = 0; i < labels.Count; i++)
            {
                var instance = toggleTemplate.Instantiate();
                instance.AddToClassList("fp-checklist-picker__item");

                var toggle = instance.Q<Toggle>();
                toggle.AddToClassList("fp-toggle--flag");
                toggle.text = labels[i];

                var item = i;
                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    setChecked(item, changeEvent.newValue);
                    ShowChecks();
                });

                toggles.Add(toggle);
                items.Add(instance);
            }

            ShowChecks();
        }

        private static VisualTreeAsset LoadTemplate(string templateName)
        {
            return UnityEngine.Resources.Load<VisualTreeAsset>($"UI/Templates/{templateName}");
        }
    }
}
