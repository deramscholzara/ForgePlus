using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
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

        // Aleph One draws it on one line, in Mac Roman
        [CreateProperty]
        public string Text
        {
            get
            {
                return Entity.NativeObject.GetText();
            }
            set
            {
                Edit(annotation => annotation.SetText(value));
            }
        }

        // Aleph One has one annotation style (0), and its map doesn't draw an annotation of another type
        [CreateProperty]
        public string Type
        {
            get
            {
                return Entity.NativeObject.type.ToString();
            }
        }

        // Where the map draws the text's lower left, in world units
        [CreateProperty]
        public int LocationX
        {
            get
            {
                return Entity.NativeObject.location.x;
            }
            set
            {
                Edit(annotation => annotation.SetLocation(new world_point2d(ClampToShort(value), annotation.NativeObject.location.y)));
            }
        }

        [CreateProperty]
        public int LocationY
        {
            get
            {
                return Entity.NativeObject.location.y;
            }
            set
            {
                Edit(annotation => annotation.SetLocation(new world_point2d(annotation.NativeObject.location.x, ClampToShort(value))));
            }
        }

        // The map only shows the annotation once this polygon is on it
        [CreateProperty]
        public int PolygonIndex
        {
            get
            {
                return Entity.NativeObject.polygon_index;
            }
            set
            {
                var isPolygonIndex = value >= short.MinValue && value <= short.MaxValue;
                LevelEntity_Polygon polygon = null;

                if (isPolygonIndex && LevelEntity_Level.Instance.Polygons.TryGetValue((short) value, out polygon))
                {
                    Edit(annotation => annotation.SetPolygon(polygon));
                }
                else
                {
                    RefreshInspectorsOf(Entity);
                }
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Root.Find<TextField>(nameof(Text)).LimitToMacRomanText(Entity.NativeObject.text.MacRomanTextCapacity());
        }
    }
}
