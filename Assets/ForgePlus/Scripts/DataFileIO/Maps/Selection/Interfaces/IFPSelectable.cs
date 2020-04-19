using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public interface IFPSelectable
    {
        // TODO: Set up visibility filtering (static state member+enum per relevant type?)
        void SetSelectability(bool enabled);
    }
}
