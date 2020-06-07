using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public interface ISelectionDisplayable : ISelectable
    {
        void DisplaySelectionState(bool state);
    }
}
