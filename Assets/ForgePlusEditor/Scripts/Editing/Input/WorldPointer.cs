using ForgePlus.ApplicationGeneral;
using ForgePlus.Entities.Geometry;
using ForgePlus.UI;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgePlus.LevelManipulation
{
    // What the pointer did to something in the level (for IWorldPointerHandler)
    public class WorldPointerEventData
    {
        // Screen position (pixels, from the bottom left), and where it was pressed
        public Vector2 Position;
        public Vector2 PressPosition;

        // Where on its collider the pointer was pressed, and the collider's normal there
        public Vector3 PressWorldPosition;
        public Vector3 PressWorldNormal;

        public bool IsDragging;
    }

    // Something in the level (with a collider, on it or a parent) that the pointer can click and drag
    public interface IWorldPointerHandler
    {
        void OnWorldPointerClick(WorldPointerEventData eventData);
        void OnWorldPointerBeginDrag(WorldPointerEventData eventData);
        void OnWorldPointerDrag(WorldPointerEventData eventData);
        void OnWorldPointerEndDrag(WorldPointerEventData eventData);
    }

    // Something that also takes double-clicks: a second click on it, soon after and near the first, comes here instead of
    // to OnWorldPointerClick (so the first click's selection stays)
    public interface IWorldPointerDoubleClickHandler
    {
        void OnWorldPointerDoubleClick(WorldPointerEventData eventData);
    }

    // Something in the level that counts as empty space for a drag from empty space (EmptySpaceDragger), such as a preview
    // of a polygon that isn't there yet, while still taking its own clicks
    public interface IWorldPointerEmptySpace
    {
    }

    // Something drawn over the level on screen (such as points' handles), which takes presses before the level's colliders
    public interface IScreenPointerPicker
    {
        // The handler drawn at a screen position (pixels, from the bottom left), and where in the level it's drawn
        bool TryPick(Vector2 screenPosition, out IWorldPointerHandler handler, out Vector3 worldPosition);
    }

    // Clicks and drags (with the Select input) on what's drawn over the level (ScreenPicker), or else on the level's
    // colliders, for the handler under the pointer when it was pressed (presses over the UI are the UI's). A drag starts
    // once the pointer moves far enough from the press. With the Select tool (in the modes it selects in), a drag from
    // anywhere not already selected selects by rectangle instead (RectangleSelection), so only what's selected is dragged.
    // A drag from empty space just after a click there (a press, release and press, as a double-click's) goes to the
    // handler the EmptySpaceDragger gives, if it gives one; let go without dragging, it's an ordinary click.
    public class WorldPointer : SingletonMonoBehaviour<WorldPointer>
    {
        // Pixels
        private const float DragThreshold = 10f;

        // A second click within this long (seconds) and this near (pixels) of the first, on the same thing, is a double-click
        private const float DoubleClickSeconds = 0.4f;
        private const float DoubleClickDistance = 6f;

        private IWorldPointerHandler lastClickedHandler;
        private float lastClickTime;
        private Vector2 lastClickPosition;

        // The last click on empty space, and the handler (if any) a drag from the press after it would go to
        private float lastEmptyClickTime = float.NegativeInfinity;
        private Vector2 lastEmptyClickPosition;
        private bool isSecondEmptyPress;
        private IWorldPointerHandler emptySpaceDragHandler;

        // Pressing and releasing over nothing (no UI, and nothing in the level)
        public event Action OnClickEmptySpace;

        // What handles a drag from empty space just after a click there (given where it's pressed), or null for nothing
        public Func<Vector2, IWorldPointerHandler> EmptySpaceDragger { get; set; }

        // The camera the level is seen through, and the UI whose presses aren't the level's
        [SerializeField]
        private Camera mainCamera = null;

        [SerializeField]
        private ForgePlusUI ui = null;

        // The level, and the selection indicators (and selected objects) drawn on their own layer
        [SerializeField]
        private LayerMask raycastLayers = default;

        private bool isPressed;
        private bool pressStartedOverEmptiness;

        // A drag from this press would select by rectangle, and is (where the pointer last was, and whether shift was held)
        private bool pressCanSelectByRectangle;
        private bool isSelectingByRectangle;
        private Vector2 rectangleEndPosition;
        private bool rectangleAddsToSelection;

        // Escape cancels selecting by rectangle (rather than toggling the menu, which checks this)
        public bool IsSelectingByRectangle
        {
            get
            {
                return isSelectingByRectangle;
            }
        }

        public int RectangleCancelledFrame { get; private set; } = -1;
        private IWorldPointerHandler pressedHandler;
        private WorldPointerEventData eventData;
        private Vector2 pressPosition;

        public IScreenPointerPicker ScreenPicker { get; set; }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            var position = pointer.position.ReadValue();

            // Pressing while holding Pan Modifier (space) pans the camera instead (EditorCamera)
            if (ForgePlusInput.Editing.Select.WasPressedThisFrame() && !Hotkeys.IsPressed(ForgePlusInput.Camera.PanModifier))
            {
                Press(position);
            }

            if (!isPressed)
            {
                return;
            }

            if (isSelectingByRectangle)
            {
                if (Hotkeys.WasPressed(ForgePlusInput.Editing.CancelSelectionBox))
                {
                    // Nothing more comes of this press
                    isSelectingByRectangle = false;
                    pressCanSelectByRectangle = false;
                    pressedHandler = null;
                    pressStartedOverEmptiness = false;
                    RectangleCancelledFrame = Time.frameCount;

                    ui.RectangleSelection.Cancel();
                    return;
                }

                // What it selects is worked out again as it (or Add to Selection) changes
                var addsToSelection = Hotkeys.IsPressed(ForgePlusInput.Editing.AddToSelection);
                if (position != rectangleEndPosition || addsToSelection != rectangleAddsToSelection)
                {
                    rectangleEndPosition = position;
                    rectangleAddsToSelection = addsToSelection;
                    ui.RectangleSelection.Drag(position, addsToSelection);
                }
            }
            else if (pressCanSelectByRectangle && (position - pressPosition).sqrMagnitude >= DragThreshold * DragThreshold)
            {
                isSelectingByRectangle = true;
                rectangleEndPosition = position;
                rectangleAddsToSelection = Hotkeys.IsPressed(ForgePlusInput.Editing.AddToSelection);
                ui.RectangleSelection.Begin(pressPosition);
                ui.RectangleSelection.Drag(position, rectangleAddsToSelection);
            }
            else if (emptySpaceDragHandler != null && (position - pressPosition).sqrMagnitude >= DragThreshold * DragThreshold)
            {
                // Whatever was pressed (if anything) counts as empty space, and the drag is the empty space's
                pressedHandler = emptySpaceDragHandler;
                emptySpaceDragHandler = null;

                eventData = new WorldPointerEventData
                {
                    Position = position,
                    PressPosition = pressPosition,
                    IsDragging = true,
                };

                pressedHandler.OnWorldPointerBeginDrag(eventData);
            }
            else if (pressedHandler != null)
            {
                var hasMoved = position != eventData.Position;
                eventData.Position = position;

                if (!eventData.IsDragging && (position - pressPosition).sqrMagnitude >= DragThreshold * DragThreshold)
                {
                    eventData.IsDragging = true;
                    pressedHandler.OnWorldPointerBeginDrag(eventData);
                }
                else if (eventData.IsDragging && hasMoved)
                {
                    pressedHandler.OnWorldPointerDrag(eventData);
                }
            }

            if (ForgePlusInput.Editing.Select.WasReleasedThisFrame())
            {
                Release(position);
            }
        }

        private void Press(Vector2 position)
        {
            isPressed = true;
            pressedHandler = null;
            pressStartedOverEmptiness = false;
            pressCanSelectByRectangle = false;
            isSelectingByRectangle = false;
            pressPosition = position;
            emptySpaceDragHandler = null;

            // Just after a click on empty space, and near it (once only)
            isSecondEmptyPress = Time.unscaledTime - lastEmptyClickTime <= DoubleClickSeconds &&
                                 (position - lastEmptyClickPosition).sqrMagnitude <= DoubleClickDistance * DoubleClickDistance;
            if (isSecondEmptyPress)
            {
                lastEmptyClickTime = float.NegativeInfinity;
            }

            if (IsOverUI(position))
            {
                return;
            }

            if (!TryGetHandler(position, out pressedHandler, out var worldPosition, out var worldNormal))
            {
                pressStartedOverEmptiness = true;
                pressCanSelectByRectangle = RectangleSelection.IsAvailable;

                if (isSecondEmptyPress && EmptySpaceDragger != null)
                {
                    emptySpaceDragHandler = EmptySpaceDragger(position);
                    pressCanSelectByRectangle &= emptySpaceDragHandler == null;
                }

                return;
            }

            pressCanSelectByRectangle = RectangleSelection.IsAvailable && !IsSelected(pressedHandler);

            if (isSecondEmptyPress && pressedHandler is IWorldPointerEmptySpace && EmptySpaceDragger != null)
            {
                emptySpaceDragHandler = EmptySpaceDragger(position);
                pressCanSelectByRectangle &= emptySpaceDragHandler == null;
            }

            eventData = new WorldPointerEventData
            {
                Position = position,
                PressPosition = position,
                PressWorldPosition = worldPosition,
                PressWorldNormal = worldNormal,
            };
        }

        private void Release(Vector2 position)
        {
            isPressed = false;

            if (isSelectingByRectangle)
            {
                isSelectingByRectangle = false;
                pressedHandler = null;

                // Add to Selection (Shift) adds to the selection (as it was before the drag), rather than replacing it
                ui.RectangleSelection.End(position, addsToSelection: Hotkeys.IsPressed(ForgePlusInput.Editing.AddToSelection));
                return;
            }

            if (pressedHandler != null)
            {
                if (eventData.IsDragging)
                {
                    pressedHandler.OnWorldPointerEndDrag(eventData);
                }
                else if (!IsOverUI(position) &&
                         TryGetHandler(position, out var releasedHandler, out _, out _) &&
                         releasedHandler == pressedHandler)
                {
                    var isDoubleClick = pressedHandler == lastClickedHandler &&
                                        Time.unscaledTime - lastClickTime <= DoubleClickSeconds &&
                                        (position - lastClickPosition).sqrMagnitude <= DoubleClickDistance * DoubleClickDistance;

                    if (isDoubleClick && pressedHandler is IWorldPointerDoubleClickHandler doubleClickHandler)
                    {
                        // A third click starts again
                        lastClickedHandler = null;
                        doubleClickHandler.OnWorldPointerDoubleClick(eventData);
                    }
                    else
                    {
                        lastClickedHandler = pressedHandler;
                        lastClickTime = Time.unscaledTime;
                        lastClickPosition = position;

                        if (pressedHandler is IWorldPointerEmptySpace)
                        {
                            RecordEmptyClick(position);
                        }

                        pressedHandler.OnWorldPointerClick(eventData);
                    }
                }

                pressedHandler = null;
            }
            else if (pressStartedOverEmptiness && !IsOverUI(position) && !TryGetHandler(position, out _, out _, out _))
            {
                RecordEmptyClick(position);
                OnClickEmptySpace?.Invoke();
            }

            emptySpaceDragHandler = null;
        }

        // Not the second click of a press, release and press (which starts again)
        private void RecordEmptyClick(Vector2 position)
        {
            if (!isSecondEmptyPress)
            {
                lastEmptyClickTime = Time.unscaledTime;
                lastEmptyClickPosition = position;
            }
        }

        // What's drawn over the level comes first, then the level's colliders. Something (a collider) can be hit without
        // having a handler (except in Objects mode, where only what can be clicked counts).
        private bool TryGetHandler(Vector2 position, out IWorldPointerHandler handler, out Vector3 worldPosition, out Vector3 worldNormal)
        {
            if (ScreenPicker != null && ScreenPicker.TryPick(position, out handler, out worldPosition))
            {
                worldNormal = Vector3.up;
                return true;
            }

            var ray = mainCamera.ScreenPointToRay(position);

            // In Objects mode, clicks pass through the level's geometry (which it doesn't select) to the objects beyond
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Objects)
            {
                var hits = Physics.RaycastAll(ray, mainCamera.farClipPlane, raycastLayers);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var objectHit in hits)
                {
                    handler = objectHit.collider.GetComponentInParent<IWorldPointerHandler>();
                    if (handler == null || (handler is EditableSurface_Base surface && !surface.IsSelectable))
                    {
                        continue;
                    }

                    worldPosition = objectHit.point;
                    worldNormal = objectHit.normal;
                    return true;
                }

                handler = null;
                worldPosition = default;
                worldNormal = default;
                return false;
            }

            if (Physics.Raycast(ray, out var hit, mainCamera.farClipPlane, raycastLayers))
            {
                handler = hit.collider.GetComponentInParent<IWorldPointerHandler>();
                worldPosition = hit.point;
                worldNormal = hit.normal;
                return true;
            }

            handler = null;
            worldPosition = default;
            worldNormal = default;
            return false;
        }

        // Whether what it's for is selected (a surface is its polygon's, side's or platform's)
        private static bool IsSelected(IWorldPointerHandler handler)
        {
            var selectionManager = SelectionManager.Instance;

            switch (handler)
            {
                case EditableSurface_Polygon polygonSurface:
                    return selectionManager.GetIsSelected(polygonSurface.ParentPolygon) ||
                           (polygonSurface.Platform && selectionManager.GetIsSelected(polygonSurface.Platform.SelectablePlatform));
                case EditableSurface_Side sideSurface:
                    return selectionManager.GetIsSelected(sideSurface.ParentSide) ||
                           (sideSurface.Platform && selectionManager.GetIsSelected(sideSurface.Platform.SelectablePlatform));
                case EditableSurface_Media mediaSurface:
                    return mediaSurface.Polygon && selectionManager.GetIsSelected(mediaSurface.Polygon);
                case ISelectable selectable:
                    return selectionManager.GetIsSelected(selectable);
                default:
                    return false;
            }
        }

        private bool IsOverUI(Vector2 position)
        {
            return ui.IsPointerOverUI(position);
        }
    }
}
