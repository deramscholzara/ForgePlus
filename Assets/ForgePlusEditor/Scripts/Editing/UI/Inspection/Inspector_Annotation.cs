using ForgePlus.Extensions;
using RuntimeCore.Entities;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_Annotation : Inspector_Base<LevelEntity_Annotation>
    {
        public Inspector_Annotation(LevelEntity_Annotation annotation) : base(annotation)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Annotation";
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
        public string Text
        {
            get
            {
                return Entity.NativeObject.GetText();
            }
        }
    }
}
