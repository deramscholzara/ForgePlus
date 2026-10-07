using ForgePlus.Extensions;
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public static class UIElementExtensions
    {
        private const string InspectorScopeClassName = "fp-inspector-scope";

        // A named element, or the control inside the template instance of that name
        public static T Find<T>(this VisualElement root, string name) where T : VisualElement
        {
            var element = root.Q(name);

            if (element is T match)
            {
                return match;
            }

            return element?.Q<T>();
        }

        // The element itself, or its nearest ancestor, with the class (or null)
        public static VisualElement GetAncestorWithClass(this VisualElement element, string className)
        {
            for (; element != null; element = element.parent)
            {
                if (element.ClassListContains(className))
                {
                    return element;
                }
            }

            return null;
        }

        // Whether the element is part of a text or number field (such as its input, or the text in it)
        public static bool IsInTextInputField(this VisualElement element)
        {
            return element.GetAncestorWithClass(TextInputBaseField<string>.ussClassName) != null;
        }

        // Calls open as the element is pressed, or submitted from the keyboard (such as with return, while it has
        // focus). Replacing the element's own handling sees the events first, and keeps them from the element.
        public static void OpensOnPress(this VisualElement element, Action open, bool replacesOwnHandling = false)
        {
            var trickleDown = replacesOwnHandling ? TrickleDown.TrickleDown : TrickleDown.NoTrickleDown;

            element.RegisterCallback<PointerDownEvent>(pointerDownEvent =>
            {
                if (pointerDownEvent.button == 0)
                {
                    StopPropagation(pointerDownEvent, replacesOwnHandling);
                    open();
                }
            }, trickleDown);

            element.RegisterCallback<NavigationSubmitEvent>(submitEvent =>
            {
                StopPropagation(submitEvent, replacesOwnHandling);
                open();
            }, trickleDown);
        }

        // Once the item is laid out (it has no position to scroll to before then), unless it's been removed by then
        public static void ScrollToAfterLayout(this ScrollView scrollView, VisualElement item)
        {
            void ScrollTo()
            {
                scrollView.schedule.Execute(() =>
                {
                    if (scrollView.contentContainer.Contains(item))
                    {
                        scrollView.ScrollTo(item);
                    }
                });
            }

            if (!float.IsNaN(item.layout.width))
            {
                ScrollTo();
                return;
            }

            void OnLaidOut(GeometryChangedEvent geometryChangedEvent)
            {
                item.UnregisterCallback<GeometryChangedEvent>(OnLaidOut);
                ScrollTo();
            }

            item.RegisterCallback<GeometryChangedEvent>(OnLaidOut);
        }

        // Keeps an element's property in step with a data source's property, pushed by the source's change notifications
        public static DataBinding Bind(this VisualElement element, string elementProperty, object dataSource, string sourceProperty, BindingMode mode = BindingMode.ToTarget)
        {
            var binding = new DataBinding
            {
                dataSource = dataSource,
                dataSourcePath = new PropertyPath(sourceProperty),
                bindingMode = mode,
            };

            element.SetBinding(elementProperty, binding);

            return binding;
        }

        public static DataBinding BindValue(this VisualElement element, object dataSource, string sourceProperty)
        {
            return element.Bind("value", dataSource, sourceProperty, BindingMode.TwoWay);
        }

        public static DataBinding BindEnabled(this VisualElement element, object dataSource, string sourceProperty)
        {
            return element.Bind("enabledSelf", dataSource, sourceProperty);
        }

        // Shows the element while the source's bool property is true
        public static DataBinding BindShown(this VisualElement element, object dataSource, string sourceProperty)
        {
            var binding = element.Bind("style.display", dataSource, sourceProperty);
            binding.sourceToUiConverters.AddConverter((ref bool isShown) => new StyleEnum<DisplayStyle>(isShown ? DisplayStyle.Flex : DisplayStyle.None));

            return binding;
        }

        // Binds an inspector layout's rows (template instances named after the source's properties) to the source.
        // Those that can edit their property do while it can be set (see BindEditability). Rows in a scope (an element
        // with the scope class, named after one of the source's properties) bind to that property's object instead.
        public static void BindInspectorFields(this VisualElement root, object dataSource)
        {
            BindEach<Label>(root, dataSource, "fp-inspector-value", (value, source) => BindToInstanceName(value, "text", source));
            BindEach<Image>(root, dataSource, "fp-inspector-texture", (texture, source) => BindToInstanceName(texture, "image", source));

            BindEach<TextField>(root, dataSource, "fp-inspector-text-field", BindInspectorField);
            BindEach<IntegerField>(root, dataSource, "fp-inspector-text-field", BindInspectorField);
            BindEach<FloatField>(root, dataSource, "fp-inspector-text-field", BindInspectorField);

            // A range's second field edits the source's <Name>Maximum property
            BindEach<IntegerField>(root, dataSource, "fp-inspector-range-maximum", BindInspectorRangeMaximum);
            BindEach<FloatField>(root, dataSource, "fp-inspector-range-maximum", BindInspectorRangeMaximum);
            BindEach<DropdownField>(root, dataSource, "fp-inspector-dropdown", BindInspectorDropdown);

            // A pair's fields edit the source's <Name>X and <Name>Y properties (each named after its part)
            BindEach<IntegerField>(root, dataSource, "fp-inspector-pair-field", BindInspectorPairField);

            // A note named after one of the source's text properties shows it (the others are fixed text)
            BindEach<Label>(root, dataSource, "fp-inspector-note", BindInspectorNote);

            BindEach<Toggle>(root, dataSource, "fp-inspector-flag", BindInspectorValue);
            BindEach<LightIndexField>(root, dataSource, "fp-inspector-light", BindInspectorValue);
            BindEach<SliderInt>(root, dataSource, "fp-inspector-slider", BindInspectorSlider);

            // A choice of one (mutually exclusive options), as the index of its selected option
            BindEach<RadioButtonGroup>(root, dataSource, "fp-inspector-choice", BindInspectorValue);

            BindEach<PlayButton>(root, dataSource, "fp-inspector-play", BindInspectorPlayButton);
        }

        private static void BindEach<T>(VisualElement root, object dataSource, string className, Action<T, object> bind) where T : VisualElement
        {
            root.Query<T>(className: className).ForEach(element =>
            {
                var source = SourceOf(element, root, dataSource);
                if (source != null)
                {
                    bind(element, source);
                }
            });
        }

        // The data source, or within a scope, the object of the (outer source's) property the scope is named after
        private static object SourceOf(VisualElement element, VisualElement root, object dataSource)
        {
            var scope = element.parent?.GetAncestorWithClass(InspectorScopeClassName);
            if (scope == null || scope == root || !root.Contains(scope))
            {
                return dataSource;
            }

            var outerSource = SourceOf(scope, root, dataSource);

            return outerSource?.GetType().GetProperty(scope.name)?.GetValue(outerSource);
        }

        // A row's play button calls its source's Play<Name>() method, while its Is<Name>Playable property is true
        private static void BindInspectorPlayButton(PlayButton button, object dataSource)
        {
            var row = button.GetFirstAncestorOfType<TemplateContainer>();
            var sourceType = dataSource.GetType();
            var play = sourceType.GetMethod("Play" + row.name, Type.EmptyTypes);
            var playableProperty = $"Is{row.name}Playable";

            if (play == null)
            {
                button.SetEnabled(false);
                return;
            }

            button.clicked += () => play.Invoke(dataSource, null);

            if (sourceType.GetProperty(playableProperty) != null)
            {
                button.BindEnabled(dataSource, playableProperty);
            }
        }

        private static void BindInspectorValue<TValue>(BaseField<TValue> field, object dataSource)
        {
            var row = field.GetFirstAncestorOfType<TemplateContainer>();
            var isEditable = BindEditability(row, dataSource, row.name);

            field.Bind("value", dataSource, row.name, ModeFor(isEditable));
        }

        // A field is read-only while its property can't be set
        private static void BindInspectorField<TValue>(TextInputBaseField<TValue> field, object dataSource)
        {
            var row = field.GetFirstAncestorOfType<TemplateContainer>();
            var isEditable = BindEditability(row, dataSource, row.name);

            BindTextInput(field, dataSource, row.name, isEditable);
        }

        // A slider beside a row's number field edits the source's <Name>Slider property (the row's editability is the
        // number field's)
        private static void BindInspectorSlider(SliderInt slider, object dataSource)
        {
            var sliderProperty = slider.GetFirstAncestorOfType<TemplateContainer>().name + "Slider";

            slider.Bind("value", dataSource, sliderProperty, ModeFor(HasSetter(dataSource, sliderProperty)));
        }

        // The row's editability is its first field's
        private static void BindInspectorRangeMaximum<TValue>(TextInputBaseField<TValue> field, object dataSource)
        {
            var maximumProperty = field.GetFirstAncestorOfType<TemplateContainer>().name + "Maximum";

            BindTextInput(field, dataSource, maximumProperty, HasSetter(dataSource, maximumProperty));
        }

        // Hidden while its text is empty
        private static void BindInspectorNote(Label note, object dataSource)
        {
            if (string.IsNullOrEmpty(note.name) || dataSource.GetType().GetProperty(note.name)?.PropertyType != typeof(string))
            {
                return;
            }

            note.Bind("text", dataSource, note.name);

            var shown = note.Bind("style.display", dataSource, note.name);
            shown.sourceToUiConverters.AddConverter((ref string text) => new StyleEnum<DisplayStyle>(string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex));
        }

        // The row is editable while its parts can be set
        private static void BindInspectorPairField(IntegerField field, object dataSource)
        {
            var row = field.GetFirstAncestorOfType<TemplateContainer>();
            var partProperty = row.name + field.name;
            var isEditable = BindEditability(row, dataSource, partProperty);

            BindTextInput(field, dataSource, partProperty, isEditable);
        }

        // A dropdown chooses from its source's <Name>Choices, as a field edits its source property
        private static void BindInspectorDropdown(DropdownField dropdown, object dataSource)
        {
            var row = dropdown.GetFirstAncestorOfType<TemplateContainer>();
            var isEditable = BindEditability(row, dataSource, row.name);

            dropdown.Bind("choices", dataSource, row.name + "Choices");
            dropdown.Bind("value", dataSource, row.name, ModeFor(isEditable));

            // Choices the source lists as unavailable are shown, but can't be chosen, and those it lists as Aleph One only
            // are marked as such
            var unavailableChoices = dataSource.GetType().GetProperty(row.name + "UnavailableChoices");
            var alephOneOnlyChoices = dataSource.GetType().GetProperty(row.name + "AlephOneOnlyChoices");
            OpensMenuToFitChoices(dropdown,
                                  unavailableChoices != null ? () => unavailableChoices.GetValue(dataSource) as ICollection<string> : null,
                                  alephOneOnlyChoices != null ? () => alephOneOnlyChoices.GetValue(dataSource) as ICollection<string> : null);
        }

        // The dropdown's menu is as wide as its choices need, where its own menu is only ever as wide as the dropdown
        private static void OpensMenuToFitChoices(DropdownField dropdown, Func<ICollection<string>> getUnavailableChoices, Func<ICollection<string>> getAlephOneOnlyChoices)
        {
            dropdown.OpensOnPress(() => ShowChoicesMenu(dropdown, getUnavailableChoices, getAlephOneOnlyChoices), replacesOwnHandling: true);
        }

        private static void ShowChoicesMenu(DropdownField dropdown, Func<ICollection<string>> getUnavailableChoices, Func<ICollection<string>> getAlephOneOnlyChoices)
        {
            var menu = new GenericDropdownMenu();
            var unavailableChoices = getUnavailableChoices?.Invoke();

            foreach (var choice in dropdown.choices)
            {
                if (unavailableChoices != null && unavailableChoices.Contains(choice))
                {
                    menu.AddDisabledItem(choice, choice == dropdown.value);
                }
                else
                {
                    menu.AddItem(choice, choice == dropdown.value, () => dropdown.value = choice);
                }
            }

            // Marked on the item that holds the choice's label
            var alephOneOnlyChoices = getAlephOneOnlyChoices?.Invoke();
            if (alephOneOnlyChoices != null && alephOneOnlyChoices.Count > 0)
            {
                menu.contentContainer.Query<Label>().ForEach(label =>
                {
                    var item = label.parent?.GetAncestorWithClass(GenericDropdownMenu.itemUssClassName);
                    if (item != null && alephOneOnlyChoices.Contains(label.text))
                    {
                        item.AddToClassList("fp-dropdown-item--aleph-one-only");
                    }
                });
            }

            var input = dropdown.Q(className: DropdownField.inputUssClassName) ?? dropdown;
            menu.DropDown(input.worldBound, dropdown, DropdownMenuSizeMode.Auto);
        }

        // A row is enabled while its property has a setter, and (for a property that can only sometimes be edited, such
        // as one that needs a data file to know what's valid) while the source's Is<Name>Editable property is true
        private static bool BindEditability(TemplateContainer row, object dataSource, string property)
        {
            var hasSetter = HasSetter(dataSource, property);
            var editabilityProperty = $"Is{row.name}Editable";

            if (hasSetter && dataSource.GetType().GetProperty(editabilityProperty) != null)
            {
                row.BindEnabled(dataSource, editabilityProperty);
            }
            else
            {
                row.SetEnabled(hasSetter);
            }

            return hasSetter;
        }

        private static void BindTextInput<TValue>(TextInputBaseField<TValue> field, object dataSource, string property, bool isEditable)
        {
            field.isReadOnly = !isEditable;
            field.Bind("value", dataSource, property, ModeFor(isEditable));
        }

        private static bool HasSetter(object dataSource, string property)
        {
            return dataSource.GetType().GetProperty(property)?.GetSetMethod() != null;
        }

        private static BindingMode ModeFor(bool isEditable)
        {
            return isEditable ? BindingMode.TwoWay : BindingMode.ToTarget;
        }

        private static void BindToInstanceName(VisualElement element, string elementProperty, object dataSource)
        {
            element.Bind(elementProperty, dataSource, element.GetFirstAncestorOfType<TemplateContainer>().name);
        }

        private static void StopPropagation(EventBase evt, bool isImmediate)
        {
            if (isImmediate)
            {
                evt.StopImmediatePropagation();
            }
            else
            {
                evt.StopPropagation();
            }
        }

        // Limits a text field to text Aleph One can store in the buffer and draw: typing a character it can't is
        // ignored, and pasted text loses such characters when it's set (by the field's source)
        public static void LimitToMacRomanText(this TextField field, int maximumLength)
        {
            field.maxLength = maximumLength;

            field.RegisterCallback<KeyDownEvent>(keyDownEvent =>
            {
                // Control characters (such as backspace and return) are edits and navigation, not text
                if (keyDownEvent.character >= ' ' && !AlephOneExtensions.IsDrawableMacRomanCharacter(keyDownEvent.character))
                {
                    keyDownEvent.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
        }

        // Lets clicks through the gaps in layout-only elements (template wrappers and radio groups' containers)
        // to the level, so only controls and boxes block them
        public static void IgnoreLayoutPicking(this VisualElement root)
        {
            root.Query<TemplateContainer>().ForEach(container => container.pickingMode = PickingMode.Ignore);

            root.Query<RadioButtonGroup>().ForEach(group =>
            {
                group.pickingMode = PickingMode.Ignore;

                // The group's own input is its first (its radio buttons' inputs come after it)
                var groupInput = group.Q(className: BaseField<int>.inputUssClassName);
                if (groupInput != null)
                {
                    groupInput.pickingMode = PickingMode.Ignore;
                }

                group.Query(className: RadioButtonGroup.containerUssClassName).ForEach(container => container.pickingMode = PickingMode.Ignore);
            });
        }

        public static void ClearAllBindings(this VisualElement root)
        {
            root.Query<VisualElement>().ForEach(element => element.ClearBindings());
        }
    }
}
