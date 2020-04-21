using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.Inspection
{
    public interface IFPInspectable
    {
        // TODO: Add this so it must be implemented in all inspectables
        ////event Action<T> InspectablePropertiesChanged;

        void Inspect();
    }
}
