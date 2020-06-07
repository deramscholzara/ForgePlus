using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public interface IManipulatable<T> where T : class
    {
        short NativeIndex { get; }
        T NativeObject { get; }
    }
}
