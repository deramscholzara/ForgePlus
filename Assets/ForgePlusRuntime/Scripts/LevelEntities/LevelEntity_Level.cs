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

        public Dictionary<short, LevelEntity_Polygon> Polygons;
        public Dictionary<short, LevelEntity_Line> Lines;
        public Dictionary<short, LevelEntity_Side> Sides;
        public Dictionary<short, LevelEntity_Light> Lights;
        public Dictionary<short, LevelEntity_Media> Medias;
        public Dictionary<short, LevelEntity_Platform> CeilingPlatforms;
        public Dictionary<short, LevelEntity_Platform> FloorPlatforms;
        public Dictionary<short, LevelEntity_MapObject> MapObjects;
        public Dictionary<short, LevelEntity_Annotation> Annotations;

        public List<EditableSurface_Polygon> EditableSurface_Polygons;
        public List<EditableSurface_Side> EditableSurface_Sides;
        public List<EditableSurface_Media> EditableSurface_Medias;

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
            foreach (var light in Lights.Values)
            {
                light.PrepareForDestruction();
            }

            foreach (var media in Medias.Values)
            {
                media.PrepareForDestruction();
            }

            foreach (var platform in CeilingPlatforms.Values)
            {
                platform.PrepareForDestruction();
            }

            foreach (var platform in FloorPlatforms.Values)
            {
                platform.PrepareForDestruction();
            }
        }
    }
}
