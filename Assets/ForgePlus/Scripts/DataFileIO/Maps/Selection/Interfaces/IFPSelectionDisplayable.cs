using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public interface IFPSelectionDisplayable : IFPSelectable
    {
        void DisplaySelectionState(bool state);
    }
}
