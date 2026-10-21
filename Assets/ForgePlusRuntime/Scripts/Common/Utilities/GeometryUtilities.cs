using AlephOne;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.LevelManipulation.Utilities
{
    public enum TransferModes
    {
        Normal = 0,
        Pulsate = 4,
        Wobble = 5,
        WobbleFast = 6,
        Landscape = 9,
        HorizontalSlide = 15,
        HorizontalSlideFast = 16,
        VerticalSlide = 17,
        VerticalSlideFast = 18,
        Wander = 19,
        WanderFast = 20,
    }

    [AutoStaticsCleanup]
    public static partial class GeometryUtilities
    {
        // Used for converting between world-unit (WU) "increments" and meters.
        // One WU is 1024 "increments", and we want a WU to convert to 2 meters, so we use 512 as the conversion ratio.
        public const float WorldUnitIncrementsPerMeter = 512f;
        public const float WorldUnitIncrementsPerWorldUnit = world.WORLD_ONE;
        public const float MeterToWorldUnit = WorldUnitIncrementsPerMeter / WorldUnitIncrementsPerWorldUnit;

        public const float UnitsPerTextureOffetNudge = WorldUnitIncrementsPerWorldUnit / 128f;

        // What texture offsets snap to (a 32nd of a world unit), dragged or nudged while snapping is on
        public const float UnitsPerTextureOffsetSnap = WorldUnitIncrementsPerWorldUnit / 32f;

        // Recreated when destroyed, such as at the end of a Play session
        private static Material selectionIndicatorMaterial;

        // Given to renderers that ExcludeFromGpuResidentDrawer leaves out of it
        private static MaterialPropertyBlock notGpuDrivenPropertyBlock;

        // For renderers lying on (or just in front of) surfaces, such as selection indicators and diagnostic overlays:
        // drawn without the GPU Resident Drawer, whose GPU occlusion culling hides some of them from some angles (judging
        // them to be behind the surface). It doesn't draw renderers that have a property block, so they're given one that
        // changes nothing.
        public static void ExcludeFromGpuResidentDrawer(Renderer renderer)
        {
            if (notGpuDrivenPropertyBlock == null)
            {
                notGpuDrivenPropertyBlock = new MaterialPropertyBlock();
                notGpuDrivenPropertyBlock.SetFloat("_ForgePlusNotGpuDriven", 1f);
            }

            renderer.SetPropertyBlock(notGpuDrivenPropertyBlock);
        }

        private static Material SelectionIndicatorMaterial
        {
            get
            {
                if (!selectionIndicatorMaterial)
                {
                    selectionIndicatorMaterial = CreateSelectionIndicatorMaterial();
                }

                return selectionIndicatorMaterial;
            }
        }

        // Selection indicators are drawn over everything: their shader ignores depth, and they're drawn after every other
        // transparent (so nothing drawn later covers them)
        public static Material CreateSelectionIndicatorMaterial()
        {
            return new Material(Shader.Find("ForgePlus/GeometrySelectionIndicator"))
            {
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100,
            };
        }

        public static Vector3 GetMeshVertex(MapLevel level, int endpointIndex, short height = 0)
        {
            var endpoint = level.EndpointList[endpointIndex].vertex;

            // Convert from Marathon right-handed to Unity left-handed
            // by flipping Y-axis and assigning it to Z
            return new Vector3(endpoint.x, height, -endpoint.y) / WorldUnitIncrementsPerMeter;
        }

        // One corner's indicator (on a parent that carries it as it moves)
        public static GameObject CreateSurfaceSelectionIndicator(string name, Transform parent, Vector3 vertexWorldPosition, Vector3 nextVertexWorldPosition, Vector3 previousVertexWorldPosition)
        {
            var indicator = CreateIndicatorObject(name, parent);

            BeginIndicatorMesh();
            AddCorner(parent.worldToLocalMatrix, vertexWorldPosition, nextVertexWorldPosition, previousVertexWorldPosition);
            EndIndicatorMesh(indicator);

            return indicator;
        }

        // Fits the indicators of every corner of a surface, as one mesh (one renderer and one draw for the surface): made the
        // first time, then reshaped in place (a ceiling's vertices wind the other way)
        public static GameObject[] FitSurfaceSelectionIndicators(string name, Transform surface, Vector3[] vertices, bool isCeiling, GameObject[] indicators = null)
        {
            if (indicators == null || indicators.Length != 1 || !indicators[0])
            {
                if (indicators != null)
                {
                    foreach (var indicator in indicators)
                    {
                        DestroySurfaceSelectionIndicator(indicator);
                    }
                }

                indicators = new[] { CreateIndicatorObject(name, surface) };
            }

            var localToWorldMatrix = surface.localToWorldMatrix;
            var worldToLocalMatrix = surface.worldToLocalMatrix;

            BeginIndicatorMesh();

            for (var i = 0; i < vertices.Length; i++)
            {
                var nextIndex = i < vertices.Length - 1 ? i + 1 : 0;
                var previousIndex = i >= 1 ? i - 1 : vertices.Length - 1;

                if (isCeiling)
                {
                    (nextIndex, previousIndex) = (previousIndex, nextIndex);
                }

                AddCorner(worldToLocalMatrix,
                          localToWorldMatrix.MultiplyPoint(vertices[i]),
                          localToWorldMatrix.MultiplyPoint(vertices[nextIndex]),
                          localToWorldMatrix.MultiplyPoint(vertices[previousIndex]));
            }

            EndIndicatorMesh(indicators[0]);

            return indicators;
        }

        // At its parent's origin, so its mesh is in the parent's space and it moves with it
        private static GameObject CreateIndicatorObject(string name, Transform parent)
        {
            var indicator = new GameObject($"Selection Indicators - {name}");
            indicator.transform.SetParent(parent, worldPositionStays: false);
            indicator.layer = SelectionManager.SelectionIndicatorLayer;

            indicator.AddComponent<MeshFilter>();
            var indicatorRenderer = indicator.AddComponent<MeshRenderer>();
            indicatorRenderer.sharedMaterial = SelectionIndicatorMaterial;
            ExcludeFromGpuResidentDrawer(indicatorRenderer);
            SurfaceCacheRenderingLayers.Exclude(indicatorRenderer);

            return indicator;
        }

        public static void DestroySurfaceSelectionIndicator(GameObject indicator)
        {
            if (!indicator)
            {
                return;
            }

            var meshFilter = indicator.GetComponent<MeshFilter>();
            if (meshFilter && meshFilter.sharedMesh)
            {
                UnityEngine.Object.Destroy(meshFilter.sharedMesh);
            }

            UnityEngine.Object.Destroy(indicator);
        }

        // Reused while building an indicator's mesh
        private static readonly List<Vector3> indicatorVertices = new List<Vector3>();
        private static readonly List<int> indicatorTriangles = new List<int>();
        private static readonly List<Color> indicatorColors = new List<Color>();

        private static readonly Color IndicatorEdgeColor = new Color(1f, 1f, 1f, 0.75f);
        private static readonly Color IndicatorInnerColor = new Color(1f, 1f, 1f, 0f);

        private static void BeginIndicatorMesh()
        {
            indicatorVertices.Clear();
            indicatorTriangles.Clear();
            indicatorColors.Clear();
        }

        // Into the indicator's own mesh, made once and refilled in place after
        private static void EndIndicatorMesh(GameObject indicator)
        {
            var meshFilter = indicator.GetComponent<MeshFilter>();
            var mesh = meshFilter.sharedMesh;
            if (!mesh)
            {
                mesh = new Mesh { name = indicator.name };
                mesh.MarkDynamic();
                meshFilter.sharedMesh = mesh;
            }

            mesh.Clear();
            mesh.SetVertices(indicatorVertices);
            mesh.SetColors(indicatorColors);
            mesh.SetTriangles(indicatorTriangles, 0);
        }

        // A corner's L-shaped wedge, along the edges to the next and previous corners, in the indicator's space
        private static void AddCorner(Matrix4x4 worldToLocalMatrix, Vector3 vertexWorldPosition, Vector3 nextVertexWorldPosition, Vector3 previousVertexWorldPosition)
        {
            const float thickness = 0.04f;
            const float length = 0.2f;

            var clockwiseDirection = (nextVertexWorldPosition - vertexWorldPosition).normalized;
            var counterclockwiseDirection = (previousVertexWorldPosition - vertexWorldPosition).normalized;
            var scale = Mathf.Min(1f, Vector3.Distance(vertexWorldPosition, nextVertexWorldPosition) / (length * 2f), Vector3.Distance(vertexWorldPosition, previousVertexWorldPosition) / (length * 2f));

            var facingVector = Vector3.Cross(clockwiseDirection, counterclockwiseDirection);
            var clockwiseThicknessDirection = Vector3.Cross(facingVector, clockwiseDirection).normalized;
            var counterclockwiseThicknessDirection = Vector3.Cross(counterclockwiseDirection, facingVector).normalized;
            var insetCornerPosition = thickness / Mathf.Abs(Mathf.Sin(Vector3.Angle(clockwiseDirection, counterclockwiseDirection) * 0.5f * Mathf.Deg2Rad)) * (clockwiseThicknessDirection + counterclockwiseThicknessDirection).normalized;

            var first = indicatorVertices.Count;

            AddIndicatorVertex(worldToLocalMatrix, vertexWorldPosition, IndicatorEdgeColor);
            AddIndicatorVertex(worldToLocalMatrix, vertexWorldPosition + (clockwiseDirection * length) * scale, IndicatorEdgeColor);
            AddIndicatorVertex(worldToLocalMatrix, vertexWorldPosition + (clockwiseDirection * length + clockwiseThicknessDirection * thickness) * scale, IndicatorInnerColor);
            AddIndicatorVertex(worldToLocalMatrix, vertexWorldPosition + insetCornerPosition * scale, IndicatorInnerColor);
            AddIndicatorVertex(worldToLocalMatrix, vertexWorldPosition + (counterclockwiseThicknessDirection * thickness + counterclockwiseDirection * length) * scale, IndicatorInnerColor);
            AddIndicatorVertex(worldToLocalMatrix, vertexWorldPosition + (counterclockwiseDirection * length) * scale, IndicatorEdgeColor);

            indicatorTriangles.Add(first + 0); indicatorTriangles.Add(first + 1); indicatorTriangles.Add(first + 2);
            indicatorTriangles.Add(first + 2); indicatorTriangles.Add(first + 3); indicatorTriangles.Add(first + 0);
            indicatorTriangles.Add(first + 3); indicatorTriangles.Add(first + 4); indicatorTriangles.Add(first + 0);
            indicatorTriangles.Add(first + 4); indicatorTriangles.Add(first + 5); indicatorTriangles.Add(first + 0);
        }

        private static void AddIndicatorVertex(Matrix4x4 worldToLocalMatrix, Vector3 worldPosition, Color color)
        {
            indicatorVertices.Add(worldToLocalMatrix.MultiplyPoint3x4(worldPosition));
            indicatorColors.Add(color);
        }
    }
}
