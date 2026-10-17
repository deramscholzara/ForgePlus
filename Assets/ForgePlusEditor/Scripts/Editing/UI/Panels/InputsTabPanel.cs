using ForgePlus.ApplicationGeneral;
using ForgePlus.Localization;
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Each key and mouse button input, by category, with its bindings: clicking one waits for the key or button to bind
    // to it (InputBindings). An input whose binding conflicts with another's (InputConflicts) is marked, and its tooltip
    // names what it conflicts with. Its categories are fixed, so there's nothing to clean up between Play sessions.
    [NoAutoStaticsCleanup]
    public class InputsTabPanel : UIPanel, IMenuTab
    {
        // An input's row: its name and tooltip are "InputsTab.<Name>.Text" and ".Tooltip" in the Menu table. Part is the
        // composite part it binds (an axis's "negative" or "positive" key), for an input that's half of an axis. Contexts
        // are when it does what it does, for its conflicts.
        private class Entry : InputConflicts.IInput
        {
            private readonly Func<InputAction> getAction;

            public Entry(string name, Func<InputAction> getAction, InputContexts contexts, string part = null,
                bool ignoredWithShortcutModifier = false)
            {
                Name = name;
                this.getAction = getAction;
                Contexts = contexts;
                Part = part;
                IgnoredWithShortcutModifier = ignoredWithShortcutModifier;
            }

            public string Name { get; }
            public InputContexts Contexts { get; }
            public string Part { get; }
            public bool IgnoredWithShortcutModifier { get; }

            public InputAction Action
            {
                get
                {
                    return getAction();
                }
            }
        }

        // A shown row, and each of its bindings with its buttons
        private class Row
        {
            public Entry Entry;
            public string CategoryName;
            public VisualElement Element;
            public readonly List<(InputConflicts.Binding Binding, VisualElement Element, List<Button> Buttons)> Bindings =
                new List<(InputConflicts.Binding, VisualElement, List<Button>)>();
        }

        // A category's header is "InputsTab.<Name>Header.Text" in the Menu table
        private class Category
        {
            public readonly string Name;
            public readonly Entry[] Entries;

            public Category(string name, params Entry[] entries)
            {
                Name = name;
                Entries = entries;
            }
        }

        // Hotkeys and the pointer's buttons act whenever the level view has input (Always). Modifiers only act during the
        // gestures they change, so one key can be several modifiers whose gestures never happen at once (such as Ctrl for
        // Align Contiguous while dragging a texture, and Keep Height Above Floor while dragging an object).
        private static readonly Category[] Categories =
        {
            new Category("Camera",
                new Entry("MoveForward", () => ForgePlusInput.Camera.Advance, InputContexts.Always, "positive"),
                new Entry("MoveBack", () => ForgePlusInput.Camera.Advance, InputContexts.Always, "negative"),
                new Entry("MoveLeft", () => ForgePlusInput.Camera.Strafe, InputContexts.Always, "negative"),
                new Entry("MoveRight", () => ForgePlusInput.Camera.Strafe, InputContexts.Always, "positive"),
                new Entry("MoveUp", () => ForgePlusInput.Camera.Elevate, InputContexts.Always, "positive"),
                new Entry("MoveDown", () => ForgePlusInput.Camera.Elevate, InputContexts.Always, "negative"),
                new Entry("MoveFaster", () => ForgePlusInput.Camera.Turbo, InputContexts.CameraMove),
                new Entry("Look", () => ForgePlusInput.Camera.EnableLook, InputContexts.Always),
                new Entry("Pan", () => ForgePlusInput.Camera.Pan, InputContexts.Always),
                // Held as Select is pressed in the level, it pans instead of whatever Select would do
                new Entry("PanWithSelect", () => ForgePlusInput.Camera.PanModifier, InputContexts.WorldPointer),
                new Entry("FrameSelection", () => ForgePlusInput.Camera.FrameSelected, InputContexts.Always),
                new Entry("TopDownView", () => ForgePlusInput.Camera.ToggleOrthographic, InputContexts.Always,
                    ignoredWithShortcutModifier: true)),
            new Category("Selection",
                new Entry("Select", () => ForgePlusInput.Editing.Select, InputContexts.Always),
                new Entry("AddToSelection", () => ForgePlusInput.Editing.AddToSelection,
                    InputContexts.WorldClick | InputContexts.RectangleSelect),
                new Entry("SelectTouching", () => ForgePlusInput.Editing.SelectTouching, InputContexts.RectangleSelect),
                new Entry("CancelSelectionBox", () => ForgePlusInput.Editing.CancelSelectionBox, InputContexts.RectangleSelect),
                new Entry("DeleteSelected", () => ForgePlusInput.Editing.DeleteSelected, InputContexts.Always)),
            new Category("Moving",
                new Entry("LockX", () => ForgePlusInput.Editing.LockX, InputContexts.Always, ignoredWithShortcutModifier: true),
                new Entry("LockY", () => ForgePlusInput.Editing.LockY, InputContexts.Always, ignoredWithShortcutModifier: true),
                // Whatever snaps: dragging geometry, objects, textures and fields' labels, nudging textures, and drawing
                new Entry("InvertGridSnap", () => ForgePlusInput.Editing.InvertGridSnap,
                    InputContexts.GeometryDrag | InputContexts.ObjectDrag | InputContexts.Draw | InputContexts.TextureDrag |
                    InputContexts.TextureNudge | InputContexts.ValueDrag),
                new Entry("KeepRelativeHeight", () => ForgePlusInput.Editing.KeepRelativeHeight, InputContexts.ObjectDrag),
                new Entry("KeepTexturesWithPoint", () => ForgePlusInput.Editing.KeepTexturesWithPoint, InputContexts.GeometryDrag),
                new Entry("AllowInvalidGeometry", () => ForgePlusInput.Editing.AllowInvalidGeometry,
                    InputContexts.GeometryDrag | InputContexts.Draw)),
            // Nudging acts on its press while a surface is selected in Textures mode, even mid-click or mid-drag
            new Category("Textures",
                new Entry("NudgeUp", () => ForgePlusInput.Editing.NudgeUp, TextureModeContexts),
                new Entry("NudgeDown", () => ForgePlusInput.Editing.NudgeDown, TextureModeContexts),
                new Entry("NudgeLeft", () => ForgePlusInput.Editing.NudgeLeft, TextureModeContexts),
                new Entry("NudgeRight", () => ForgePlusInput.Editing.NudgeRight, TextureModeContexts),
                new Entry("AlignToSelection", () => ForgePlusInput.Editing.AlignToSelection, InputContexts.TextureClick),
                // Read as the drag begins, and watched during it
                new Entry("AlignContiguous", () => ForgePlusInput.Editing.AlignContiguous,
                    InputContexts.TextureDragStart | InputContexts.TextureDrag | InputContexts.TextureNudge),
                // Only read as the drag begins (letting go of it mid-drag changes nothing)
                new Entry("TargetOuterLayer", () => ForgePlusInput.Editing.TargetOuterLayer, InputContexts.TextureDragStart)),
            new Category("Platforms",
                new Entry("UsePlatform", () => ForgePlusInput.Editing.UsePlatform, InputContexts.Always,
                    ignoredWithShortcutModifier: true),
                new Entry("ActivatePlatform", () => ForgePlusInput.Editing.ActivatePlatform, InputContexts.Always,
                    ignoredWithShortcutModifier: true),
                new Entry("ObstructPlatform", () => ForgePlusInput.Editing.ObstructPlatform, InputContexts.Always)),
            new Category("Interface",
                new Entry("ToggleUI", () => ForgePlusInput.Interface.ToggleUI, InputContexts.Always),
                new Entry("ToggleMenu", () => ForgePlusInput.Interface.ToggleMenu, InputContexts.Always),
                new Entry("Undo", () => ForgePlusInput.Interface.Undo, InputContexts.Always),
                new Entry("Redo", () => ForgePlusInput.Interface.Redo, InputContexts.Always)),
        };

        private const InputContexts TextureModeContexts =
            InputContexts.TextureClick | InputContexts.TextureDragStart | InputContexts.TextureDrag | InputContexts.TextureNudge;

        // Shows each binding as it is now (after a rebinding or a reset)
        private readonly List<Action> refreshes = new List<Action>();

        private readonly List<Row> rows = new List<Row>();
        private readonly List<InputConflicts.Binding> shownBindings = new List<InputConflicts.Binding>();

        private VisualElement rebindingLayer;
        private Label rebindingTitle;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/InputsTab";
            }
        }

        public VisualElement Actions
        {
            get
            {
                return null;
            }
        }

        protected override void OnLoaded()
        {
            Root.Find<Button>("reset-all").clicked += InputBindings.ResetAll;

            rebindingLayer = Root.Q("rebinding-layer");
            rebindingTitle = rebindingLayer.Q<Label>("rebinding-title");
            rebindingLayer.RemoveFromHierarchy();

            var entries = Root.Find<ScrollView>("entries");
            foreach (var category in Categories)
            {
                var header = new Label($"@Menu/InputsTab.{category.Name}Header.Text");
                header.AddToClassList("fp-inputs__header");
                entries.Add(header);

                foreach (var entry in category.Entries)
                {
                    entries.Add(CreateRow(entry, category.Name));
                }
            }

            Strings.Localize(entries);

            InputBindings.OnChanged += Refresh;
            Refresh();
        }

        protected override void OnUnloading()
        {
            InputBindings.OnChanged -= Refresh;
            InputBindings.CancelRebinding();

            rebindingLayer.RemoveFromHierarchy();
            rebindingLayer.ClearAllBindings();
            refreshes.Clear();
            rows.Clear();
            shownBindings.Clear();
        }

        private VisualElement CreateRow(Entry entry, string categoryName)
        {
            var action = entry.Action;

            // Its tooltip is shown with its conflicts (ShowConflicts)
            var row = new VisualElement();
            row.AddToClassList("fp-inputs__row");

            var rowView = new Row { Entry = entry, CategoryName = categoryName, Element = row };
            rows.Add(rowView);

            var label = new Label($"@Menu/InputsTab.{entry.Name}.Text");
            label.AddToClassList("fp-inputs__label");
            row.Add(label);

            var bindingArea = new VisualElement();
            bindingArea.AddToClassList("fp-inputs__bindings");
            row.Add(bindingArea);

            var partIndexes = new List<int>();
            foreach (var parts in ShownBindings(action, entry.Part))
            {
                var binding = new VisualElement();
                binding.AddToClassList("fp-inputs__binding");
                bindingArea.Add(binding);

                var buttons = new List<Button>();
                for (var i = 0; i < parts.Count; i++)
                {
                    if (i > 0)
                    {
                        var plus = new Label("+");
                        plus.AddToClassList("fp-inputs__plus");
                        binding.Add(plus);
                    }

                    var button = CreateBindingButton(action, parts[i], label);
                    binding.Add(button);
                    buttons.Add(button);
                    partIndexes.Add(parts[i]);
                }

                var conflictBinding = new InputConflicts.Binding(entry, parts);
                rowView.Bindings.Add((conflictBinding, binding, buttons));
                shownBindings.Add(conflictBinding);
            }

            var reset = new Button(() =>
            {
                InputBindings.CancelRebinding();

                foreach (var bindingIndex in partIndexes)
                {
                    InputBindings.Reset(action, bindingIndex);
                }
            })
            {
                text = "@Menu/InputsTab.ResetEntry.Control.Text",
                tooltip = "@Menu/InputsTab.ResetEntry.Tooltip",
            };
            reset.AddToClassList("fp-button");
            reset.AddToClassList("fp-inputs__reset");
            row.Add(reset);

            refreshes.Add(() => reset.SetEnabled(partIndexes.Exists(bindingIndex => InputBindings.HasOverride(action, bindingIndex))));

            return row;
        }

        // Its tooltip is shown with its binding's conflicts (ShowConflicts)
        private Button CreateBindingButton(InputAction action, int bindingIndex, Label entryLabel)
        {
            var button = new Button();
            button.AddToClassList("fp-button");
            button.AddToClassList("fp-inputs__key");

            button.clicked += () =>
            {
                ShowRebindingLayer(entryLabel.text);
                InputBindings.Rebind(action, bindingIndex, HideRebindingLayer);
            };

            refreshes.Add(() => button.text = BindingText(action, bindingIndex));

            return button;
        }

        // As the Input System shows it, except Command (which it calls "meta")
        private static string BindingText(InputAction action, int bindingIndex)
        {
            if (action.bindings[bindingIndex].effectivePath == "<Keyboard>/meta")
            {
                return Strings.Get(Strings.Menu, "InputsTab.CommandKey.Text");
            }

            return action.GetBindingDisplayString(bindingIndex);
        }

        // A binding's keys and buttons, as they're shown ("Ctrl + Z")
        private static string BindingText(InputConflicts.Binding binding)
        {
            var parts = new List<string>();
            foreach (var partIndex in binding.PartIndexes)
            {
                parts.Add(BindingText(binding.Input.Action, partIndex));
            }

            return string.Join(" + ", parts);
        }

        private void Refresh()
        {
            foreach (var refresh in refreshes)
            {
                refresh();
            }

            ShowConflicts();
        }

        // Marks each conflicting binding and its row, and adds what it conflicts with to their tooltips
        private void ShowConflicts()
        {
            var conflicts = InputConflicts.Find(shownBindings);
            var bindingTooltip = Strings.Get(Strings.Menu, "InputsTab.Binding.Tooltip");

            foreach (var row in rows)
            {
                var rowConflicts = new List<string>();

                foreach (var (binding, element, buttons) in row.Bindings)
                {
                    var bindingConflicts = new List<string>();
                    if (conflicts.TryGetValue(binding, out var others))
                    {
                        var bindingText = BindingText(binding);
                        foreach (var other in others)
                        {
                            bindingConflicts.Add(Strings.Get(Strings.Menu, "InputsTab.Conflict.Text", bindingText,
                                InputName(other.Input), CategoryName(other.Input), BindingText(other)));
                        }
                    }

                    element.EnableInClassList("fp-inputs__binding--conflict", bindingConflicts.Count > 0);
                    foreach (var button in buttons)
                    {
                        Strings.SetTooltip(button, WithConflicts(bindingTooltip, bindingConflicts));
                    }

                    rowConflicts.AddRange(bindingConflicts);
                }

                row.Element.EnableInClassList("fp-inputs__row--conflict", rowConflicts.Count > 0);
                Strings.SetTooltip(row.Element,
                    WithConflicts(Strings.Get(Strings.Menu, $"InputsTab.{row.Entry.Name}.Tooltip"), rowConflicts));
            }
        }

        private static string WithConflicts(string tooltip, List<string> conflicts)
        {
            if (conflicts.Count == 0)
            {
                return tooltip;
            }

            return $"{tooltip}\n\n{Strings.Get(Strings.Menu, "InputsTab.Conflicts.Text")}\n{string.Join("\n", conflicts)}";
        }

        private static string InputName(InputConflicts.IInput input)
        {
            return Strings.Get(Strings.Menu, $"InputsTab.{input.Name}.Text");
        }

        private string CategoryName(InputConflicts.IInput input)
        {
            var row = rows.Find(candidate => candidate.Entry == input);

            return row != null ? Strings.Get(Strings.Menu, $"InputsTab.{row.CategoryName}Header.Text") : string.Empty;
        }

        // Over the whole UI (the menu's tabs too)
        private void ShowRebindingLayer(string inputName)
        {
            var uiRoot = Root.panel?.visualTree.Q(className: "fp-root");
            if (uiRoot == null)
            {
                return;
            }

            rebindingTitle.text = inputName;
            uiRoot.Add(rebindingLayer);
        }

        private void HideRebindingLayer()
        {
            rebindingLayer.RemoveFromHierarchy();
        }

        // The action's keyboard and mouse bindings (not touch's), each as its binding's index or its composite's parts'
        // indexes (just the given part's, for half of an axis). Command's are only shown on a Mac.
        private static IEnumerable<List<int>> ShownBindings(InputAction action, string part)
        {
            var bindings = action.bindings;

            for (var i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].isPartOfComposite)
                {
                    continue;
                }

                var parts = new List<int>();
                if (bindings[i].isComposite)
                {
                    for (var partIndex = i + 1; partIndex < bindings.Count && bindings[partIndex].isPartOfComposite; partIndex++)
                    {
                        if (part == null || string.Equals(bindings[partIndex].name, part, StringComparison.OrdinalIgnoreCase))
                        {
                            parts.Add(partIndex);
                        }
                    }
                }
                else
                {
                    parts.Add(i);
                }

                if (parts.Count > 0 && parts.TrueForAll(partIndex => IsShown(bindings[partIndex])))
                {
                    yield return parts;
                }
            }
        }

        // By its default path, whatever it's rebound to
        private static bool IsShown(InputBinding binding)
        {
            var path = binding.path ?? string.Empty;

            if (path.StartsWith("<Keyboard>/meta", StringComparison.OrdinalIgnoreCase) &&
                Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.OSXEditor)
            {
                return false;
            }

            return path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase) || path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase);
        }
    }
}
