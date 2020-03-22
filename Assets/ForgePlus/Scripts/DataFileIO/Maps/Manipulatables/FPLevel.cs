using System.Collections.Generic;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPLevel : MonoBehaviour
    {
        public short Index = -1;
        public Level Level;

        public Dictionary<short, FPPolygon> FPPolygons;
        public Dictionary<short, FPLine> FPLines;
        public Dictionary<short, FPSide> FPSides;
        public Dictionary<short, FPPlatform> FPCeilingFpPlatforms;
        public Dictionary<short, FPPlatform> FPFloorFpPlatforms;
        public Dictionary<short, FPMapObject> FPMapObjects;
    }
}
