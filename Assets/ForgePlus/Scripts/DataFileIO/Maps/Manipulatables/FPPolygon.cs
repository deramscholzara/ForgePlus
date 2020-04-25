using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.ShapesCollections;
using System.Collections.Generic;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPPolygon : MonoBehaviour, IFPManipulatable<Polygon>, IFPSelectionDisplayable, IFPInspectable
    {
        private List<GameObject> selectionVisualizationIndicators = new List<GameObject>(16);

        public short Index { get; set; }
        public Polygon WelandObject { get; set; }
        public GameObject CeilingSurface;
        public GameObject FloorSurface;

        public FPLevel FPLevel { private get; set; }

        public void SetSelectability(bool enabled)
        {
            // Intentionally empty - Selectability is handled in FPSurfacePolygon & the availability of SwitchFPLight buttons
        }

        public void DisplaySelectionState(bool state)
        {
            if (state)
            {
                CreateSelectionIndicators(CeilingSurface, isfloor: false);
                CreateSelectionIndicators(FloorSurface, isfloor: true);
            }
            else
            {
                foreach (var indicator in selectionVisualizationIndicators)
                {
                    Destroy(indicator);
                }

                selectionVisualizationIndicators.Clear();
            }
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<InspectorFPPolygon>("Inspectors/Inspector - FPPolygon");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void GenerateSurfaces(Polygon polygon, short polygonIndex)
        {
            BuildFloorAndCeiling(polygon, (short)polygonIndex);
        }

        private void BuildFloorAndCeiling(
            Polygon polygon,
            short polygonIndex)
        {
            #region Generate_Mesh_Data
            var floorRoot = new GameObject($"Floor (polygon: {polygonIndex})");
            FloorSurface = floorRoot;
            floorRoot.transform.position = new Vector3(0f, polygon.FloorHeight / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);
            floorRoot.transform.SetParent(transform);
            var ceilingRoot = new GameObject($"Ceiling (polygon: {polygonIndex})");
            CeilingSurface = ceilingRoot;
            ceilingRoot.transform.position = new Vector3(0f, polygon.CeilingHeight / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);
            ceilingRoot.transform.SetParent(transform);

            var floorVertices = new Vector3[polygon.VertexCount];
            var ceilingVertices = new Vector3[polygon.VertexCount];
            var floorTriangles = new int[(polygon.VertexCount - 2) * 3];
            var ceilingTriangles = new int[(polygon.VertexCount - 2) * 3];

            #region Media
            var hasMedia = polygon.MediaIndex >= 0;
            GameObject mediaRoot = null;
            Media media = null;

            Vector3[] mediaVertices = null;
            int[] mediaTriangles = null;

            if (hasMedia)
            {
                mediaRoot = new GameObject($"Media (polygon: {polygonIndex} media:{polygon.MediaIndex})");
                mediaRoot.transform.SetParent(floorRoot.transform);

                media = FPLevel.Level.Medias[polygon.MediaIndex];

                mediaVertices = new Vector3[polygon.VertexCount];
                mediaTriangles = new int[(polygon.VertexCount - 2) * 3];
            }
            #endregion Media

            for (int earlyVertexIndex = 0, lateVertexIndex = polygon.VertexCount - 1, currentTriangleIndex = 0;
                 earlyVertexIndex <= lateVertexIndex;
                 earlyVertexIndex++, lateVertexIndex--)
            {
                AssignFloorAndCeilingVertices(earlyVertexIndex, polygon, floorVertices, ceilingVertices);

                if (hasMedia)
                {
                    AssignMediaVertex(earlyVertexIndex, polygon, mediaVertices);
                }

                if (earlyVertexIndex < lateVertexIndex)
                {
                    // Vertex-traversal has not intersected, so continue
                    AssignFloorAndCeilingVertices(lateVertexIndex, polygon, floorVertices, ceilingVertices);

                    if (hasMedia)
                    {
                        AssignMediaVertex(lateVertexIndex, polygon, mediaVertices);
                    }

                    if (earlyVertexIndex + 1 < lateVertexIndex)
                    {
                        // Vertex-traversal is not on the final vertices, so continue
                        AssignFloorOrCeilingOrMediaTriangle(earlyVertexIndex, lateVertexIndex, currentTriangleIndex, floorTriangles);
                        AssignFloorOrCeilingOrMediaTriangle(earlyVertexIndex, lateVertexIndex, currentTriangleIndex, ceilingTriangles, reverseOrder: true);

                        if (hasMedia)
                        {
                            AssignFloorOrCeilingOrMediaTriangle(earlyVertexIndex, lateVertexIndex, currentTriangleIndex, mediaTriangles);
                        }

                        currentTriangleIndex += 3;

                        if (earlyVertexIndex + 1 < lateVertexIndex - 1)
                        {
                            // Vertex traversal is not about to intersect, so continue
                            AssignFloorOrCeilingOrMediaTriangle(earlyVertexIndex, lateVertexIndex, currentTriangleIndex, floorTriangles, isLateTriangle: true);
                            AssignFloorOrCeilingOrMediaTriangle(earlyVertexIndex, lateVertexIndex, currentTriangleIndex, ceilingTriangles, isLateTriangle: true, reverseOrder: true);

                            if (hasMedia)
                            {
                                AssignFloorOrCeilingOrMediaTriangle(earlyVertexIndex, lateVertexIndex, currentTriangleIndex, mediaTriangles, isLateTriangle: true);
                            }

                            currentTriangleIndex += 3;
                        }
                    }
                }
            }

            #region UVs
            var ceilingUvs = new Vector2[ceilingVertices.Length];
            var floorUvs = new Vector2[floorVertices.Length];

            Vector2[] mediaUvs = null;
            if (hasMedia)
            {
                mediaUvs = new Vector2[floorVertices.Length];
            }

            for (var i = 0; i < floorUvs.Length; i++)
            {
                var u = -(ceilingVertices[i].z * GeometryUtilities.MeterToWorldUnit);
                var v = -(ceilingVertices[i].x * GeometryUtilities.MeterToWorldUnit);
                var ceilingOffset = new Vector2((float)polygon.CeilingOrigin.Y / GeometryUtilities.WorldUnitIncrementsPerWorldUnit,
                                                -(float)polygon.CeilingOrigin.X / GeometryUtilities.WorldUnitIncrementsPerWorldUnit);
                ceilingUvs[i] = new Vector2(u, v) + ceilingOffset;

                u = -(floorVertices[i].z * GeometryUtilities.MeterToWorldUnit);
                v = -(floorVertices[i].x * GeometryUtilities.MeterToWorldUnit);
                var floorOffset = new Vector2((float)polygon.FloorOrigin.Y / GeometryUtilities.WorldUnitIncrementsPerWorldUnit,
                                              -(float)polygon.FloorOrigin.X / GeometryUtilities.WorldUnitIncrementsPerWorldUnit);
                floorUvs[i] = new Vector2(u, v) + floorOffset;

                if (hasMedia)
                {
                    u = mediaVertices[i].z * GeometryUtilities.MeterToWorldUnit;
                    v = mediaVertices[i].x * GeometryUtilities.MeterToWorldUnit;
                    mediaUvs[i] = new Vector2(u, v);
                }
            }
            #endregion UVs
            #endregion Generate_Mesh_Data

            #region Platforms
            var hasCeilingPlatform = false;
            var hasFloorPlatform = false;
            short platformIndex = -1;
            if (polygon.Type == PolygonType.Platform)
            {
                var platform = GeometryUtilities.GetPlatformForPolygonIndex(FPLevel.Level, polygonIndex);
                platformIndex = GeometryUtilities.GetPlatformIndexForPolygonIndex(FPLevel.Level, polygonIndex);

                if (platform.ComesFromCeiling)
                {
                    hasCeilingPlatform = true;

                    var fpPlatform = ceilingRoot.AddComponent<FPPlatform>();
                    fpPlatform.SetPlatform(platformIndex, platform, FPLevel, FPPlatform.LinkedSurfaces.Ceiling);
                    FPLevel.FPCeilingFpPlatforms[platformIndex] = fpPlatform;
                }

                if (platform.ComesFromFloor)
                {
                    hasFloorPlatform = true;

                    var fpPlatform = floorRoot.AddComponent<FPPlatform>();
                    fpPlatform.SetPlatform(platformIndex, platform, FPLevel, FPPlatform.LinkedSurfaces.Floor);
                    FPLevel.FPFloorFpPlatforms[platformIndex] = fpPlatform;
                }
            }
            #endregion Platforms

            #region TransferModes_VertexColor
            var floorTransferModesVertexColors = new Color[floorVertices.Length];
            for (var i = 0; i < floorVertices.Length; i++)
            {
                floorTransferModesVertexColors[i] = GeometryUtilities.GetTransferModeVertexColor(polygon.FloorTransferMode, isSideSurface: false);
            }

            var ceilingTransferModesVertexColors = new Color[ceilingVertices.Length];
            for (var i = 0; i < ceilingVertices.Length; i++)
            {
                ceilingTransferModesVertexColors[i] = GeometryUtilities.GetTransferModeVertexColor(polygon.CeilingTransferMode, isSideSurface: false);
            }
            #endregion TransferModes_VertexColor

            // Floor
            GeometryUtilities.BuildRendererObject(
                floorRoot,
                floorVertices,
                floorTriangles,
                floorUvs,
                polygon.FloorTexture,
                FPLevel.FPLights[polygon.FloorLight],
                polygon.FloorTransferMode,
                floorTransferModesVertexColors,
                isOpaqueSurface: true,
                isStaticBatchable: !hasFloorPlatform);

            var fpSurfacePolygonFloor = floorRoot.AddComponent<FPInteractiveSurfacePolygon>();
            fpSurfacePolygonFloor.ParentFPPolygon = this;
            fpSurfacePolygonFloor.surfaceShapeDescriptor = polygon.FloorTexture;
            fpSurfacePolygonFloor.FPLight = FPLevel.FPLights[polygon.FloorLight];
            fpSurfacePolygonFloor.FPMedia = hasMedia ? FPLevel.FPMedias[polygon.MediaIndex] : null;
            fpSurfacePolygonFloor.FPPlatform = hasFloorPlatform ? FPLevel.FPFloorFpPlatforms[platformIndex] : null;

            FPLevel.FPInteractiveSurfacePolygons.Add(fpSurfacePolygonFloor);

            // Ceiling
            GeometryUtilities.BuildRendererObject(
                ceilingRoot,
                ceilingVertices,
                ceilingTriangles,
                ceilingUvs,
                polygon.CeilingTexture,
                FPLevel.FPLights[polygon.CeilingLight],
                polygon.CeilingTransferMode,
                ceilingTransferModesVertexColors,
                isOpaqueSurface: true,
                isStaticBatchable: !hasCeilingPlatform);

            var fpSurfacePolygonCeiling = ceilingRoot.AddComponent<FPInteractiveSurfacePolygon>();
            fpSurfacePolygonCeiling.ParentFPPolygon = this;
            fpSurfacePolygonCeiling.surfaceShapeDescriptor = polygon.CeilingTexture;
            fpSurfacePolygonCeiling.FPLight = FPLevel.FPLights[polygon.CeilingLight];
            fpSurfacePolygonCeiling.FPMedia = hasMedia ? FPLevel.FPMedias[polygon.MediaIndex] : null;
            fpSurfacePolygonCeiling.FPPlatform = hasCeilingPlatform ? FPLevel.FPCeilingFpPlatforms[platformIndex] : null;

            FPLevel.FPInteractiveSurfacePolygons.Add(fpSurfacePolygonCeiling);

            // Media
            if (hasMedia)
            {
                #region Infinity_Media_Texture_Assignment
                var mediaShapeDescriptor = new ShapeDescriptor((ushort)polygon.FloorTexture);
                switch (media.Type)
                {
                    case MediaType.Water:
                        mediaShapeDescriptor.Collection = 17;
                        mediaShapeDescriptor.Bitmap = 19;
                        break;
                    case MediaType.Lava:
                        mediaShapeDescriptor.Collection = 18;
                        mediaShapeDescriptor.Bitmap = 12;
                        break;
                    case MediaType.Goo:
                        mediaShapeDescriptor.Collection = 21;
                        mediaShapeDescriptor.Bitmap = 5;
                        break;
                    case MediaType.Sewage:
                        mediaShapeDescriptor.Collection = 19;
                        mediaShapeDescriptor.Bitmap = 13;
                        break;
                    case MediaType.Jjaro:
                        mediaShapeDescriptor.Collection = 20;
                        mediaShapeDescriptor.Bitmap = 13;
                        break;
                }
                #endregion Infinity_Media_Texture_Assignment

                GeometryUtilities.BuildRendererObject(
                    mediaRoot,
                    mediaVertices,
                    mediaTriangles,
                    mediaUvs,
                    mediaShapeDescriptor,
                    FPLevel.FPLights[polygon.MediaLight],
                    transferMode: 0,
                    floorTransferModesVertexColors,
                    isOpaqueSurface: true,
                    isStaticBatchable: false,
                    FPLevel.FPMedias[polygon.MediaIndex]);

                var fpSurfacePolygonMedia = mediaRoot.AddComponent<FPInteractiveSurfaceMedia>();
                fpSurfacePolygonMedia.ParentFPPolygon = this;
                fpSurfacePolygonMedia.FPLight = FPLevel.FPLights[polygon.MediaLight];
                fpSurfacePolygonMedia.FPMedia = FPLevel.FPMedias[polygon.MediaIndex];

                FPLevel.FPInteractiveSurfaceMedias.Add(fpSurfacePolygonMedia);
            }
        }

        private void AssignFloorAndCeilingVertices(int vertexIndex, Polygon polygon, Vector3[] floorVertices, Vector3[] ceilingVertices)
        {
            var endpointIndex = polygon.EndpointIndexes[vertexIndex];

            floorVertices[vertexIndex] = GeometryUtilities.GetMeshVertex(FPLevel.Level, endpointIndex);
            ceilingVertices[vertexIndex] = GeometryUtilities.GetMeshVertex(FPLevel.Level, endpointIndex);
        }

        private void AssignMediaVertex(int vertexIndex, Polygon polygon, Vector3[] mediaVertices)
        {
            var endpointIndex = polygon.EndpointIndexes[vertexIndex];

            mediaVertices[vertexIndex] = GeometryUtilities.GetMeshVertex(FPLevel.Level, endpointIndex);
        }

        private void AssignFloorOrCeilingOrMediaTriangle(int earlyVertexIndex, int lateVertexIndex, int currentTriangleIndex, int[] triangles, bool reverseOrder = false, bool isLateTriangle = false)
        {
            if (isLateTriangle)
            {
                triangles[currentTriangleIndex] = earlyVertexIndex + 1;
                triangles[currentTriangleIndex + 1] = lateVertexIndex - 1;
                triangles[currentTriangleIndex + 2] = lateVertexIndex;
            }
            else
            {
                triangles[currentTriangleIndex] = earlyVertexIndex;
                triangles[currentTriangleIndex + 1] = earlyVertexIndex + 1;
                triangles[currentTriangleIndex + 2] = lateVertexIndex;
            }

            if (reverseOrder)
            {
                var firstIndex = triangles[currentTriangleIndex];
                triangles[currentTriangleIndex] = triangles[currentTriangleIndex + 2];
                triangles[currentTriangleIndex + 2] = firstIndex;
            }
        }

        private void CreateSelectionIndicators(GameObject surface, bool isfloor)
        {
            var vertices = surface.GetComponent<MeshCollider>().sharedMesh.vertices;

            var localToWorldMatrix = surface.transform.localToWorldMatrix;

            for (var i = 0; i < vertices.Length; i++)
            {
                var currentVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i]);
                Vector3 previousVertexWorldPosition;
                Vector3 nextVertexWorldPosition;

                if (isfloor)
                {
                    previousVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i >= 1 ? i - 1 : vertices.Length - 1]);
                    nextVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i < vertices.Length - 1 ? i + 1 : 0]);
                }
                else
                {
                    previousVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i < vertices.Length - 1 ? i + 1 : 0]);
                    nextVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i >= 1 ? i - 1 : vertices.Length - 1]);
                }

                selectionVisualizationIndicators.Add(GeometryUtilities.CreateSurfaceSelectionIndicator($"Vertex ({i})", surface.transform, currentVertexWorldPosition, nextVertexWorldPosition, previousVertexWorldPosition));
            }
        }
    }
}
