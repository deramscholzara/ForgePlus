using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPLine : MonoBehaviour, IFPManipulatable<Line>
    {
        public short? Index { get; set; }
        public Line WelandObject { get; set; }
        public FPSide ClockwiseSide;
        public FPSide CounterclockwiseSide;

        public FPLevel FPLevel { private get; set; }

        public void GenerateSurfaces()
        {
            ClockwiseSide = FPSide.GenerateSurfaces(FPLevel, isClockwise: true, WelandObject);
            if (ClockwiseSide)
            {
                ClockwiseSide.transform.SetParent(transform);
            }

            CounterclockwiseSide = FPSide.GenerateSurfaces(FPLevel, isClockwise: false, WelandObject);
            if (CounterclockwiseSide)
            {
                CounterclockwiseSide.transform.SetParent(transform);
            }
        }
    }
}
