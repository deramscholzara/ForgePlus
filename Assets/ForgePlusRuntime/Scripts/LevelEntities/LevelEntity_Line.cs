using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using UnityEngine;
using AlephOne;

namespace RuntimeCore.Entities.Geometry
{
    // TODO: Should inherit from LevelEntity_Base, and should have a separate EditableSurface component
    public partial class LevelEntity_Line : EditableSurface_Base, ISelectionDisplayable, IInspectable
    {
        public short NativeIndex { get; set; }
        public line_data NativeObject { get; set; }
        public LevelEntity_Side ClockwiseSide;
        public LevelEntity_Side CounterclockwiseSide;

        public LevelEntity_Level ParentLevel { private get; set; }

        // Clicked and dragged through its lines drawn over the level (PointHandles), in Geometry mode
        public override void OnValidatedPointerClick(WorldPointerEventData eventData)
        {
            ClickLine();
        }

        public override void OnValidatedBeginDrag(WorldPointerEventData eventData)
        {
            BeginLineMove(eventData);
        }

        public override void OnValidatedDrag(WorldPointerEventData eventData)
        {
            DragLineMove(eventData);
        }

        public override void OnValidatedEndDrag(WorldPointerEventData eventData)
        {
            EndLineMove();
        }

        public override void SetSelectability(bool enabled)
        {
            base.SetSelectability(enabled);

            // TODO: Set Line selectability (enable scene-clickable element)
        }

        // Its lines drawn over the level (PointHandles) are filled red while it's selected
        public bool IsSelected { get; private set; }

        public void DisplaySelectionState(bool state)
        {
            IsSelected = state;
        }

        public void Inspect()
        {
            var inspector = new Inspector_Line(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void GenerateSurfaces()
        {
            ClockwiseSide = LevelEntity_Side.AssembleEntity(ParentLevel, isClockwise: true, NativeIndex);
            if (ClockwiseSide)
            {
                ClockwiseSide.transform.SetParent(transform);
            }

            CounterclockwiseSide = LevelEntity_Side.AssembleEntity(ParentLevel, isClockwise: false, NativeIndex);
            if (CounterclockwiseSide)
            {
                CounterclockwiseSide.transform.SetParent(transform);
            }
        }
    }
}
