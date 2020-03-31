using ForgePlus.LevelManipulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.Inspection
{
    public abstract class InspectorBase : MonoBehaviour
    {
        public abstract void PopulateValues(IFPInspectable inspectedObject);
    }
}
