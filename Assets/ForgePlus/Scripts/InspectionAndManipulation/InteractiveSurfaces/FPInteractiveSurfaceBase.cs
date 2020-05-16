using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgePlus.LevelManipulation
{
    public abstract class FPInteractiveSurfaceBase : MonoBehaviour,
        IFPSelectable,
        IPointerClickHandler,
        IBeginDragHandler,
        IEndDragHandler,
        IDragHandler
    {
        protected bool isSelectable = false;

        private CancellationTokenSource inputListenerCancellationTokenSource;

        public abstract void OnValidatedPointerClick(PointerEventData eventData);
        public abstract void OnValidatedBeginDrag(PointerEventData eventData);
        public abstract void OnValidatedDrag(PointerEventData eventData);
        public abstract void OnValidatedEndDrag(PointerEventData eventData);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging && isSelectable)
            {
                OnValidatedPointerClick(eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && isSelectable)
            {
                OnValidatedBeginDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && isSelectable)
            {
                OnValidatedDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && isSelectable)
            {
                OnValidatedEndDrag(eventData);
            }
        }

        public virtual void OnDirectionalInputDown(Vector2 direction)
        {
            // Intentionally blank
        }

        public virtual void SetSelectability(bool enabled)
        {
            isSelectable = enabled;
        }

        protected async void InputListener(IFPSelectable mustBeSelectedObject)
        {
            while (Application.isPlaying && SelectionManager.Instance.GetIsSelected(mustBeSelectedObject))
            {
                var inputDirection = Vector2.zero;
                var directionalInputReceived = false;

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    inputDirection.y += 1f;
                    directionalInputReceived = true;
                }
                
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    inputDirection.y -= 1f;
                    directionalInputReceived = true;
                }

                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    inputDirection.x += 1f;
                    directionalInputReceived = true;
                }
                
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    inputDirection.x -= 1f;
                    directionalInputReceived = true;
                }

                if (directionalInputReceived)
                {
                    OnDirectionalInputDown(inputDirection);
                }

                await Task.Yield();
            }
        }
    }
}
