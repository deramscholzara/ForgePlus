using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Runtime.Constraints;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPMapObject : MonoBehaviour, IFPManipulatable<MapObject>
    {
        private static Material MapObjectPlaceholderMaterial;
        private static Mesh ItemMesh;
        private static Mesh SceneryMesh;
        private static Mesh SoundMesh;

        private const float PlaceholderHeight = 0.05f;

        private enum SideDataSources
        {
            Primary,
            Secondary,
            Transparent,
        }

        public short? Index { get; set; }
        public MapObject WelandObject { get; set; }

        public FPLevel FPLevel { private get; set; }

        public void Awake()
        {
            if (!MapObjectPlaceholderMaterial)
            {
                MapObjectPlaceholderMaterial = new Material(Shader.Find("ForgePlus/MapObjectPlaceholder"));
            }

            if (!ItemMesh)
            {
                ItemMesh = Resources.Load<Mesh>("Objects/Item");
            }

            if (!SceneryMesh)
            {
                SceneryMesh = Resources.Load<Mesh>("Objects/Scenery");
            }

            if (!SoundMesh)
            {
                SoundMesh = Resources.Load<Mesh>("Objects/Sound");
            }
        }

        public void GenerateObject()
        {
            switch (WelandObject.Type)
            {
                case ObjectType.Player:
                    gameObject.AddComponent<MeshFilter>().sharedMesh = BuildTriangleMesh(Color.yellow);
                    break;
                case ObjectType.Monster:
                    gameObject.AddComponent<MeshFilter>().sharedMesh = BuildTriangleMesh(Color.red);
                    break;
                case ObjectType.Item:
                    gameObject.AddComponent<MeshFilter>().sharedMesh = ItemMesh;
                    break;
                case ObjectType.Scenery:
                    gameObject.AddComponent<MeshFilter>().sharedMesh = SceneryMesh;
                    break;
                case ObjectType.Sound:
                    gameObject.AddComponent<MeshFilter>().sharedMesh = SoundMesh;
                    break;
                case ObjectType.Goal:
                default:
                    Debug.LogError($"Object type \"{WelandObject.Type}\" is not implemented and will not be displayed.");
                    return;
            }

            gameObject.AddComponent<MeshRenderer>().sharedMaterial = MapObjectPlaceholderMaterial;

            var floorRelativeElevation = FPLevel.Level.Polygons[WelandObject.PolygonIndex].FloorHeight + WelandObject.Z;

            transform.position = new Vector3(WelandObject.X, floorRelativeElevation, -WelandObject.Y) / GeometryUtilities.WorldUnitIncrementsPerMeter;

            if (WelandObject.Type == ObjectType.Player ||
                WelandObject.Type == ObjectType.Monster)
            {
                // Directionality only for these types
                // 
                transform.eulerAngles = new Vector3(0f, (float)WelandObject.Facing + 90f, 0f);
            }
        }

        private Mesh BuildTriangleMesh(Color color)
        {
            var mesh = CreateNamedMesh();

            mesh.vertices = new Vector3[]
            {
                new Vector3(0f, 0f, 0.2f),
                new Vector3(0.15f, 0f, -0.2f),
                new Vector3(-0.15f, 0f, -0.2f),
                new Vector3(0f, PlaceholderHeight, 0.2f),
                new Vector3(0.15f, PlaceholderHeight, -0.2f),
                new Vector3(-0.15f, PlaceholderHeight, -0.2f),
            };

            mesh.triangles = new int[]
            {
                2, 1, 0, // triangle bottom cap
                0, 1, 3, // triangle right-side lower
                3, 1, 4, // triangle right-side upper
                1, 2, 4, // triangle back-side lower
                4, 2, 5, // triangle back-side upper
                2, 0, 5, // triangle left-side lower
                5, 0, 3, // triangle left-side upper
                3, 4, 5, // triangle top cap
            };

            mesh.colors = new Color[]
            {
                color,
                color,
                color,
                color,
                color,
                color,
            };

            return mesh;
        }

        private Mesh CreateNamedMesh()
        {
            var mesh = new Mesh();

            mesh.name = $"{WelandObject.Type} ({Index})";

            return mesh;
        }
    }
}
