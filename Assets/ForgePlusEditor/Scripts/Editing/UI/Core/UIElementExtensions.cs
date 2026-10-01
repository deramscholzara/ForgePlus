using ForgePlus.Extensions;
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public static class UIElementExtensions
    {
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

        // Whether the element is part of a text or number field (such as its input, or the text in it)
        public static bool IsInTextInputField(this VisualElement element)
        {
            for (; element != null; element = element.parent)
            {
                if (element.ClassListContains(TextInputBaseField<string>.ussClassName))
                {
                    return true;
                }
            }

            return false;
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

        // Binds an inspector layout's rows, text and number fields, dropdowns, choices, textures and flags (template
        // instances named after the source's properties) to the source. Those that can edit their property do while
        // it can be set (see BindEditability).
        public static void BindInspectorFields(this VisualElement root, object dataSource)
        {
            root.Query<Label>(className: "fp-inspector-value").ForEach(value => BindToInstanceName(value, "text", dataSource));
            root.Query<Image>(className: "fp-inspector-texture").ForEach(texture => BindToInstanceName(texture, "image", dataSource));

            root.Query<TextField>(className: "fp-inspector-text-field").ForEach(field => BindInspectorField(field, dataSource));
            root.Query<IntegerField>(className: "fp-inspector-text-field").ForEach(field => BindInspectorField(field, dataSource));
            root.Query<FloatField>(className: "fp-inspector-text-field").ForEach(field => BindInspectorField(field, dataSource));

            // A range's second field edits the source's <Name>Maximum property
            root.Query<IntegerField>(className: "fp-inspector-range-maximum").ForEach(field => BindInspectorRangeMaximum(field, dataSource));
            root.Query<FloatField>(className: "fp-inspector-range-maximum").ForEach(field => BindInspectorRangeMaximum(field, dataSource));
            root.Query<DropdownField>(className: "fp-inspector-dropdown").ForEach(dropdown => BindInspectorDropdown(dropdown, dataSource));

            root.Query<Toggle>(className: "fp-inspector-flag").ForEach(flag => BindInspectorValue(flag, dataSource));
            root.Query<LightIndexField>(className: "fp-inspector-light").ForEach(light => BindInspectorValue(light, dataSource));
            root.Query<SliderInt>(className: "fp-inspector-slider").ForEach(slider => BindInspectorSlider(slider, dataSource));

            // A choice of one (mutually exclusive options), as the index of its selected option
            root.Query<RadioButtonGroup>(className: "fp-inspector-choice").ForEach(choice => BindInspectorValue(choice, dataSource));
        }

        private static void BindInspectorValue<TValue>(BaseField<TValue> field, object dataSource)
        {
            var row = field.GetFirstAncestorOfType<TemplateContainer>();
            var isEditable = BindEditability(row, dataSource);

            field.Bind("value", dataSource, row.name, isEditable ? BindingMode.TwoWay : BindingMode.ToTarget);
        }

        // A slider beside a row's number field edits the source's <Name>Slider property (the row's editability is the
        // number field's)
        private static void BindInspectorSlider(SliderInt slider, object dataSource)
        {
            var row = slider.GetFirstAncestorOfType<TemplateContainer>();
            var sliderProperty = row.name + "Slider";
            var isEditable = dataSource.GetType().GetProperty(sliderProperty)?.GetSetMethod() != null;

            slider.Bind("value", dataSource, sliderProperty, isEditable ? BindingMode.TwoWay : BindingMode.ToTarget);
        }

        // The row's editability is its first field's
        private static void BindInspectorRangeMaximum<TValue>(TextInputBaseField<TValue> field, object dataSource)
        {
            var row = field.GetFirstAncestorOfType<TemplateContainer>();
            var maximumProperty = row.name + "Maximum";
            var isEditable = dataSource.GetType().GetProperty(maximumProperty)?.GetSetMethod() != null;

            field.isReadOnly = !isEditable;
            field.Bind("value", dataSource, maximumProperty, isEditable ? BindingMode.TwoWay : BindingMode.ToTarget);
        }

        // A field edits its source property while the property has a setter, and is grayed out (read-only) until then
        private static void BindInspectorField<TValue>(TextInputBaseField<TValue> field, object dataSource)
        {
            var row = field.GetFirstAncestorOfType<TemplateContainer>();
            var isEditable = BindEditability(row, dataSource);

            field.isReadOnly = !isEditable;
            field.Bind("value", dataSource, row.name, isEditable ? BindingMode.TwoWay : BindingMode.ToTarget);
        }

        // A dropdown chooses from its source's <Name>Choices, as a field edits its source property
        private static void BindInspectorDropdown(DropdownField dropdown, object dataSource)
        {
            var row = dropdown.GetFirstAncestorOfType<TemplateContainer>();
            var isEditable = BindEditability(row, dataSource);

            dropdown.Bind("choices", dataSource, row.name + "Choices");
            dropdown.Bind("value", dataSource, row.name, isEditable ? BindingMode.TwoWay : BindingMode.ToTarget);

            // Choices the source lists as unavailable are shown, but can't be chosen
            var unavailableChoices = dataSource.GetType().GetProperty(row.name + "UnavailableChoices");
            dropdown.OpensMenuToFitChoices(unavailableChoices != null ? () => unavailableChoices.GetValue(dataSource) as ICollection<string> : null);
        }

        // A dropdown's menu (drawn over the whole UI) is as wide as the dropdown at least, and wider when its choices need
        // it (rather than always matching the dropdown, which cuts off choices in narrow panels). Replaces the menu the
        // dropdown opens itself, which is only ever as wide as it is.
        public static void OpensMenuToFitChoices(this DropdownField dropdown, Func<ICollection<string>> getUnavailableChoices = null)
        {
            dropdown.RegisterCallback<PointerDownEvent>(pointerDownEvent =>
            {
                if (pointerDownEvent.button == 0)
                {
                    pointerDownEvent.StopImmediatePropagation();
                    ShowChoicesMenu(dropdown, getUnavailableChoices);
                }
            }, TrickleDown.TrickleDown);

            // As the dropdown opens its menu from the keyboard (such as with return, while it has focus)
            dropdown.RegisterCallback<NavigationSubmitEvent>(submitEvent =>
            {
                submitEvent.StopImmediatePropagation();
                ShowChoicesMenu(dropdown, getUnavailableChoices);
            }, TrickleDown.TrickleDown);
        }

        private static void ShowChoicesMenu(DropdownField dropdown, Func<ICollection<string>> getUnavailableChoices)
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

            var input = dropdown.Q(className: DropdownField.inputUssClassName) ?? dropdown;
            menu.DropDown(input.worldBound, dropdown, DropdownMenuSizeMode.Auto);
        }

        // A row is enabled while its source property has a setter, and (for a property that can only sometimes be edited,
        // such as one that needs a data file to know what's valid) while the source's Is<Name>Editable property is true
        private static bool BindEditability(TemplateContainer row, object dataSource)
        {
            var sourceType = dataSource.GetType();
            var hasSetter = sourceType.GetProperty(row.name)?.GetSetMethod() != null;
            var editabilityProperty = $"Is{row.name}Editable";

            if (hasSetter && sourceType.GetProperty(editabilityProperty) != null)
            {
                row.BindEnabled(dataSource, editabilityProperty);
            }
            else
            {
                row.SetEnabled(hasSetter);
            }

            return hasSetter;
        }

        private static void BindToInstanceName(VisualElement element, string elementProperty, object dataSource)
        {
            element.Bind(elementProperty, dataSource, element.GetFirstAncestorOfType<TemplateContainer>().name);
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
