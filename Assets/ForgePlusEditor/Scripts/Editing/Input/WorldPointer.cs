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

    // Clicks and drags (with the Select input) on the level's colliders, for the handler under the pointer when it was
    // pressed (presses over the UI are the UI's). A drag starts once the pointer moves far enough from the press.
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

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            var position = pointer.position.ReadValue();

            if (ForgePlusInput.Editing.Select.WasPressedThisFrame())
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

            if (!Raycast(position, out var hit))
            {
                pressStartedOverEmptiness = true;
                return;
            }

            pressedHandler = hit.collider.GetComponentInParent<IWorldPointerHandler>();
            eventData = new WorldPointerEventData
            {
                Position = position,
                PressWorldPosition = hit.point,
                PressWorldNormal = hit.normal,
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
                         Raycast(position, out var hit) &&
                         hit.collider.GetComponentInParent<IWorldPointerHandler>() == pressedHandler)
                {
                    pressedHandler.OnWorldPointerClick(eventData);
                }

                pressedHandler = null;
            }
            else if (pressStartedOverEmptiness && !IsOverUI(position) && !Raycast(position, out _))
            {
                OnClickEmptySpace?.Invoke();
            }
        }

        private bool Raycast(Vector2 position, out RaycastHit hit)
        {
            return Physics.Raycast(mainCamera.ScreenPointToRay(position), out hit, mainCamera.farClipPlane, raycastLayers);
        }

        private bool IsOverUI(Vector2 position)
        {
            return ui.IsPointerOverUI(position);
        }
    }
}
