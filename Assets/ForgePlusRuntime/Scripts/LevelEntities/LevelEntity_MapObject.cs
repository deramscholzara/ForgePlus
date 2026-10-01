using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Materials;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using AlephOne;
using ForgePlus.Extensions;
using Rect = UnityEngine.Rect;

namespace RuntimeCore.Entities.MapObjects
{
    // TODO: Should inherit from LevelEntity_Base, and should have a separate EditableSurface component
    [AutoStaticsCleanup]
    public partial class LevelEntity_MapObject : EditableSurface_Base, ISelectionDisplayable, IInspectable
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

        private static readonly Dictionary<Rect, Mesh> SpriteQuadMeshes = new Dictionary<Rect, Mesh>();

        private const float PlaceholderHeight = 0.05f;
        private const string SpriteObjectName = "Sprite";

        private static bool iconsAreVisible = true;
        private static bool spritePreviewsAreVisible = true;

        private MeshRenderer spritePreviewRenderer;

        private enum SideDataSources
        {
            Primary,
            Secondary,
            Transparent,
        }

        public short NativeIndex { get; set; }
        public map_object NativeObject { get; set; }

        public LevelEntity_Level ParentLevel { private get; set; }

        // The icon is the placeholder geometry on the root
        public static bool IconsAreVisible
        {
            get
            {
                return iconsAreVisible;
            }
            set
            {
                iconsAreVisible = value;

                ApplyVisibilityToAllObjects();
            }
        }

        // Sound sources' icons show while this is set, whether or not the other icons do (in Sounds mode, where they're
        // selected)
        public static bool SoundSourceIconsAreVisible
        {
            get
            {
                return soundSourceIconsAreVisible;
            }
            set
            {
                soundSourceIconsAreVisible = value;

                ApplyVisibilityToAllObjects();
            }
        }

        private static bool soundSourceIconsAreVisible = false;

        public static bool SpritePreviewsAreVisible
        {
            get
            {
                return spritePreviewsAreVisible;
            }
            set
            {
                spritePreviewsAreVisible = value;

                ApplyVisibilityToAllObjects();
            }
        }

        public object_frequency_definition Placement
        {
            get
            {
                if (NativeObject.type == map._saved_monster)
                {
                    return ParentLevel.Level.object_placement_info[placement.monster_placement_info + NativeObject.index];
                }
                else if (NativeObject.type == map._saved_item)
                {
                    return ParentLevel.Level.object_placement_info[placement.item_placement_info + NativeObject.index];
                }

                return null;
            }
        }

        public override void OnValidatedPointerClick(WorldPointerEventData eventData)
        {
            SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false);
        }

        public override void OnValidatedBeginDrag(WorldPointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedDrag(WorldPointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedEndDrag(WorldPointerEventData eventData)
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
            var inspector = new Inspector_MapObject(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void GenerateObject()
        {
            switch (NativeObject.type)
            {
                case map._saved_player:
                    if (!PlayerMesh)
                    {
                        PlayerMesh = BuildTriangleMesh(Color.yellow);
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = PlayerMesh;
                    break;
                case map._saved_monster:
                    if (!MonsterMesh)
                    {
                        MonsterMesh = BuildTriangleMesh(Color.red);
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = MonsterMesh;
                    break;
                case map._saved_item:
                    if (!ItemMesh)
                    {
                        ItemMesh = Resources.Load<Mesh>("Objects/Item");
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = ItemMesh;
                    break;
                case map._saved_object:
                    if (!SceneryMesh)
                    {
                        SceneryMesh = Resources.Load<Mesh>("Objects/Scenery");
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = SceneryMesh;
                    break;
                case map._saved_sound_source:
                    if (!SoundMesh)
                    {
                        SoundMesh = Resources.Load<Mesh>("Objects/Sound");
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = SoundMesh;
                    break;
                case map._saved_goal:
                    if (!GoalMesh)
                    {
                        GoalMesh = BuildTriangleMesh(Color.white);
                    }

                    gameObject.AddComponent<MeshFilter>().sharedMesh = GoalMesh;
                    break;
                default:
                    Debug.LogError($"Object type \"{NativeObject.GetTypeName()}\" is not part of the standard Marathon 2 engine - so... be careful.");
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
                MapObjectPlaceholderMaterial.enableInstancing = true;
                MapObjectPlaceholderSelectedMaterial = new Material(MapObjectPlaceholderMaterial);
                MapObjectPlaceholderSelectedMaterial.SetFloat(selectedShaderPropertyId, 1f);
            }

            gameObject.AddComponent<MeshRenderer>().sharedMaterial = MapObjectPlaceholderMaterial;

            gameObject.AddComponent<MeshCollider>().convex = true;

            ApplyPlacement();

            GenerateSprite();

            ApplyVisibility();
        }

        // Where it is (from its polygon's floor, or its ceiling for one hanging from it), and which way it faces
        public void ApplyPlacement()
        {
            var hangsFromCeiling = (NativeObject.flags & map._map_object_hanging_from_ceiling) != 0;

            int elevation = hangsFromCeiling ?
                            ParentLevel.Level.PolygonList[NativeObject.polygon_index].ceiling_height + NativeObject.location.z :
                            ParentLevel.Level.PolygonList[NativeObject.polygon_index].floor_height + NativeObject.location.z;

            transform.localScale = new Vector3(1f, hangsFromCeiling ? -1f : 1f, 1f);

            transform.position = new Vector3(NativeObject.location.x, elevation, -NativeObject.location.y) / GeometryUtilities.WorldUnitIncrementsPerMeter;

            // A sound source's facing is its volume instead, so it isn't turned
            var facing = NativeObject.type == map._saved_sound_source ? (short) 0 : NativeObject.facing;
            transform.eulerAngles = new Vector3(0f, AlephOneExtensions.AngleToDegrees(facing) + 90f, 0f);
        }

        private static void ApplyVisibilityToAllObjects()
        {
            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                // No level is open, so exit
                return;
            }

            foreach (var mapObject in level.MapObjects.Values)
            {
                mapObject.ApplyVisibility();
            }
        }

        private void ApplyVisibility()
        {
            GetComponent<MeshRenderer>().enabled = iconsAreVisible || (soundSourceIconsAreVisible && NativeObject.type == map._saved_sound_source);

            if (spritePreviewRenderer)
            {
                spritePreviewRenderer.enabled = spritePreviewsAreVisible;
            }
        }

        private void GenerateSprite()
        {
            var sprite = MaterialGeneration_Sprites.GetSprite(MapObjectSpriteDefinitions.GetDefinition(NativeObject));

            if (sprite == null)
            {
                // Sounds, goals, and anything missing from the loaded shapes file have no sprite
                return;
            }

            var spriteObject = new GameObject(SpriteObjectName);
            spriteObject.transform.SetParent(transform, worldPositionStays: false);
            spriteObject.transform.localPosition = Vector3.zero;
            spriteObject.transform.localRotation = Quaternion.identity;

            spriteObject.AddComponent<MeshFilter>().sharedMesh = GetSpriteQuadMesh(sprite.Bounds);
            spritePreviewRenderer = spriteObject.AddComponent<MeshRenderer>();
            spritePreviewRenderer.sharedMaterial = sprite.Material;
        }

        private static Mesh GetSpriteQuadMesh(Rect bounds)
        {
            if (SpriteQuadMeshes.TryGetValue(bounds, out var mesh))
            {
                return mesh;
            }

            mesh = BuildQuadMesh(bounds);
            SpriteQuadMeshes[bounds] = mesh;

            return mesh;
        }

        // On the local XY plane, which the DirectionalSpriteRenderer shader billboards around the object's origin
        private static Mesh BuildQuadMesh(Rect bounds)
        {
            var mesh = new Mesh();

            mesh.name = $"Sprite Quad ({bounds.width:0.###} x {bounds.height:0.###})";

            mesh.vertices = new Vector3[]
            {
                new Vector3(bounds.xMin, bounds.yMin, 0f),
                new Vector3(bounds.xMax, bounds.yMin, 0f),
                new Vector3(bounds.xMin, bounds.yMax, 0f),
                new Vector3(bounds.xMax, bounds.yMax, 0f),
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };

            mesh.triangles = new int[]
            {
                0, 2, 1, // lower-left triangle
                2, 3, 1, // upper-right triangle
            };

            mesh.RecalculateNormals();

            // The shader turns the quad to face the camera and ignores a ceiling object's flipped scale,
            // so the bounds must contain it at any rotation, above and below the origin
            var horizontalExtent = Mathf.Max(Mathf.Abs(bounds.xMin), Mathf.Abs(bounds.xMax));
            var verticalExtent = Mathf.Max(Mathf.Abs(bounds.yMin), Mathf.Abs(bounds.yMax));
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(horizontalExtent, verticalExtent, horizontalExtent) * 2f);

            return mesh;
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

            mesh.name = $"{NativeObject.GetTypeName()} ({NativeIndex})";

            return mesh;
        }
    }
}
