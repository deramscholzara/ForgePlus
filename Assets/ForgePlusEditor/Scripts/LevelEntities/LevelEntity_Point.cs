#if !NO_EDITING
using AlephOne;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    // One of the level's points (endpoints), where its polygons' corners meet. It's shown, clicked and dragged in Geometry
    // mode through its handles at those corners (PointHandles), which follow its selection.
    public class LevelEntity_Point : ISelectionDisplayable, IInspectable, IWorldPointerHandler
    {
        private PointsMove move;
        private LineDrawing drawing;

        public LevelEntity_Point(LevelEntity_Level parentLevel, short nativeIndex)
        {
            ParentLevel = parentLevel;
            NativeIndex = nativeIndex;
        }

        public LevelEntity_Level ParentLevel { get; }

        public short NativeIndex { get; }

        public endpoint_data NativeObject
        {
            get
            {
                return ParentLevel.Level.EndpointList[NativeIndex];
            }
        }

        public bool IsSelected { get; private set; }

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - its handles are only shown (and clicked) while Geometry mode shows points
        }

        public void DisplaySelectionState(bool state)
        {
            IsSelected = state;
        }

        public void Inspect()
        {
            InspectorPanel.Instance.AddInspector(new Inspector_Point(this));
        }

        public void OnWorldPointerClick(WorldPointerEventData eventData)
        {
            SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false);
        }

        // In Select mode, dragging it moves it across the level (at its handle's height), with the rest of the selection if
        // it's selected (PointsMove), recorded as one action when it ends. Holding Allow Invalid Move lets it go where it'd
        // make a polygon invalid. In Draw mode, dragging it draws a new line from it (LineDrawing), without moving it.
        public void OnWorldPointerBeginDrag(WorldPointerEventData eventData)
        {
            switch (ModeManager.Instance.SecondaryMode)
            {
                case ModeManager.SecondaryModes.Selection:
                    SelectionManager.Instance.SelectObject(this, multiSelect: false);
                    move = PointsMove.ForSelection(ParentLevel, this, eventData.PressWorldPosition);
                    break;
                case ModeManager.SecondaryModes.Drawing:
                    drawing = LineDrawing.Begin(ParentLevel, NativeIndex, eventData.PressWorldPosition);
                    break;
            }
        }

        public void OnWorldPointerDrag(WorldPointerEventData eventData)
        {
            var pointerRay = Camera.main.ScreenPointToRay(new Vector3(eventData.Position.x, eventData.Position.y, 0f));

            if (drawing != null)
            {
                drawing.Drag(pointerRay, eventData.Position);
                return;
            }

            if (move == null)
            {
                return;
            }

            move.Drag(pointerRay);

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        public void OnWorldPointerEndDrag(WorldPointerEventData eventData)
        {
            if (drawing != null)
            {
                var endedDrawing = drawing;
                drawing = null;
                endedDrawing.End();

                InspectorPanel.Instance.RefreshAllInspectors();
                return;
            }

            if (move == null)
            {
                return;
            }

            var ended = move;
            move = null;
            ended.End();

            InspectorPanel.Instance.RefreshAllInspectors();
        }
    }
}
#endif
