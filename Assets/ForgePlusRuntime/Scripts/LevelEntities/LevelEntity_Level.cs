using ForgePlus.Entities.Geometry;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using System.Collections.Generic;
using UnityEngine;
using AlephOne;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using RuntimeCore.Common;

namespace RuntimeCore.Entities
{
    public class LevelEntity_Level : SingletonMonoBehaviour<LevelEntity_Level>, IDestructionPreparable, ISelectable, IInspectable
    {
        public short Index = -1;
        [System.NonSerialized]
        public MapLevel Level;

        [System.NonSerialized]
        public Dictionary<short, LevelEntity_Polygon> Polygons;
        [System.NonSerialized]
        public Dictionary<short, LevelEntity_Line> Lines;
        [System.NonSerialized]
        public Dictionary<short, LevelEntity_Side> Sides;
        // Exposed faces of lines with no side in the level's data (their NativeIndex is NONE)
        [System.NonSerialized]
        public List<LevelEntity_Side> PlaceholderSides;
        [System.NonSerialized]
        public Dictionary<short, LevelEntity_Light> Lights;
        [System.NonSerialized]
        public Dictionary<short, LevelEntity_Media> Medias;
        [System.NonSerialized]
        public Dictionary<short, LevelEntity_Platform> CeilingPlatforms;
        [System.NonSerialized]
        public Dictionary<short, LevelEntity_Platform> FloorPlatforms;
        [System.NonSerialized]
        public Dictionary<short, LevelEntity_MapObject> MapObjects;
        [System.NonSerialized]
        public Dictionary<short, LevelEntity_Annotation> Annotations;

        public List<short>[] EndpointLines;

#if !NO_EDITING
        // Its endpoints, as points to select and move (in Geometry mode)
        [System.NonSerialized]
        public Dictionary<short, LevelEntity_Point> Points;
#endif

        public List<EditableSurface_Polygon> EditableSurface_Polygons;
        public List<EditableSurface_Side> EditableSurface_Sides;
        public List<EditableSurface_Media> EditableSurface_Medias;

        public static string GameObjectName(string levelName)
        {
            return $"Level ({levelName})";
        }

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - no current reason to toggle this, as it is selected/deselected by switching to/from Level mode.
        }

        public void Inspect()
        {
            var inspector = new Inspector_Level(this);
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
