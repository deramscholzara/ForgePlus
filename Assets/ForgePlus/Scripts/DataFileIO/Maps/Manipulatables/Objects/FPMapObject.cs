using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    [RequireComponent(typeof(MeshCollider))]
    public class FPMapObject : MonoBehaviour, IFPManipulatable<MapObject>, IFPSelectable, IFPInspectable
    {
        private readonly int selectedShaderPropertyId = Shader.PropertyToID("_Selected");

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

        private Material unselectedMaterial = null;

        // TODO: Set up visibility filtering (static state member+enum per relevant type?)
        public void OnMouseUpAsButton()
        {
            SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false);
        }

        public void SetSelectability(bool enabled)
        {
            GetComponent<MeshCollider>().enabled = enabled;
        }

        public void DisplaySelectionState(bool state)
        {
            var renderer = GetComponent<Renderer>();

            if (state)
            {
                // TODO: Create a selection utilities class for instantiating and arranging selection corners to vertices (needs a shader that renders on top of everything else, in a new render pass, too)
                //       Not really needed for MapObjects, since they'll just use stripes effect, but it'll be important for geometry (specifically, polygons & sides (selecting a side also displays line info))
                if (!unselectedMaterial)
                {
                    unselectedMaterial = renderer.sharedMaterial;
                }

                renderer.material.SetFloat(selectedShaderPropertyId, 1f);
            }
            else
            {
                renderer.sharedMaterial = unselectedMaterial;

                unselectedMaterial = null;
            }
        }

        public void Inspect()
        {
            var prefab = Resources.Load<InspectorFPMapObject>("Inspectors/Inspector - FPMapObject");
            var inspector = Instantiate(prefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);

            // Note: This method would call Inspect() on any other relevant objects
            //       - like a side calling a line, and a line calling a polygon
        }

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

            gameObject.AddComponent<MeshCollider>().convex = true;

            var floorRelativeElevation = FPLevel.Level.Polygons[WelandObject.PolygonIndex].FloorHeight + WelandObject.Z;

            transform.position = new Vector3(WelandObject.X, floorRelativeElevation, -WelandObject.Y) / GeometryUtilities.WorldUnitIncrementsPerMeter;

            transform.eulerAngles = new Vector3(0f, (float)WelandObject.Facing + 90f, 0f);
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
