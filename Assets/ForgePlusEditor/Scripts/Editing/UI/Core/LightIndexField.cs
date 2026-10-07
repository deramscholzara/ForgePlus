using RuntimeCore.Entities;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A light's index, with a preview of its current intensity, chosen from the level's lights (LightPicker). -1 is no
    // light, shown as "-".
    [UxmlElement]
    public partial class LightIndexField : BaseField<int>
    {
        public const int NoLight = -1;

        private readonly Label indexLabel;
        private readonly VisualElement preview;

        public LightIndexField() : this(null)
        {
        }

        public LightIndexField(string label) : base(label, new VisualElement())
        {
            var input = this.Q(className: inputUssClassName);
            input.AddToClassList("fp-light-field__input");

            indexLabel = new Label { pickingMode = PickingMode.Ignore };
            indexLabel.AddToClassList("fp-light-field__index");
            input.Add(indexLabel);

            preview = new VisualElement { pickingMode = PickingMode.Ignore };
            preview.AddToClassList("fp-light-field__preview");
            input.Add(preview);

            var arrow = new VisualElement { pickingMode = PickingMode.Ignore };
            arrow.AddToClassList("unity-base-popup-field__arrow");
            arrow.AddToClassList("fp-light-field__arrow");
            input.Add(arrow);

            this.OpensOnPress(OpenPicker);

            // Lights animate, so the preview follows its light every frame
            schedule.Execute(UpdatePreview).Every(0);

            UpdateIndex();
        }

        // Lights below this index can't be chosen (such as light 0, which a sound source's volume can't follow)
        public int MinimumLight { get; set; }

        public override void SetValueWithoutNotify(int newValue)
        {
            base.SetValueWithoutNotify(newValue);

            UpdateIndex();
            UpdatePreview();
        }

        private void OpenPicker()
        {
            if (enabledInHierarchy)
            {
                LightPicker.Show(this, value, chosenLight => value = chosenLight, MinimumLight);
            }
        }

        private void UpdateIndex()
        {
            indexLabel.text = value == NoLight ? "-" : value.ToString();
        }

        private void UpdatePreview()
        {
            var level = LevelEntity_Level.Instance;
            if (value != NoLight && level && level.Lights.TryGetValue((short) value, out var light))
            {
                Swatches.ShowIntensity(preview, light);
                preview.style.visibility = Visibility.Visible;
            }
            else
            {
                preview.style.visibility = Visibility.Hidden;
            }
        }
    }
}
