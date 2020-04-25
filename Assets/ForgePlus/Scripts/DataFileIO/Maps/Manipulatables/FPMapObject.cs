using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPMapObject : FPInteractiveSurfaceBase, IFPManipulatable<MapObject>, IFPSelectionDisplayable, IFPInspectable
    {
        private readonly int selectedShaderPropertyId = Shader.PropertyToID("_Selected");

        private static Material MapObjectPlaceholderMaterial;
        private static Material MapObjectPlaceholderSelectedMaterial;

        private static Mesh PlayerMesh;
        private static Mesh MonsterMesh;
        private static Mesh GoalMesh; // TODO: replace this with something more appropriate - like a flag or something

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

        public short Index { get; set; }
        public MapObject WelandObject { get; set; }

        public FPLevel FPLevel { private get; set; }

        public Placement Placement
        {
            get
            {
                if (WelandObject.Type == ObjectType.Monster)
                {
                    return FPLevel.Level.MonsterPlacement[WelandObject.Index];
                }
                else if (WelandObject.Type == ObjectType.Item)
                {
                    return FPLevel.Level.ItemPlacement[WelandObject.Index];
                }

                return null;
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (isSelectable)
            {
                SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false);
            }
        }

        public override void SetSelectability(bool enabled)
        {
            base.SetSelectability(enabled);

            GetComponent<MeshCollider>().enabled = enabled;
        }

        public void DisplaySelectionState(bool state)
        {
            var renderer = GetComponent<Renderer>();

            if (state)
            {
                renderer.sharedMaterial = MapObjectPlaceholderSelectedMaterial;

                gameObject.layer = SelectionManager.SelectionIndicatorLayer;
            }
            else
            {
                renderer.sharedMaterial = MapObjectPlaceholderMaterial;

                gameObject.layer = SelectionManager.DefaultLayer;
            }
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<InspectorFPMapObject>("Inspectors/Inspector - FPMapObject");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void GenerateObject()
        {
            switch (WelandObject.Type)
            {
                case ObjectType.Player:
                    if (!PlayerMesh)
                    {
                        PlayerMesh = BuildTriangleMesh(Color.yellow);
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = PlayerMesh;
                    break;
                case ObjectType.Monster:
                    if (!MonsterMesh)
                    {
                        MonsterMesh = BuildTriangleMesh(Color.red);
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = MonsterMesh;
                    break;
                case ObjectType.Item:
                    if (!ItemMesh)
                    {
                        ItemMesh = Resources.Load<Mesh>("Objects/Item");
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = ItemMesh;
                    break;
                case ObjectType.Scenery:
                    if (!SceneryMesh)
                    {
                        SceneryMesh = Resources.Load<Mesh>("Objects/Scenery");
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = SceneryMesh;
                    break;
                case ObjectType.Sound:
                    if (!SoundMesh)
                    {
                        SoundMesh = Resources.Load<Mesh>("Objects/Sound");
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = SoundMesh;
                    break;
                case ObjectType.Goal:
                    if (!GoalMesh)
                    {
                        GoalMesh = BuildTriangleMesh(Color.white);
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = GoalMesh;
                    break;
                default:
                    Debug.LogError($"Object type \"{WelandObject.Type}\" is not implemented and will not be displayed.");
                    return;
            }

            if (!MapObjectPlaceholderMaterial)
            {
                MapObjectPlaceholderMaterial = new Material(Shader.Find("ForgePlus/MapObjectPlaceholder"));
                MapObjectPlaceholderSelectedMaterial = new Material(MapObjectPlaceholderMaterial);
                MapObjectPlaceholderSelectedMaterial.SetFloat(selectedShaderPropertyId, 1f);
            }

            gameObject.AddComponent<MeshRenderer>().sharedMaterial = MapObjectPlaceholderMaterial;

            gameObject.AddComponent<MeshCollider>().convex = true;

            int elevation = WelandObject.FromCeiling ?
                            FPLevel.Level.Polygons[WelandObject.PolygonIndex].CeilingHeight + WelandObject.Z :
                            FPLevel.Level.Polygons[WelandObject.PolygonIndex].FloorHeight + WelandObject.Z;

            if (WelandObject.FromCeiling)
            {
                transform.localScale = new Vector3(1f, -1f, 1f);
            }

            transform.position = new Vector3(WelandObject.X, elevation, -WelandObject.Y) / GeometryUtilities.WorldUnitIncrementsPerMeter;

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
