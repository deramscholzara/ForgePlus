using AlephOne;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using RuntimeCore.Entities;
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

        // Between two polygons, a solid line is an invisible wall (it's still transparent)
        [CreateProperty]
        public bool Solid
        {
            get
            {
                return map.LINE_IS_SOLID(Line);
            }
            set
            {
                if (value != Solid)
                {
                    Edit(line => LineFlagsEditing.SetSolidity(LevelEntity_Level.Instance.Level, line.NativeIndex, value));
                }
            }
        }

        // A wall (with a polygon on one side only) is always solid, and a platform sets whether a line beside it is as it
        // moves (its variable elevation)
        [CreateProperty]
        public bool IsSolidEditable
        {
            get
            {
                return IsBetweenPolygons && !map.LINE_IS_VARIABLE_ELEVATION(Line);
            }
        }

        [CreateProperty]
        public string SolidCaption
        {
            get
            {
                if (!IsBetweenPolygons)
                {
                    return Strings.Get(Strings.Geometry, "Inspector.Line.SolidCaption.Wall");
                }

                if (map.LINE_IS_VARIABLE_ELEVATION(Line))
                {
                    return Strings.Get(Strings.Geometry, "Inspector.Line.SolidCaption.Platform");
                }

                return Solid ? Strings.Get(Strings.Geometry, "Inspector.Line.SolidCaption.InvisibleWall") : string.Empty;
            }
        }

        private bool IsBetweenPolygons
        {
            get
            {
                return Line.clockwise_polygon_owner != cstypes.NONE && Line.counterclockwise_polygon_owner != cstypes.NONE;
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

        // Aleph One only: projectiles pass through the line's transparent sides (rather than hitting a grate's texture)
        [CreateProperty]
        public bool Decorative
        {
            get
            {
                return Line.is_decorative();
            }
            set
            {
                Edit(line => line.NativeObject.set_decorative(value));
            }
        }

        // Only a line with a transparent side has anything for projectiles to pass through
        [CreateProperty]
        public bool IsDecorativeEditable
        {
            get
            {
                return map.LINE_HAS_TRANSPARENT_SIDE(Line) || Line.is_decorative();
            }
        }
    }
}
