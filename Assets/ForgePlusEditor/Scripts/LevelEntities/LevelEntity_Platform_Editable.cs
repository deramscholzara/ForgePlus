#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    public partial class LevelEntity_Platform : ISelectionDisplayable
    {
        private PlatformSelectionIndicators selectionIndicators;

        public void DisplaySelectionState(bool state)
        {
            if (state)
            {
                if (!selectionIndicators)
                {
                    selectionIndicators = gameObject.AddComponent<PlatformSelectionIndicators>();
                    selectionIndicators.Initialize(this);
                }
            }
            else if (selectionIndicators)
            {
                Destroy(selectionIndicators);
                selectionIndicators = null;
            }
        }

        // Both halves of a platform that goes both ways, since they're one Aleph One platform,
        // and the sides facing it (its own sides don't move)
        public void GetMovingSurfaces(out MeshFilter floorSurface, out MeshFilter ceilingSurface, out List<PlatformSideClipping> sides)
        {
            floorSurface = ParentLevel.FloorPlatforms.TryGetValue(NativeIndex, out var floorPlatform) ? floorPlatform.GetComponent<MeshFilter>() : null;
            ceilingSurface = ParentLevel.CeilingPlatforms.TryGetValue(NativeIndex, out var ceilingPlatform) ? ceilingPlatform.GetComponent<MeshFilter>() : null;

            sides = new List<PlatformSideClipping>();

            foreach (var editableSide in ParentLevel.EditableSurface_Sides)
            {
                var sideClipping = editableSide.GetComponent<PlatformSideClipping>();
                if (!sideClipping)
                {
                    continue;
                }

                var side = editableSide.ParentSide;
                var line = map.get_line_data(ParentLevel.Level, side.ParentLineIndex);

                if (line.GetPolygonOwner(!side.IsClockwise) == NativeObject.polygon_index)
                {
                    sides.Add(sideClipping);
                }
            }
        }
    }
}
#endif
