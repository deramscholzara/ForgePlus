using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using System;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_Polygon : Inspector_Base<LevelEntity_Polygon>
    {
        public Inspector_Polygon(LevelEntity_Polygon polygon) : base(polygon)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Polygon";
            }
        }

        private polygon_data Polygon
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
        public string Type
        {
            get
            {
                return AlephOneNames.PolygonType(Polygon.type);
            }
        }

        [CreateProperty]
        public string Permutation
        {
            get
            {
                return Polygon.permutation.ToString();
            }
        }

        [CreateProperty]
        public string MediaIndex
        {
            get
            {
                return Polygon.media_index.ToString();
            }
        }

        [CreateProperty]
        public string MediaLight
        {
            get
            {
                return Polygon.media_lightsource_index.ToString();
            }
        }

        [CreateProperty]
        public string AmbientSound
        {
            get
            {
                return Polygon.ambient_sound_image_index.ToString();
            }
        }

        [CreateProperty]
        public string RandomSound
        {
            get
            {
                return Polygon.random_sound_image_index.ToString();
            }
        }

        [CreateProperty]
        public string FloorHeight
        {
            get
            {
                return Polygon.floor_height.ToString();
            }
        }

        [CreateProperty]
        public string FloorLightIndex
        {
            get
            {
                return Polygon.floor_lightsource_index.ToString();
            }
        }

        [CreateProperty]
        public string CeilingHeight
        {
            get
            {
                return Polygon.ceiling_height.ToString();
            }
        }

        [CreateProperty]
        public string CeilingLightIndex
        {
            get
            {
                return Polygon.ceiling_lightsource_index.ToString();
            }
        }

        [CreateProperty]
        public string VertexCount
        {
            get
            {
                return Polygon.vertex_count.ToString();
            }
        }

        [CreateProperty]
        public string VertexIndices
        {
            get
            {
                return Lines(Polygon.endpoint_indexes, index => index.ToString());
            }
        }

        [CreateProperty]
        public string LineIndices
        {
            get
            {
                return Lines(Polygon.line_indexes, index => index.ToString());
            }
        }

        [CreateProperty]
        public string SideIndices
        {
            get
            {
                return Lines(Polygon.side_indexes, index => index < 0 ? "- no side -" : index.ToString());
            }
        }

        [CreateProperty]
        public string AdjacentPolygonIndices
        {
            get
            {
                return Lines(Polygon.adjacent_polygon_indexes, index => index < 0 ? "- no polygon -" : index.ToString());
            }
        }

        [CreateProperty]
        public string FirstObjectIndex
        {
            get
            {
                return Polygon.first_object.ToString();
            }
        }

        // One of the polygon's vertices' values per line
        private string Lines(short[] values, Func<short, string> format)
        {
            var lines = new string[Polygon.vertex_count];
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = format(values[i]);
            }

            return string.Join("\n", lines);
        }
    }
}
