#if !NO_EDITING
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    public partial class LevelEntity_Line
    {
        private PointsMove move;
        private LineExtrusion extrusion;

        private void ClickLine()
        {
            SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false);
            InspectorPanel.Instance.RefreshAllInspectors();
        }

        // In Select mode, dragging it moves both its ends together across the level (at the height of the line it was
        // grabbed by), with the rest of the selection if it's selected (PointsMove), recorded as one action when it ends.
        // In Draw mode, dragging it adds a polygon between it and a copy of it that follows the pointer (LineExtrusion),
        // without moving anything.
        private void BeginLineMove(WorldPointerEventData eventData)
        {
            switch (ModeManager.Instance.SecondaryMode)
            {
                case ModeManager.SecondaryModes.Selection:
                    SelectionManager.Instance.SelectObject(this, multiSelect: false);
                    move = PointsMove.ForSelection(LevelEntity_Level.Instance, this, eventData.PressWorldPosition);
                    break;
                case ModeManager.SecondaryModes.Drawing:
                    extrusion = LineExtrusion.Begin(LevelEntity_Level.Instance, NativeIndex, eventData.PressWorldPosition);
                    break;
            }
        }

        private void DragLineMove(WorldPointerEventData eventData)
        {
            var pointerRay = Camera.main.ScreenPointToRay(new Vector3(eventData.Position.x, eventData.Position.y, 0f));

            if (extrusion != null)
            {
                extrusion.Drag(pointerRay);
                return;
            }

            if (move == null)
            {
                return;
            }

            move.Drag(pointerRay);
            InspectorPanel.Instance.RefreshAllInspectors();
        }

        private void EndLineMove()
        {
            if (extrusion != null)
            {
                var endedExtrusion = extrusion;
                extrusion = null;
                endedExtrusion.End();

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

        // Its sides are built again from the level's data, as loading the level builds them
        public void RegenerateSurfaces()
        {
            foreach (var side in new[] { ClockwiseSide, CounterclockwiseSide })
            {
                if (side)
                {
                    side.PrepareForDestruction();

                    // Destroying waits for the end of the frame, so it's hidden (and out of the pointer's way) now
                    side.gameObject.SetActive(false);
                    Destroy(side.gameObject);
                }
            }

            ClockwiseSide = null;
            CounterclockwiseSide = null;

            GenerateSurfaces();

            foreach (var side in new[] { ClockwiseSide, CounterclockwiseSide })
            {
                if (side)
                {
                    SelectionManager.Instance.MatchSelectabilityToMode(side);
                }
            }
        }
    }
}
#endif
