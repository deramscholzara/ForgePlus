using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public interface IFPSelectable
    {
        void SetSelectability(bool enabled);

        void DisplaySelectionState(bool state);
    }
}
