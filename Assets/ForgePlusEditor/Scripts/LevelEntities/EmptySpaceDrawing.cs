#if !NO_EDITING
using ForgePlus.Inspection;
using RuntimeCore.Entities;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // With Geometry mode's Draw tool, a drag from empty space just after a click there (a press, release and press) draws
    // a new line from a new point where it was pressed (LineDrawing.BeginFromEmptySpace). WorldPointer asks for it
    // (EmptySpaceDragger), which ForgePlusUI sets up.
    public sealed class EmptySpaceDrawing : IWorldPointerHandler
    {
        private LineDrawing drawing;

        public static IWorldPointerHandler TryCreate(Vector2 screenPosition)
        {
            var level = LevelEntity_Level.Instance;
            if (!level || level.Points == null || LevelEditing.IsRebuildPending ||
                ModeManager.Instance.PrimaryMode != ModeManager.PrimaryModes.Geometry ||
                ModeManager.Instance.SecondaryMode != ModeManager.SecondaryModes.Drawing)
            {
                return null;
            }

            return new EmptySpaceDrawing();
        }

        public void OnWorldPointerClick(WorldPointerEventData eventData)
        {
            // Only dragging draws
        }

        public void OnWorldPointerBeginDrag(WorldPointerEventData eventData)
        {
            drawing = LineDrawing.BeginFromEmptySpace(LevelEntity_Level.Instance, RayAt(eventData.PressPosition));
            OnWorldPointerDrag(eventData);
        }

        public void OnWorldPointerDrag(WorldPointerEventData eventData)
        {
            drawing?.Drag(RayAt(eventData.Position), eventData.Position);
        }

        public void OnWorldPointerEndDrag(WorldPointerEventData eventData)
        {
            if (drawing == null)
            {
                return;
            }

            var ended = drawing;
            drawing = null;
            ended.End();

            InspectorPanel.Instance.RefreshAllInspectors();
        }

        private static Ray RayAt(Vector2 screenPosition)
        {
            return Camera.main.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));
        }
    }
}
#endif
