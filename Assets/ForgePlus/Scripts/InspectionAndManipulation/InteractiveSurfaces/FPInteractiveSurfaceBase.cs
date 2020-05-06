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

        public abstract void OnPointerClickValidated(PointerEventData eventData);
        public abstract void OnBeginDragValidated(PointerEventData eventData);
        public abstract void OnDragValidated(PointerEventData eventData);
        public abstract void OnEndDragValidated(PointerEventData eventData);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerId == -1 && !eventData.dragging && isSelectable)
            {
                OnPointerClickValidated(eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && isSelectable)
            {
                OnBeginDragValidated(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && isSelectable)
            {
                OnDragValidated(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && isSelectable)
            {
                OnEndDragValidated(eventData);
            }
        }

        public virtual void SetSelectability(bool enabled)
        {
            isSelectable = enabled;
        }
    }
}
