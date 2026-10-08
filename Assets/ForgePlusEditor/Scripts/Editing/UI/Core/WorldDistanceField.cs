using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // A world distance, shown and typed as WorldDistances formats and reads it. Text it can't read leaves the value as it was.
    [UxmlElement]
    public partial class WorldDistanceField : TextValueField<int>
    {
        // Letters are for a separator such as " and "
        private const string AllowedCharacters = "0123456789-+/.,% abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private bool wraps;

        // Wrapping (as a texture offset does), so it's kept from 0 to 1023
        [UxmlAttribute]
        public bool Wraps
        {
            get
            {
                return wraps;
            }
            set
            {
                wraps = value;

                // The value is set before this, as the field is made, so it's shown again here
                SetValueWithoutNotify(rawValue);
                ShowValue();
            }
        }

        public WorldDistanceField() : this(new WorldDistanceInput())
        {
        }

        private WorldDistanceField(WorldDistanceInput input) : base(null, -1, input)
        {
            input.Field = this;

            // Once typed text is committed it's shown as the value is (even if the value didn't change), and the value is
            // shown again when the format changes
            RegisterCallback<FocusOutEvent>(_ => schedule.Execute(ShowValue), TrickleDown.TrickleDown);
            RegisterCallback<KeyDownEvent>(keyDown =>
            {
                if (keyDown.keyCode == KeyCode.Return || keyDown.keyCode == KeyCode.KeypadEnter)
                {
                    schedule.Execute(ShowValue);
                }
            }, TrickleDown.TrickleDown);

            RegisterCallback<AttachToPanelEvent>(_ => WorldDistances.OnFormatChanged += ShowValue);
            RegisterCallback<DetachFromPanelEvent>(_ => WorldDistances.OnFormatChanged -= ShowValue);
        }

        public override void SetValueWithoutNotify(int newValue)
        {
            base.SetValueWithoutNotify(wraps ? WorldDistances.Wrap(newValue) : newValue);
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, int startValue)
        {
            value = startValue + Mathf.RoundToInt(delta.x);
        }

        protected override string ValueToString(int distance)
        {
            return wraps ? WorldDistances.FormatWrapped(distance) : WorldDistances.Format(distance);
        }

        protected override int StringToValue(string text)
        {
            if (!WorldDistances.TryParse(text, out var distance))
            {
                return rawValue;
            }

            // Wrapped here too, as a change event carries the value before SetValueWithoutNotify wraps it
            return wraps ? WorldDistances.Wrap(distance) : distance;
        }

        private void ShowValue()
        {
            var shown = ValueToString(rawValue);
            if (text != shown)
            {
                text = shown;
            }
        }

        private class WorldDistanceInput : TextValueInput
        {
            public WorldDistanceField Field;

            protected override string allowedCharacters
            {
                get
                {
                    return AllowedCharacters;
                }
            }

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, int startValue)
            {
                Field.ApplyInputDeviceDelta(delta, speed, startValue);
            }

            protected override string ValueToString(int distance)
            {
                return Field.ValueToString(distance);
            }

            protected override int StringToValue(string text)
            {
                return Field.StringToValue(text);
            }
        }
    }
}
