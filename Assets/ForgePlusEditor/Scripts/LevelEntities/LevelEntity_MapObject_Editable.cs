#if !NO_EDITING
using AlephOne;
using ForgePlus.History;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using UnityEngine;

namespace RuntimeCore.Entities.MapObjects
{
    public partial class LevelEntity_MapObject
    {
        private ObjectDrag drag;

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
            level.MapIndexList.Clear();
            map_constructors.precalculate_map_indexes(level);
        }

        // In Select mode, dragging it moves it (recorded as one action when it ends)
        private void BeginMoveDrag(WorldPointerEventData eventData)
        {
            if (ModeManager.Instance.SecondaryMode != ModeManager.SecondaryModes.Selection)
            {
                return;
            }

            SelectionManager.Instance.SelectObject(this, multiSelect: false);

            drag = new ObjectDrag(this, eventData.PressWorldPosition);
            LevelHistory.BeginGesture();
        }

        private void MoveDrag(WorldPointerEventData eventData)
        {
            if (drag == null)
            {
                return;
            }

            drag.Drag(Camera.main.ScreenPointToRay(new Vector3(eventData.Position.x, eventData.Position.y, 0f)));

            Inspector_Base.RefreshInspectorsOf(this);
        }

        // Which polygons hear a sound source is only worked out once it's put down
        private void EndMoveDrag()
        {
            if (drag == null)
            {
                return;
            }

            drag = null;

            if (NativeObject.type == map._saved_sound_source)
            {
                RecalculateHeardSoundSources(Level);
            }

            Inspector_Base.RefreshInspectorsOf(this);
            LevelHistory.EndGesture();
        }
    }
}
#endif
