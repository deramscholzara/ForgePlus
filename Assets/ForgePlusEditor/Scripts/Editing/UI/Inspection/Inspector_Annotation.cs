using ForgePlus.Extensions;
using RuntimeCore.Entities;
using Unity.Properties;
using UnityEngine.UIElements;

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

        // Which of Aleph One's annotation styles its map draws it in. There's only one (0), and the map doesn't draw
        // an annotation of any other type, so it isn't for editing, but it's shown for finding the ones that aren't 0.
        [CreateProperty]
        public string Type
        {
            get
            {
                return Entity.NativeObject.type.ToString();
            }
        }

        [CreateProperty]
        public string LocationX
        {
            get
            {
                return Entity.NativeObject.location.x.ToString();
            }
        }

        [CreateProperty]
        public string LocationY
        {
            get
            {
                return Entity.NativeObject.location.y.ToString();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Root.Q("Type").SetEnabled(false);
        }

        // The map only shows the annotation once this polygon is on it
        [CreateProperty]
        public string PolygonIndex
        {
            get
            {
                return Entity.NativeObject.polygon_index.ToString();
            }
        }
    }
}
