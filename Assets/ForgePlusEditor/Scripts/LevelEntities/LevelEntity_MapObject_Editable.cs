#if !NO_EDITING
using AlephOne;
using ForgePlus.History;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RuntimeCore.Entities.MapObjects
{
    public partial class LevelEntity_MapObject
    {
        // Its own, and the rest of the selection's
        private List<(LevelEntity_MapObject MapObject, ObjectDrag Drag)> drags;

        public MapLevel Level
        {
            get
            {
                return ParentLevel.Level;
            }
        }

        // Its height (z) is above the polygon's floor (or below its ceiling, for one hanging from it)
        public void MoveTo(world_point2d location, short polygonIndex, short z)
        {
            NativeObject.location.x = location.x;
            NativeObject.location.y = location.y;
            NativeObject.location.z = z;
            NativeObject.polygon_index = polygonIndex;

            ApplyPlacement();
        }

        // A sound source is heard from the polygons near it, which each polygon lists with the level's other map indexes
        // (map_constructors.cpp: precalculate_polygon_sound_sources)
        public static void RecalculateHeardSoundSources(MapLevel level)
        {
            LevelEditing.RecalculateMapIndexes(level);
        }

        // In Select mode, dragging it moves it, along with the rest of the selection's objects if it's selected (each as far
        // across the level, into whichever polygon it's then over), recorded as one action when it ends
        private void BeginMoveDrag(WorldPointerEventData eventData)
        {
            if (ModeManager.Instance.SecondaryMode != ModeManager.SecondaryModes.Selection)
            {
                return;
            }

            SelectionManager.Instance.SelectObject(this, multiSelect: false);

            drags = SelectionManager.Instance.Selection.OfType<LevelEntity_MapObject>()
                                                       .Select(mapObject => (mapObject, new ObjectDrag(mapObject, eventData.PressWorldPosition)))
                                                       .ToList();
            LevelHistory.BeginGesture();
        }

        private void MoveDrag(WorldPointerEventData eventData)
        {
            if (drags == null)
            {
                return;
            }

            var pointerRay = Camera.main.ScreenPointToRay(new Vector3(eventData.Position.x, eventData.Position.y, 0f));
            foreach (var (_, objectDrag) in drags)
            {
                objectDrag.Drag(pointerRay);
            }

            Inspector_Base.RefreshInspectorsOf(this);
        }

        // Which polygons hear a sound source is only worked out once it's put down
        private void EndMoveDrag()
        {
            if (drags == null)
            {
                return;
            }

            var moved = drags;
            drags = null;

            if (moved.Any(entry => entry.MapObject && entry.MapObject.NativeObject.type == map._saved_sound_source))
            {
                RecalculateHeardSoundSources(Level);
            }

            Inspector_Base.RefreshInspectorsOf(this);
            LevelHistory.EndGesture();
        }
    }
}
#endif
