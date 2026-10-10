#if !NO_EDITING
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    public partial class LevelEntity_Line
    {
        private PointsMove move;

        private void ClickLine()
        {
            SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false);
            InspectorPanel.Instance.RefreshAllInspectors();
        }

        // In Select mode, dragging it moves both its ends together across the level (at the height of the line it was
        // grabbed by), recorded as one action when it ends
        private void BeginLineMove(WorldPointerEventData eventData)
        {
            if (ModeManager.Instance.SecondaryMode != ModeManager.SecondaryModes.Selection)
            {
                return;
            }

            SelectionManager.Instance.SelectObject(this, multiSelect: false);
            move = PointsMove.ForLine(LevelEntity_Level.Instance, this, eventData.PressWorldPosition);
        }

        private void DragLineMove(WorldPointerEventData eventData)
        {
            if (move == null)
            {
                return;
            }

            move.Drag(Camera.main.ScreenPointToRay(new Vector3(eventData.Position.x, eventData.Position.y, 0f)));
            InspectorPanel.Instance.RefreshAllInspectors();
        }

        private void EndLineMove()
        {
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
