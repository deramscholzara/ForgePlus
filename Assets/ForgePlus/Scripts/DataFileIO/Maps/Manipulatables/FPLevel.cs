using ForgePlus.Inspection;
using System.Collections.Generic;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPLevel : SingletonMonoBehaviour<FPLevel>, IFPDestructionPreparable, IFPSelectable, IFPInspectable
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

        public List<FPInteractiveSurfacePolygon> FPInteractiveSurfacePolygons;
        public List<FPInteractiveSurfaceSide> FPInteractiveSurfaceSides;
        public List<FPInteractiveSurfaceMedia> FPInteractiveSurfaceMedias;

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - no current reason to toggle this, as it is selected/deselected by switching to/from Level mode.
        }

        public void DisplaySelectionState(bool state)
        {
            // Intentially blank - no real need to show the selection state of the level, as it's apparent enough from it showing up in the inspector
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<InspectorFPLevel>("Inspectors/Inspector - FPLevel");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

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
