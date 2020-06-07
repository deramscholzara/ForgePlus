using ForgePlus.Entities.Geometry;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using System.Collections.Generic;
using UnityEngine;
using Weland;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using RuntimeCore.Common;

namespace RuntimeCore.Entities
{
    public class LevelEntity_Level : SingletonMonoBehaviour<LevelEntity_Level>, IDestructionPreparable, ISelectable, IInspectable
    {
        public short Index = -1;
        public Level Level;

        public Dictionary<short, LevelEntity_Polygon> FPPolygons;
        public Dictionary<short, LevelEntity_Line> FPLines;
        public Dictionary<short, LevelEntity_Side> FPSides;
        public Dictionary<short, LevelEntity_Light> FPLights;
        public Dictionary<short, LevelEntity_Media> FPMedias;
        public Dictionary<short, LevelEntity_Platform> FPCeilingFpPlatforms;
        public Dictionary<short, LevelEntity_Platform> FPFloorFpPlatforms;
        public Dictionary<short, LevelEntity_MapObject> FPMapObjects;
        public Dictionary<short, LevelEntity_Annotation> FPAnnotations;

        public List<EditableSurface_Polygon> FPInteractiveSurfacePolygons;
        public List<EditableSurface_Side> FPInteractiveSurfaceSides;
        public List<EditableSurface_Media> FPInteractiveSurfaceMedias;

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - no current reason to toggle this, as it is selected/deselected by switching to/from Level mode.
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<Inspector_Level>("Inspectors/Inspector - Level");
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
