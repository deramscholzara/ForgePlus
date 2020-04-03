using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Runtime.Constraints;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public abstract class SurfaceBase : MonoBehaviour, IFPSelectable
    {
        protected bool isSelectable = false;

        public abstract void OnMouseUpAsButton();

        public virtual void SetSelectability(bool enabled)
        {
            isSelectable = enabled;
        }

        public void DisplaySelectionState(bool state)
        {
            Debug.LogError("FPSurface components should never be directly displayed, so this request will be ignored.", this);
        }
    }
}
