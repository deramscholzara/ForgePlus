using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using RuntimeCore.Entities.Geometry;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    // A face of a line that's drawn (its polygons' heights expose it) but has no side in the level's data, so it has no
    // texture, light or anything else a side has: the line it's on, and the polygons it's between
    public class Inspector_PlaceholderSide : Inspector_Base<LevelEntity_Side>
    {
        public Inspector_PlaceholderSide(LevelEntity_Side side) : base(side)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Placeholder Side";
            }
        }

        private line_data Line
        {
            get
            {
                return map.get_line_data(Entity.ParentLevel.Level, Entity.ParentLineIndex);
            }
        }

        [CreateProperty]
        public string LineIndex
        {
            get
            {
                return Entity.ParentLineIndex.ToString();
            }
        }

        [CreateProperty]
        public string Face
        {
            get
            {
                return Entity.IsClockwise ?
                       Strings.Get(Strings.Common, "Inspector.PlaceholderSide.Face.Clockwise") :
                       Strings.Get(Strings.Common, "Inspector.PlaceholderSide.Face.Counterclockwise");
            }
        }

        [CreateProperty]
        public string FacingPolygon
        {
            get
            {
                return PolygonDescription(Line.GetPolygonOwner(Entity.IsClockwise));
            }
        }

        [CreateProperty]
        public string OpposingPolygon
        {
            get
            {
                return PolygonDescription(Line.GetPolygonOwner(!Entity.IsClockwise));
            }
        }

        private string PolygonDescription(short polygonIndex)
        {
            if (polygonIndex < 0)
            {
                return Strings.Get(Strings.Common, "Inspector.PlaceholderSide.PolygonNone");
            }

            var polygon = map.get_polygon_data(Entity.ParentLevel.Level, polygonIndex);

            return polygon != null && polygon.type == map._polygon_is_platform ?
                   Strings.Get(Strings.Common, "Inspector.PlaceholderSide.PlatformPolygon", polygonIndex, polygon.permutation) :
                   polygonIndex.ToString();
        }
    }
}
