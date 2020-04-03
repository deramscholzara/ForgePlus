using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public interface IFPSelectable
    {
        // TODO: Set up visibility filtering (static state member+enum per relevant type?)
        // TODO: Replace with custom raycast receptor, which should be in a separate interface - like IClickable
        void OnMouseUpAsButton();

        void SetSelectability(bool enabled);

        void DisplaySelectionState(bool state);
    }
}
