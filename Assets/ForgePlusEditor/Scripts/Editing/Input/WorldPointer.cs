using ForgePlus.ApplicationGeneral;
using ForgePlus.UI;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgePlus.LevelManipulation
{
    // What the pointer did to something in the level (for IWorldPointerHandler)
    public class WorldPointerEventData
    {
        // Screen position (pixels, from the bottom left)
        public Vector2 Position;

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

    // Something drawn over the level on screen (such as points' handles), which takes presses before the level's colliders
    public interface IScreenPointerPicker
    {
        // The handler drawn at a screen position (pixels, from the bottom left), and where in the level it's drawn
        bool TryPick(Vector2 screenPosition, out IWorldPointerHandler handler, out Vector3 worldPosition);
    }

    // Clicks and drags (with the Select input) on what's drawn over the level (ScreenPicker), or else on the level's
    // colliders, for the handler under the pointer when it was pressed (presses over the UI are the UI's). A drag starts
    // once the pointer moves far enough from the press.
    public class WorldPointer : SingletonMonoBehaviour<WorldPointer>
    {
        // Pixels
        private const float DragThreshold = 10f;

        // Pressing and releasing over nothing (no UI, and nothing in the level)
        public event Action OnClickEmptySpace;

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

            if (pressedHandler != null)
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
            pressPosition = position;

            if (IsOverUI(position))
            {
                return;
            }

            if (!TryGetHandler(position, out pressedHandler, out var worldPosition, out var worldNormal))
            {
                pressStartedOverEmptiness = true;
                return;
            }

            eventData = new WorldPointerEventData
            {
                Position = position,
                PressWorldPosition = worldPosition,
                PressWorldNormal = worldNormal,
            };
        }

        private void Release(Vector2 position)
        {
            isPressed = false;

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
                    pressedHandler.OnWorldPointerClick(eventData);
                }

                pressedHandler = null;
            }
            else if (pressStartedOverEmptiness && !IsOverUI(position) && !TryGetHandler(position, out _, out _, out _))
            {
                OnClickEmptySpace?.Invoke();
            }
        }

        // What's drawn over the level comes first, then the level's colliders. Something (a collider) can be hit without
        // having a handler.
        private bool TryGetHandler(Vector2 position, out IWorldPointerHandler handler, out Vector3 worldPosition, out Vector3 worldNormal)
        {
            if (ScreenPicker != null && ScreenPicker.TryPick(position, out handler, out worldPosition))
            {
                worldNormal = Vector3.up;
                return true;
            }

            if (Physics.Raycast(mainCamera.ScreenPointToRay(position), out var hit, mainCamera.farClipPlane, raycastLayers))
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

        private bool IsOverUI(Vector2 position)
        {
            return ui.IsPointerOverUI(position);
        }
    }
}
