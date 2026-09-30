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

    }
}
