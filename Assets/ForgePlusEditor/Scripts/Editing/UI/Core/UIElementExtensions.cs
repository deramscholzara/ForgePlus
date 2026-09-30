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

        // Binds an inspector layout's rows, text fields, textures and flags (template instances named after the source's
        // properties) to the source, the text fields and flags read-only
        public static void BindInspectorFields(this VisualElement root, object dataSource)
        {
            root.Query<Label>(className: "fp-inspector-value").ForEach(value => BindToInstanceName(value, "text", dataSource));
            root.Query<Image>(className: "fp-inspector-texture").ForEach(texture => BindToInstanceName(texture, "image", dataSource));

            root.Query<TextField>(className: "fp-inspector-text-field").ForEach(field =>
            {
                field.isReadOnly = true;
                field.GetFirstAncestorOfType<TemplateContainer>().SetEnabled(false);
                BindToInstanceName(field, "value", dataSource);
            });

            root.Query<Toggle>(className: "fp-inspector-flag").ForEach(flag =>
            {
                flag.SetEnabled(false);
                BindToInstanceName(flag, "value", dataSource);
            });
        }

        private static void BindToInstanceName(VisualElement element, string elementProperty, object dataSource)
        {
            element.Bind(elementProperty, dataSource, element.GetFirstAncestorOfType<TemplateContainer>().name);
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
