using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Runtime.Constraints;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public abstract class FPInteractiveSurfaceBase : MonoBehaviour, IFPSelectable
    {
        protected bool isSelectable = false;

        public abstract void OnMouseUpAsButton();

        public virtual void SetSelectability(bool enabled)
        {
            isSelectable = enabled;
        }
    }
}
