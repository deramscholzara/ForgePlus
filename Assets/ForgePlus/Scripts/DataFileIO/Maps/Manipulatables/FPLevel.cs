using System.Collections.Generic;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPLevel : SingletonMonoBehaviour<FPLevel>, IFPDestructionPreparable
    {
        public short Index = -1;
        public Level Level;

        public Dictionary<short, FPPolygon> FPPolygons;
        public Dictionary<short, FPLine> FPLines;
        public Dictionary<short, FPSide> FPSides;
        public Dictionary<short, FPLight> FPLights;
        public Dictionary<short, FPMedia> FPMedias;
        public Dictionary<short, FPPlatform> FPCeilingFpPlatforms;
        public Dictionary<short, FPPlatform> FPFloorFpPlatforms;
        public Dictionary<short, FPMapObject> FPMapObjects;

        public List<FPSurfacePolygon> FPSurfacePolygons;
        public List<FPSurfaceSide> FPSurfaceSides;
        public List<FPSurfaceMedia> FPSurfaceMedias;

        public void PrepareForDestruction()
        {
            foreach (var fpLight in FPLights.Values)
            {
                fpLight.PrepareForDestruction();
            }

            foreach (var fpMedia in FPMedias.Values)
            {
                fpMedia.PrepareForDestruction();
            }

            foreach (var fpPlatform in FPCeilingFpPlatforms.Values)
            {
                fpPlatform.PrepareForDestruction();
            }

            foreach (var fpPlatform in FPFloorFpPlatforms.Values)
            {
                fpPlatform.PrepareForDestruction();
            }
        }
    }
}
