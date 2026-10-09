using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.History;
using ForgePlus.LevelManipulation;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Dragging a row's label right or up raises its field's value, and left or down lowers it, by a step for each pixel
    // (ten times as fast while shift is held). It's set through the field, as typing in it would be, and does nothing
    // while the field is grayed out. Dragging is recorded as one action when it ends. Snapping (which alt/option inverts)
    // snaps a distance to the world's sixteenth-of-a-world-unit marks, and anything else to its own increment. It drags
    // a range's middle bar on its slider the same way (only the bar, not its thumbs).
    public class LabelDragger : PointerManipulator
    {
        private const float FastMultiplier = 10f;

        // A 128th of a world unit (the classic pixel distance)
        private const float DistanceStepPerPixel = world.WORLD_ONE / 128f;

        // For a number (such as degrees or a volume), unless its source's <Name>DragStep and <Name>DragSnap say otherwise
        private const float DefaultStepPerPixel = 1f;
        private const float DefaultSnapIncrement = 5f;

        private const string DraggableLabelClassName = "fp-inspector-row__label--draggable";

        private readonly VisualElement field;
        private readonly Func<float> getValue;
        private readonly Action<float> setValue;
        private readonly Func<float> getStepPerPixel;
        private readonly float snapIncrement;

        private int pointerId = PointerId.invalidPointerId;
        private float startValue;
        private float stepPerPixel;
        private float draggedPixels;

        // Called as a drag starts and ends (such as to commit what was dragged only once it ends)
        public Action OnBegin { get; set; }
        public Action OnEnd { get; set; }

        // Dragged past either end of it, the value wraps around to the other (such as an angle, from 0 to 360 degrees);
        // none means it doesn't wrap
        public float WrapPeriod { get; set; }

        // The step is worked out as each drag starts (such as from a slider's width); no snap increment means no snapping
        public LabelDragger(VisualElement field, Func<float> getValue, Action<float> setValue, Func<float> getStepPerPixel, float snapIncrement = 0f)
        {
            this.field = field;
            this.getValue = getValue;
            this.setValue = setValue;
            this.getStepPerPixel = getStepPerPixel;
            this.snapIncrement = snapIncrement;

            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        // A position (or a height), in world units
        public static LabelDragger ForDistance(WorldDistanceField field)
        {
            return new LabelDragger(field, () => field.value, value => field.value = Mathf.RoundToInt(value), () => DistanceStepPerPixel, HeightsEditing.SnapIncrement);
        }

        public static LabelDragger ForNumber(IntegerField field, float stepPerPixel, float snapIncrement)
        {
            return new LabelDragger(field, () => field.value, value => field.value = Mathf.RoundToInt(value), () => stepPerPixel, snapIncrement);
        }

        public static LabelDragger ForNumber(FloatField field, float stepPerPixel, float snapIncrement)
        {
            return new LabelDragger(field, () => field.value, value => field.value = value, () => stepPerPixel, snapIncrement);
        }

        // A range (from its first value to its second) moved as a whole, kept within its limits without narrowing. The end
        // it's moved toward moves first, so the range is never reversed on the way.
        public static LabelDragger ForRange(VisualElement field, Func<float> getFrom, Func<float> getTo, Action<float> setFrom, Action<float> setTo,
                                            Func<Vector2> getLimits, Func<float> getStepPerPixel, float snapIncrement)
        {
            void Move(float from)
            {
                var limits = getLimits();
                var width = getTo() - getFrom();
                from = Mathf.Clamp(from, limits.x, Mathf.Max(limits.x, limits.y - width));

                var movement = from - getFrom();
                if (movement > 0f)
                {
                    setTo(getTo() + movement);
                    setFrom(from);
                }
                else if (movement < 0f)
                {
                    setFrom(from);
                    setTo(getTo() + movement);
                }
            }

            return new LabelDragger(field, getFrom, Move, getStepPerPixel, snapIncrement);
        }

        // A row's distance or number field, dragged by its label. A source that's an ILabelDragHandler is told as the drag
        // starts and ends.
        public static void AttachToRow(TemplateContainer row, object dataSource)
        {
            LabelDragger dragger;
            switch (row.Q("value"))
            {
                case WorldDistanceField distance:
                    dragger = ForDistance(distance);
                    break;
                case IntegerField number:
                    dragger = ForNumber(number, StepPerPixel(row, dataSource), SnapIncrement(row, dataSource));
                    break;
                case FloatField number:
                    dragger = ForNumber(number, StepPerPixel(row, dataSource), SnapIncrement(row, dataSource));
                    break;
                default:
                    return;
            }

            dragger.WrapPeriod = SettingOf(dataSource, row.name + "DragWrap", 0f);

            AttachToLabel(row, dataSource, dragger);
        }

        // A range row's two fields, moved together by dragging its label, or its slider's middle bar (which follows the
        // pointer along the slider)
        public static void AttachToRangeRow(TemplateContainer row, MinMaxSlider slider, object dataSource)
        {
            var from = row.Q("value");
            var to = row.Q("maximum");
            var snapIncrement = SnapIncrement(row, dataSource);
            var stepPerPixel = StepPerPixel(row, dataSource);

            LabelDragger RangeDragger(Func<float> getStepPerPixel)
            {
                return ForRange(from, GetterOf(from), GetterOf(to), SetterOf(from), SetterOf(to), () => new Vector2(slider.lowLimit, slider.highLimit), getStepPerPixel, snapIncrement);
            }

            AttachToLabel(row, dataSource, RangeDragger(() => stepPerPixel));

            var tracker = slider.Q(className: "unity-min-max-slider__tracker");
            var middleBar = RangeDragger(() => (slider.highLimit - slider.lowLimit) / Mathf.Max(1f, tracker.layout.width));
            AddHandler(row, dataSource, middleBar);
            slider.Q(className: "unity-min-max-slider__dragger").AddManipulator(middleBar);
        }

        private static void AttachToLabel(TemplateContainer row, object dataSource, LabelDragger dragger)
        {
            var label = row.Q<Label>("label");
            if (label == null)
            {
                return;
            }

            AddHandler(row, dataSource, dragger);
            label.AddManipulator(dragger);
            label.AddToClassList(DraggableLabelClassName);
        }

        private static void AddHandler(TemplateContainer row, object dataSource, LabelDragger dragger)
        {
            if (dataSource is ILabelDragHandler handler)
            {
                dragger.OnBegin = () => handler.BeginLabelDrag(row.name);
                dragger.OnEnd = () => handler.EndLabelDrag(row.name);
            }
        }

        private static float StepPerPixel(TemplateContainer row, object dataSource)
        {
            return SettingOf(dataSource, row.name + "DragStep", DefaultStepPerPixel);
        }

        private static float SnapIncrement(TemplateContainer row, object dataSource)
        {
            return SettingOf(dataSource, row.name + "DragSnap", DefaultSnapIncrement);
        }

        private static float SettingOf(object dataSource, string property, float fallback)
        {
            var value = dataSource?.GetType().GetProperty(property)?.GetValue(dataSource);

            return value is float setting ? setting : fallback;
        }

        private static Func<float> GetterOf(VisualElement field)
        {
            switch (field)
            {
                case IntegerField number:
                    return () => number.value;
                case FloatField number:
                    return () => number.value;
                default:
                    return () => 0f;
            }
        }

        private static Action<float> SetterOf(VisualElement field)
        {
            switch (field)
            {
                case IntegerField number:
                    return value => number.value = Mathf.RoundToInt(value);
                case FloatField number:
                    return value => number.value = value;
                default:
                    return value => { };
            }
        }

        private float Snap(float value)
        {
            // While snapping is on (which alt/option inverts)
            if (snapIncrement <= 0f || !AxisLocks.Instance.SnapToGrid)
            {
                return value;
            }

            return Mathf.Round(value / snapIncrement) * snapIncrement;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnPointerDown(PointerDownEvent pointerDownEvent)
        {
            // Pressed on the element itself (not on a slider's thumbs, which are inside its middle bar)
            if (pointerId != PointerId.invalidPointerId || pointerDownEvent.target != target || !CanStartManipulation(pointerDownEvent) ||
                !field.enabledInHierarchy)
            {
                return;
            }

            pointerId = pointerDownEvent.pointerId;
            startValue = getValue();
            stepPerPixel = getStepPerPixel();
            draggedPixels = 0f;

            target.CapturePointer(pointerId);
            LevelHistory.BeginGesture();
            OnBegin?.Invoke();

            pointerDownEvent.StopPropagation();
        }

        // Up is the panel's negative Y
        private void OnPointerMove(PointerMoveEvent pointerMoveEvent)
        {
            if (pointerMoveEvent.pointerId != pointerId || !target.HasPointerCapture(pointerId))
            {
                return;
            }

            var delta = pointerMoveEvent.deltaPosition;
            draggedPixels += (delta.x - delta.y) * (Hotkeys.IsShiftPressed ? FastMultiplier : 1f);

            var value = Snap(startValue + draggedPixels * stepPerPixel);
            if (WrapPeriod > 0f)
            {
                value = Mathf.Repeat(value, WrapPeriod);
            }

            if (value != getValue())
            {
                setValue(value);
            }

            pointerMoveEvent.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent pointerUpEvent)
        {
            if (pointerUpEvent.pointerId == pointerId && target.HasPointerCapture(pointerId))
            {
                target.ReleasePointer(pointerId);
                pointerUpEvent.StopPropagation();
            }
        }

        // However the drag ends (released, or the capture is taken)
        private void OnPointerCaptureOut(PointerCaptureOutEvent pointerCaptureOutEvent)
        {
            EndDrag();
        }

        // Such as when its inspector closes during the drag
        private void OnDetachFromPanel(DetachFromPanelEvent detachFromPanelEvent)
        {
            EndDrag();
        }

        private void EndDrag()
        {
            if (pointerId == PointerId.invalidPointerId)
            {
                return;
            }

            pointerId = PointerId.invalidPointerId;
            OnEnd?.Invoke();
            LevelHistory.EndGesture();
        }
    }
}
