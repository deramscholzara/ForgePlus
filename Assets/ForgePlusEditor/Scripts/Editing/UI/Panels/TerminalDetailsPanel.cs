using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // What's stored for the terminal chosen from the level's terminals, and for the group chosen from its groups
    // (the group being previewed, which is edited here), in the inspector column
    public class TerminalDetailsPanel : UIPanel
    {
        // How far a group's number is dragged before it's being moved (rather than clicked)
        private const float DragThreshold = 6f;

        // Milliseconds after a style is applied before its text is selected again
        private const long SelectionRestoreDelay = 50;

        private TerminalsViewModel terminals;
        private RadioButtonGroup terminalList;
        private RadioButtonGroup groupList;
        private VisualElement insertionMarker;
        private TextField textField;

        // The text field's selection when it was last focused (a style chosen from the styles panel takes its focus)
        private int textCursorIndex;
        private int textSelectIndex;

        // The group whose number is pressed (or -1), and whether it's being dragged
        private int draggedGroupIndex = -1;
        private Vector2 dragStartPosition;
        private bool isDraggingGroup;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/TerminalDetails";
            }
        }

        protected override void OnLoaded()
        {
            terminals = ForgePlusUI.Instance.Terminals;
            terminalList = Root.Q<RadioButtonGroup>("terminals");
            groupList = Root.Q<RadioButtonGroup>("groups");
            insertionMarker = Root.Q("group-insertion-marker");
            textField = Root.Find<TextField>("Text");

            // The text's line breaks are its own (so return types one)
            textField.multiline = true;
            textField.verticalScrollerVisibility = ScrollerVisibility.Auto;
            textField.LimitToMacRomanText(-1);

            Root.BindInspectorFields(terminals);

            // What doesn't apply to the group is grayed out, rather than hidden, so the panel's layout stays put
            Root.Q("Permutation").BindEnabled(terminals, nameof(TerminalsViewModel.UsesPermutation));
            Root.Q("Text").BindEnabled(terminals, nameof(TerminalsViewModel.HasText));
            Root.Q("details").BindEnabled(terminals, nameof(TerminalsViewModel.HasTerminals));

            var removeGroup = Root.Find<Button>("remove-group");
            removeGroup.BindEnabled(terminals, nameof(TerminalsViewModel.CanRemoveGroup));
            removeGroup.clicked += () => EditAndPreview(terminals.RemoveGroup);
            Root.Find<Button>("add-group").clicked += () => EditAndPreview(terminals.AddGroup);

            AddTerminalToggles();
            AddGroupToggles();
            terminalList.BindValue(terminals, nameof(TerminalsViewModel.TerminalIndex));
            groupList.BindValue(terminals, nameof(TerminalsViewModel.GroupIndex));

            terminals.OnTerminalsChanged += AddTerminalToggles;
            terminals.OnTerminalChanged += AddGroupToggles;
            terminals.OnPageChanged += ShowGroupKinds;
            terminals.OnStyleChosen += ApplyStyle;

            // Choosing a terminal or group shows its preview (in the menu's place, if it's open)
            terminalList.RegisterCallback<ClickEvent>(OnListClicked);
            groupList.RegisterCallback<ClickEvent>(OnListClicked);

            // Seen on its way to the pressed toggle, which takes it (and the pointer) where it's sent
            groupList.RegisterCallback<PointerDownEvent>(OnGroupPointerDown, TrickleDown.TrickleDown);
            insertionMarker.style.display = DisplayStyle.None;

            foreach (var element in new VisualElement[] { textField, textField.Q<TextElement>() })
            {
                element.RegisterCallback<PointerUpEvent>(pointerUpEvent => RememberTextSelection());
                element.RegisterCallback<KeyUpEvent>(keyUpEvent => RememberTextSelection());
            }

            AddStyleButtons();
        }

        protected override void OnUnloading()
        {
            terminals.OnTerminalsChanged -= AddTerminalToggles;
            terminals.OnTerminalChanged -= AddGroupToggles;
            terminals.OnPageChanged -= ShowGroupKinds;
            terminals.OnStyleChosen -= ApplyStyle;
        }

        // A button for each style, which applies it as choosing it from the styles panel does
        private void AddStyleButtons()
        {
            var faces = Root.Q("style-faces");
            var colors = Root.Q("style-colors");

            foreach (var style in TerminalText.Styles)
            {
                var isColor = style.Kind == TerminalSource.StyleKind.Color;

                // Not focusable, so the text keeps its selection
                var button = new Button(() => terminals.ChooseStyle(style.Kind, style.Value)) { text = isColor ? style.Key : style.Name, focusable = false };
                button.AddToClassList("fp-button");
                button.AddToClassList("fp-terminal-style-button");

                if (isColor)
                {
                    button.AddToClassList("fp-terminal-style-button--color");
                    TerminalText.ApplyStyle(button, style.Face, style.ColorIndex, 1f);
                    button.style.borderBottomWidth = 0;
                    colors.Add(button);
                }
                else
                {
                    faces.Add(button);
                }
            }

            faces.BindEnabled(terminals, nameof(TerminalsViewModel.HasText));
            colors.BindEnabled(terminals, nameof(TerminalsViewModel.HasText));
        }

        private static void OnListClicked(ClickEvent clickEvent)
        {
            ForgePlusUI.Instance.ShowTerminalPreview();
        }

        // A group added, removed or moved is shown
        private static void EditAndPreview(Action edit)
        {
            edit();
            ForgePlusUI.Instance.ShowTerminalPreview();
        }

        // A toggle for each terminal, numbered as control panels refer to them
        private void AddTerminalToggles()
        {
            AddToggles(terminalList, terminals.TerminalCount);
            terminalList.SetValueWithoutNotify(terminals.TerminalIndex);
        }

        // A toggle for each of the terminal's groups, numbered by their order in it, which can be dragged elsewhere in it
        private void AddGroupToggles()
        {
            AddToggles(groupList, terminals.GroupCount);
            groupList.SetValueWithoutNotify(terminals.GroupIndex);

            // A pressed toggle takes the pointer, and is sent its events alone (not its list), until it's released
            groupList.Query<RadioButton>().ForEach(toggle =>
            {
                toggle.RegisterCallback<PointerMoveEvent>(OnGroupPointerMove);
                toggle.RegisterCallback<PointerUpEvent>(OnGroupPointerUp);
                toggle.RegisterCallback<PointerCancelEvent>(OnGroupPointerCancel);

                var kindBar = new VisualElement { name = "kind", pickingMode = PickingMode.Ignore };
                kindBar.AddToClassList("fp-terminal-group-kind");
                toggle.Add(kindBar);
            });

            ShowGroupKinds();
        }

        // Each group's toggle has a bar colored by what the group is for (which changes with its type)
        private void ShowGroupKinds()
        {
            var terminal = terminals.Terminal;

            for (var index = 0; index < groupList.contentContainer.childCount; index++)
            {
                var kindBar = groupList.contentContainer[index].Q("kind");
                if (kindBar == null)
                {
                    continue;
                }

                foreach (TerminalRules.GroupKind kind in Enum.GetValues(typeof(TerminalRules.GroupKind)))
                {
                    var isKind = terminal != null && index < terminal.groupings.Count && TerminalRules.KindOf(terminal.groupings[index].type) == kind;
                    kindBar.EnableInClassList(KindClass(kind), isKind);
                }
            }
        }

        private static string KindClass(TerminalRules.GroupKind kind)
        {
            switch (kind)
            {
                case TerminalRules.GroupKind.SectionStart:
                    return "fp-terminal-group-kind--section-start";
                case TerminalRules.GroupKind.Log:
                    return "fp-terminal-group-kind--log";
                case TerminalRules.GroupKind.Action:
                    return "fp-terminal-group-kind--action";
                case TerminalRules.GroupKind.Ending:
                    return "fp-terminal-group-kind--ending";
                case TerminalRules.GroupKind.Stranding:
                    return "fp-terminal-group-kind--stranding";
                default:
                    return "fp-terminal-group-kind--body";
            }
        }

        private static void AddToggles(RadioButtonGroup list, int count)
        {
            list.Clear();

            var template = LoadTemplate("RadioToggle");
            for (var i = 0; i < count; i++)
            {
                var instance = template.Instantiate();
                instance.AddToClassList("fp-index-list__item");
                instance.Q<RadioButton>().text = i.ToString();
                list.Add(instance);
            }

            list.IgnoreLayoutPicking();
        }

        // As it's made (with the pointer, or the keys), since pressing a style's button takes the field's focus, and
        // its selection with it
        private void RememberTextSelection()
        {
            textCursorIndex = textField.textSelection.cursorIndex;
            textSelectIndex = textField.textSelection.selectIndex;
        }

        // A style chosen from the styles panel applies to the selected text (or from the cursor on)
        private void ApplyStyle(TerminalSource.StyleKind kind, short value)
        {
            var text = textField.value ?? string.Empty;
            var start = Mathf.Clamp(textSelectIndex, 0, text.Length);
            var end = Mathf.Clamp(textCursorIndex, 0, text.Length);

            textField.value = TerminalSource.ApplyStyle(text, ref start, ref end, kind, value);

            // Back in the text, with the styled text still selected (once focusing it, and the button's click, are done
            // with the selection)
            textSelectIndex = start;
            textCursorIndex = end;
            textField.Focus();
            textField.schedule.Execute(() => textField.textSelection.SelectRange(end, start)).ExecuteLater(SelectionRestoreDelay);
        }

        // Dragging a group's number moves it to another place in the order. The pressed toggle keeps the pointer (as it
        // does for a click), so it always gets its release (and isn't clicked unless it's released over itself).
        private void OnGroupPointerDown(PointerDownEvent pointerDownEvent)
        {
            draggedGroupIndex = pointerDownEvent.button == 0 ? GetGroupItemIndex(pointerDownEvent.target as VisualElement) : -1;
            dragStartPosition = pointerDownEvent.position;
            isDraggingGroup = false;
        }

        private void OnGroupPointerMove(PointerMoveEvent pointerMoveEvent)
        {
            if (draggedGroupIndex < 0)
            {
                return;
            }

            // Released where the list didn't see it
            if ((pointerMoveEvent.pressedButtons & 1) == 0)
            {
                EndGroupDrag();
                return;
            }

            if (!isDraggingGroup)
            {
                if (Vector2.Distance(dragStartPosition, pointerMoveEvent.position) < DragThreshold)
                {
                    return;
                }

                isDraggingGroup = true;
                GetGroupItem(draggedGroupIndex)?.AddToClassList("fp-index-list__item--dragged");
            }

            ShowInsertionMarker(GetInsertionIndex(pointerMoveEvent.position), pointerMoveEvent.position);
        }

        private void OnGroupPointerUp(PointerUpEvent pointerUpEvent)
        {
            if (!isDraggingGroup)
            {
                draggedGroupIndex = -1;
                return;
            }

            var fromIndex = draggedGroupIndex;
            var insertionIndex = GetInsertionIndex(pointerUpEvent.position);

            EndGroupDrag();

            // Where it's inserted, once it's taken from where it was. The toggles are made again for the new order, so
            // that's once the toggle has had its release.
            var toIndex = insertionIndex > fromIndex ? insertionIndex - 1 : insertionIndex;
            if (toIndex != fromIndex)
            {
                groupList.schedule.Execute(() => EditAndPreview(() => terminals.MoveGroup(fromIndex, toIndex)));
            }
        }

        private void OnGroupPointerCancel(PointerCancelEvent pointerCancelEvent)
        {
            EndGroupDrag();
        }

        private void EndGroupDrag()
        {
            GetGroupItem(draggedGroupIndex)?.RemoveFromClassList("fp-index-list__item--dragged");
            insertionMarker.style.display = DisplayStyle.None;
            draggedGroupIndex = -1;
            isDraggingGroup = false;
        }

        // The group toggle the element is in (or -1)
        private int GetGroupItemIndex(VisualElement element)
        {
            var item = element?.GetAncestorWithClass("fp-index-list__item");

            return item != null ? groupList.contentContainer.IndexOf(item) : -1;
        }

        private VisualElement GetGroupItem(int index)
        {
            return index >= 0 && index < groupList.contentContainer.childCount ? groupList.contentContainer[index] : null;
        }

        // Where in the order the pointer is: after each toggle above its row, or before it in its row
        private int GetInsertionIndex(Vector2 position)
        {
            var insertionIndex = 0;

            for (var index = 0; index < groupList.contentContainer.childCount; index++)
            {
                var bounds = groupList.contentContainer[index].worldBound;
                if (position.y > bounds.yMax || (position.y >= bounds.yMin && position.x > bounds.center.x))
                {
                    insertionIndex = index + 1;
                }
            }

            return insertionIndex;
        }

        // A bar at the place, in the pointer's row: after the toggle before it (at the end of a row, or of the list), or
        // else before the toggle there
        private void ShowInsertionMarker(int insertionIndex, Vector2 pointerPosition)
        {
            var count = groupList.contentContainer.childCount;
            if (count == 0)
            {
                return;
            }

            var previousItem = insertionIndex > 0 ? groupList.contentContainer[insertionIndex - 1] : null;
            var isAfterPrevious = insertionIndex >= count ||
                                  (previousItem != null && pointerPosition.y >= previousItem.worldBound.yMin && pointerPosition.y <= previousItem.worldBound.yMax);
            var item = isAfterPrevious ? previousItem : groupList.contentContainer[insertionIndex];

            // Between the toggles (in the gap around the item's toggle), and inside the list (which clips beyond it)
            var toggleBounds = item.Q<RadioButton>().worldBound;
            var halfWidth = insertionMarker.resolvedStyle.width / 2f;
            var listBounds = groupList.contentContainer.worldBound;
            var x = isAfterPrevious ? (toggleBounds.xMax + item.worldBound.xMax) / 2f : (item.worldBound.xMin + toggleBounds.xMin) / 2f;
            x = Mathf.Clamp(x, listBounds.xMin + halfWidth, Mathf.Min(listBounds.xMax, toggleBounds.xMax + halfWidth) - halfWidth);
            var position = insertionMarker.parent.WorldToLocal(new Vector2(x, toggleBounds.yMin));

            insertionMarker.style.left = position.x - halfWidth;
            insertionMarker.style.top = position.y;
            insertionMarker.style.height = toggleBounds.height;
            insertionMarker.style.display = DisplayStyle.Flex;
        }
    }
}
