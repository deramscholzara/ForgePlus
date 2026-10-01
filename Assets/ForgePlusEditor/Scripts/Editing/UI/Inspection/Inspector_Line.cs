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
        public bool Transparent
        {
            get
            {
                return map.LINE_IS_TRANSPARENT(Line);
            }
        }

        // Aleph One only: projectiles always pass through the line's transparent sides (rather than hitting their
        // textures, such as a grate's)
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
