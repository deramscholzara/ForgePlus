using AlephOne;
using RuntimeCore.Entities.Geometry;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_Line : Inspector_Base<LevelEntity_Line>
    {
        public Inspector_Line(LevelEntity_Line line) : base(line)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Line";
            }
        }

        private line_data Line
        {
            get
            {
                return Entity.NativeObject;
            }
        }

        [CreateProperty]
        public string Id
        {
            get
            {
                return Entity.NativeIndex.ToString();
            }
        }

        [CreateProperty]
        public bool Solid
        {
            get
            {
                return map.LINE_IS_SOLID(Line);
            }
        }

        [CreateProperty]
        public bool HasTransparentSide
        {
            get
            {
                return map.LINE_HAS_TRANSPARENT_SIDE(Line);
            }
        }

        [CreateProperty]
        public bool Transparent
        {
            get
            {
                return map.LINE_IS_TRANSPARENT(Line);
            }
        }

        [CreateProperty]
        public bool Landscape
        {
            get
            {
                return map.LINE_IS_LANDSCAPED(Line);
            }
        }

        [CreateProperty]
        public bool VariableElevation
        {
            get
            {
                return map.LINE_IS_VARIABLE_ELEVATION(Line);
            }
        }

        [CreateProperty]
        public bool Elevation
        {
            get
            {
                return map.LINE_IS_ELEVATION(Line);
            }
        }

        [CreateProperty]
        public string Length
        {
            get
            {
                return Line.length.ToString();
            }
        }

        [CreateProperty]
        public string HighestFloorHeight
        {
            get
            {
                return Line.highest_adjacent_floor.ToString();
            }
        }

        [CreateProperty]
        public string LowestCeilingHeight
        {
            get
            {
                return Line.lowest_adjacent_ceiling.ToString();
            }
        }

        [CreateProperty]
        public string ClockwiseSideIndex
        {
            get
            {
                return Line.clockwise_polygon_side_index.ToString();
            }
        }

        [CreateProperty]
        public string ClockwisePolygonIndex
        {
            get
            {
                return Line.clockwise_polygon_owner.ToString();
            }
        }

        [CreateProperty]
        public string CounterclockwiseSideIndex
        {
            get
            {
                return Line.counterclockwise_polygon_side_index.ToString();
            }
        }

        [CreateProperty]
        public string CounterclockwisePolygonIndex
        {
            get
            {
                return Line.counterclockwise_polygon_owner.ToString();
            }
        }
    }
}
