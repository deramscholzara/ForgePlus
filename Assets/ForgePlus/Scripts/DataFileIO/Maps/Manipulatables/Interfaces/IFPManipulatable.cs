using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public interface IFPManipulatable<T> where T : class
    {
        short? Index { get; set; }
        T WelandObject { get; set; }
        FPLevel FPLevel { set; }
    }
}
