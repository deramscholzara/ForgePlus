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
        private LevelPlaneDrag drag;

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

        // In Select mode, dragging it moves it across the level (at its handle's height), recorded as one action when it
        // ends. Holding Allow Invalid Move lets it go where it'd make a polygon invalid.
        public void OnWorldPointerBeginDrag(WorldPointerEventData eventData)
        {
            if (ModeManager.Instance.SecondaryMode != ModeManager.SecondaryModes.Selection)
            {
                return;
            }

            SelectionManager.Instance.SelectObject(this, multiSelect: false);

            drag = new LevelPlaneDrag(eventData.PressWorldPosition, NativeObject.vertex);
            PointEditing.BeginDrag();
            PointEditing.DragHeight = eventData.PressWorldPosition.y;
        }

        public void OnWorldPointerDrag(WorldPointerEventData eventData)
        {
            if (drag == null)
            {
                return;
            }

            var pointerRay = Camera.main.ScreenPointToRay(new Vector3(eventData.Position.x, eventData.Position.y, 0f));
            PointEditing.DragPoint(this, drag.DraggedLocation(pointerRay), canBeAllowedInvalid: true);

            Inspector_Base.RefreshInspectorsOf(this);
        }

        public void OnWorldPointerEndDrag(WorldPointerEventData eventData)
        {
            if (drag == null)
            {
                return;
            }

            drag = null;
            PointEditing.EndDrag();

            Inspector_Base.RefreshInspectorsOf(this);
        }
    }
}
#endif
