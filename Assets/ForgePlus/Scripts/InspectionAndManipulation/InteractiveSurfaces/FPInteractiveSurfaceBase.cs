using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgePlus.LevelManipulation
{
    public abstract class FPInteractiveSurfaceBase : MonoBehaviour, IFPSelectable, IPointerClickHandler
    {
        protected bool isSelectable = false;

        public abstract void OnPointerClick(PointerEventData eventData);

        public virtual void SetSelectability(bool enabled)
        {
            isSelectable = enabled;
        }
    }
}
