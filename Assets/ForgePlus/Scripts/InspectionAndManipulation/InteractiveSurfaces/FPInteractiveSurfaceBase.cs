using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgePlus.LevelManipulation
{
    public abstract class FPInteractiveSurfaceBase : MonoBehaviour, IFPSelectable, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        protected bool isSelectable = false;

        public void OnBeginDrag(PointerEventData eventData)
        {
            eventData.dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Intentionally blank - this is only here because IDragHandler must be implemented for Begin and End Drag events to fire.
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            eventData.dragging = false;
        }

        public abstract void OnPointerClick(PointerEventData eventData);

        public virtual void SetSelectability(bool enabled)
        {
            isSelectable = enabled;
        }
    }
}
