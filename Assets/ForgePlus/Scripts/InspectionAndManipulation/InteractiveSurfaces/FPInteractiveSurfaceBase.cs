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

        public virtual void SetSelectability(bool enabled)
        {
            isSelectable = enabled;
        }
    }
}
