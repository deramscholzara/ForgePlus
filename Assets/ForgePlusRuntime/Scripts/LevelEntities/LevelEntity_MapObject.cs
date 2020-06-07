using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace RuntimeCore.Entities.MapObjects
{
    // TODO: Should inherit from LevelEntity_Base, and should have a separate EditableSurface component
    public class LevelEntity_MapObject : EditableSurface_Base, ISelectionDisplayable, IInspectable
    {
        private readonly int selectedShaderPropertyId = Shader.PropertyToID("_Selected");

        private static Material MapObjectPlaceholderMaterial;
        private static Material MapObjectPlaceholderSelectedMaterial;

        private static Mesh PlayerMesh;
        private static Mesh MonsterMesh;
        private static Mesh GoalMesh; // TODO: replace this with something more appropriate - like a flag or something
        private static Mesh GenericMesh;

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

        public short NativeIndex { get; set; }
        public MapObject NativeObject { get; set; }

        public LevelEntity_Level ParentLevel { private get; set; }

        public Placement Placement
        {
            get
            {
                if (NativeObject.Type == ObjectType.Monster)
                {
                    return ParentLevel.Level.MonsterPlacement[NativeObject.Index];
                }
                else if (NativeObject.Type == ObjectType.Item)
                {
                    return ParentLevel.Level.ItemPlacement[NativeObject.Index];
                }

                return null;
            }
        }

        public override void OnValidatedPointerClick(PointerEventData eventData)
        {
            SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false);
        }

        public override void OnValidatedBeginDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedEndDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
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
            var inspectorPrefab = Resources.Load<Inspector_MapObject>("Inspectors/Inspector - MapObject");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void GenerateObject()
        {
            switch (NativeObject.Type)
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
                    Debug.LogError($"Object type \"{NativeObject.Type}\" is not part of the standard Marathon 2 engine - so... be careful.");
                    if (!GenericMesh)
                    {
                        GenericMesh = BuildTriangleMesh(Color.white);
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = GenericMesh;
                    break;
            }

            if (!MapObjectPlaceholderMaterial)
            {
                MapObjectPlaceholderMaterial = new Material(Shader.Find("ForgePlus/MapObjectPlaceholder"));
                MapObjectPlaceholderSelectedMaterial = new Material(MapObjectPlaceholderMaterial);
                MapObjectPlaceholderSelectedMaterial.SetFloat(selectedShaderPropertyId, 1f);
            }

            gameObject.AddComponent<MeshRenderer>().sharedMaterial = MapObjectPlaceholderMaterial;

            gameObject.AddComponent<MeshCollider>().convex = true;

            int elevation = NativeObject.FromCeiling ?
                            ParentLevel.Level.Polygons[NativeObject.PolygonIndex].CeilingHeight + NativeObject.Z :
                            ParentLevel.Level.Polygons[NativeObject.PolygonIndex].FloorHeight + NativeObject.Z;

            if (NativeObject.FromCeiling)
            {
                transform.localScale = new Vector3(1f, -1f, 1f);
            }

            transform.position = new Vector3(NativeObject.X, elevation, -NativeObject.Y) / GeometryUtilities.WorldUnitIncrementsPerMeter;

            transform.eulerAngles = new Vector3(0f, (float)NativeObject.Facing + 90f, 0f);
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

            mesh.name = $"{NativeObject.Type} ({NativeIndex})";

            return mesh;
        }
    }
}
