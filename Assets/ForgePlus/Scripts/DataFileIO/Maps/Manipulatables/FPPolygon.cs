using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.ShapesCollections;
using System.Collections.Generic;
using UnityEngine;
using Weland;
using Weland.Extensions;

namespace ForgePlus.LevelManipulation
{
    public class FPPolygon : MonoBehaviour, IFPManipulatable<Polygon>, IFPSelectionDisplayable, IFPInspectable
    {
        public enum DataSources
        {
            Ceiling,
            Floor,
            Media,
        }

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
            var inspectorPrefab = ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry ?
                                  Resources.Load<InspectorBase>("Inspectors/Inspector - FPPolygon") :
                                  Resources.Load<InspectorBase>("Inspectors/Inspector - FPPolygon Textures");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void GenerateSurfaces()
        {
            BuildFloorAndCeiling();
        }

        public void SetOffset(FPInteractiveSurfacePolygon surfaceObject, DataSources surfaceType, short x, short y, bool rebatch)
        {
            switch (surfaceType)
            {
                case DataSources.Ceiling:
                    if (WelandObject.CeilingTransferMode == 9 ||
                        WelandObject.CeilingTexture.UsesLandscapeCollection() ||
                        WelandObject.CeilingTexture.IsEmpty())
                    {
                        // Don't adjust UVs for landscape surfaces.
                        return;
                    }

                    WelandObject.CeilingOrigin.X = x;
                    WelandObject.CeilingOrigin.Y = y;

                    break;
                case DataSources.Floor:
                    if (WelandObject.FloorTransferMode == 9 ||
                        WelandObject.FloorTexture.UsesLandscapeCollection() ||
                        WelandObject.FloorTexture.IsEmpty())
                    {
                        // Don't adjust UVs for landscape surfaces.
                        return;
                    }

                    WelandObject.FloorOrigin.X = x;
                    WelandObject.FloorOrigin.Y = y;

                    break;
                default:
                    return;
            }

            RuntimeSurfaceLight runtimeSurfaceLight = null;

            var shouldRemerge = false;
            if (rebatch)
            {
                runtimeSurfaceLight = surfaceObject.GetComponent<RuntimeSurfaceLight>();
                shouldRemerge = runtimeSurfaceLight.UnmergeBatch();
            }

            var meshUVs = BuildUVs(x, y);
            surfaceObject.GetComponent<MeshFilter>().sharedMesh.SetUVs(channel: 0, meshUVs);

            if (rebatch && shouldRemerge)
            {
                runtimeSurfaceLight.MergeBatch();
            }
        }

        public void SetShapeDescriptor(RuntimeSurfaceLight surfaceLight, DataSources surfaceType, ShapeDescriptor shapeDescriptor)
        {
            short transferMode;

            switch (surfaceType)
            {
                case DataSources.Ceiling:
                    if (shapeDescriptor.Equals(WelandObject.CeilingTexture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    WallsCollection.DecrementTextureUsage(WelandObject.CeilingTexture);

                    WelandObject.CeilingTexture = shapeDescriptor;
                    transferMode = WelandObject.CeilingTransferMode;

                    break;
                case DataSources.Floor:
                    if (shapeDescriptor.Equals(WelandObject.FloorTexture))
                    {
                        // Texture is not different, so exit
                        return;
                    }

                    WallsCollection.DecrementTextureUsage(WelandObject.FloorTexture);

                    WelandObject.FloorTexture = shapeDescriptor;
                    transferMode = WelandObject.FloorTransferMode;

                    break;
                default:
                    return;
            }

            surfaceLight.SetShapeDescriptor(shapeDescriptor,
                                            transferMode,
                                            isOpaqueSurface: true,
                                            WallsCollection.SurfaceTypes.Normal);
        }

        private void BuildFloorAndCeiling()
        {
            #region Generate_Mesh_Data
            var floorRoot = new GameObject($"Floor (polygon: {Index})");
            FloorSurface = floorRoot;
            floorRoot.transform.position = new Vector3(0f, WelandObject.FloorHeight / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);
            floorRoot.transform.SetParent(transform);
            var ceilingRoot = new GameObject($"Ceiling (polygon: {Index})");
            CeilingSurface = ceilingRoot;
            ceilingRoot.transform.position = new Vector3(0f, WelandObject.CeilingHeight / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);
            ceilingRoot.transform.SetParent(transform);

            var floorVertices = new Vector3[WelandObject.VertexCount];
            var ceilingVertices = new Vector3[WelandObject.VertexCount];
            var floorTriangles = new int[(WelandObject.VertexCount - 2) * 3];
            var ceilingTriangles = new int[(WelandObject.VertexCount - 2) * 3];

            #region Media
            var hasMedia = WelandObject.MediaIndex >= 0;
            GameObject mediaRoot = null;
            Media media = null;

            Vector3[] mediaVertices = null;
            int[] mediaTriangles = null;

            if (hasMedia)
            {
                mediaRoot = new GameObject($"Media (polygon: {Index} media:{WelandObject.MediaIndex})");
                mediaRoot.transform.SetParent(floorRoot.transform);

                media = FPLevel.Level.Medias[WelandObject.MediaIndex];

                mediaVertices = new Vector3[WelandObject.VertexCount];
                mediaTriangles = new int[(WelandObject.VertexCount - 2) * 3];
            }
            #endregion Media

            for (int earlyVertexIndex = 0, lateVertexIndex = WelandObject.VertexCount - 1, currentTriangleIndex = 0;
                 earlyVertexIndex <= lateVertexIndex;
                 earlyVertexIndex++, lateVertexIndex--)
            {
                AssignFloorAndCeilingVertices(earlyVertexIndex, WelandObject, floorVertices, ceilingVertices);

                if (hasMedia)
                {
                    AssignMediaVertex(earlyVertexIndex, WelandObject, mediaVertices);
                }

                if (earlyVertexIndex < lateVertexIndex)
                {
                    // Vertex-traversal has not intersected, so continue
                    AssignFloorAndCeilingVertices(lateVertexIndex, WelandObject, floorVertices, ceilingVertices);

                    if (hasMedia)
                    {
                        AssignMediaVertex(lateVertexIndex, WelandObject, mediaVertices);
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
            var ceilingUvs = BuildUVs(WelandObject.CeilingOrigin.X, WelandObject.CeilingOrigin.Y);
            var floorUvs = BuildUVs(WelandObject.FloorOrigin.X, WelandObject.FloorOrigin.Y);

            Vector2[] mediaUvs = null;
            if (hasMedia)
            {
                mediaUvs = BuildUVs(0, 0);
            }
            #endregion UVs
            #endregion Generate_Mesh_Data

            #region Platforms
            var hasCeilingPlatform = false;
            var hasFloorPlatform = false;
            short platformIndex = WelandObject.Permutation;
            if (WelandObject.Type == PolygonType.Platform)
            {
                var platform = GeometryUtilities.GetPlatformForPolygon(FPLevel.Level, this.WelandObject);

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
            var floorTransferModesVertexColors = new Color[WelandObject.VertexCount];
            for (var i = 0; i < WelandObject.VertexCount; i++)
            {
                floorTransferModesVertexColors[i] = GeometryUtilities.GetTransferModeVertexColor(WelandObject.FloorTransferMode, isSideSurface: false);
            }

            var ceilingTransferModesVertexColors = new Color[WelandObject.VertexCount];
            for (var i = 0; i < WelandObject.VertexCount; i++)
            {
                ceilingTransferModesVertexColors[i] = GeometryUtilities.GetTransferModeVertexColor(WelandObject.CeilingTransferMode, isSideSurface: false);
            }
            #endregion TransferModes_VertexColor

            // Floor
            GeometryUtilities.BuildRendererObject(
                floorRoot,
                floorVertices,
                floorTriangles,
                floorUvs,
                WelandObject.FloorTexture,
                FPLevel.FPLights[WelandObject.FloorLight],
                WelandObject.FloorTransferMode,
                floorTransferModesVertexColors,
                isOpaqueSurface: true,
                isStaticBatchable: !hasFloorPlatform);

            var fpSurfacePolygonFloor = floorRoot.AddComponent<FPInteractiveSurfacePolygon>();
            fpSurfacePolygonFloor.ParentFPPolygon = this;
            fpSurfacePolygonFloor.DataSource = DataSources.Floor;
            fpSurfacePolygonFloor.surfaceShapeDescriptor = WelandObject.FloorTexture;
            fpSurfacePolygonFloor.FPLight = FPLevel.FPLights[WelandObject.FloorLight];
            fpSurfacePolygonFloor.FPMedia = hasMedia ? FPLevel.FPMedias[WelandObject.MediaIndex] : null;
            fpSurfacePolygonFloor.FPPlatform = hasFloorPlatform ? FPLevel.FPFloorFpPlatforms[platformIndex] : null;

            FPLevel.FPInteractiveSurfacePolygons.Add(fpSurfacePolygonFloor);

            // Ceiling
            GeometryUtilities.BuildRendererObject(
                ceilingRoot,
                ceilingVertices,
                ceilingTriangles,
                ceilingUvs,
                WelandObject.CeilingTexture,
                FPLevel.FPLights[WelandObject.CeilingLight],
                WelandObject.CeilingTransferMode,
                ceilingTransferModesVertexColors,
                isOpaqueSurface: true,
                isStaticBatchable: !hasCeilingPlatform);

            var fpSurfacePolygonCeiling = ceilingRoot.AddComponent<FPInteractiveSurfacePolygon>();
            fpSurfacePolygonCeiling.ParentFPPolygon = this;
            fpSurfacePolygonCeiling.DataSource = DataSources.Ceiling;
            fpSurfacePolygonCeiling.surfaceShapeDescriptor = WelandObject.CeilingTexture;
            fpSurfacePolygonCeiling.FPLight = FPLevel.FPLights[WelandObject.CeilingLight];
            fpSurfacePolygonCeiling.FPMedia = hasMedia ? FPLevel.FPMedias[WelandObject.MediaIndex] : null;
            fpSurfacePolygonCeiling.FPPlatform = hasCeilingPlatform ? FPLevel.FPCeilingFpPlatforms[platformIndex] : null;

            FPLevel.FPInteractiveSurfacePolygons.Add(fpSurfacePolygonCeiling);

            // Media
            if (hasMedia)
            {
                #region Infinity_Media_Texture_Assignment
                var mediaShapeDescriptor = new ShapeDescriptor((ushort)WelandObject.FloorTexture);
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
                    FPLevel.FPLights[WelandObject.MediaLight],
                    FPLevel.FPMedias[WelandObject.MediaIndex]);

                var fpSurfacePolygonMedia = mediaRoot.AddComponent<FPInteractiveSurfaceMedia>();
                fpSurfacePolygonMedia.ParentFPPolygon = this;
                fpSurfacePolygonMedia.FPLight = FPLevel.FPLights[WelandObject.MediaLight];
                fpSurfacePolygonMedia.FPMedia = FPLevel.FPMedias[WelandObject.MediaIndex];

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

        private Vector2[] BuildUVs(short x, short y)
        {
            var meshUVs = new Vector2[WelandObject.VertexCount];

            for (var i = 0; i < WelandObject.VertexCount; i++)
            {
                var vertexPosition = GeometryUtilities.GetMeshVertex(FPLevel.Level, WelandObject.EndpointIndexes[i]);

                var u = -(vertexPosition.z * GeometryUtilities.MeterToWorldUnit);
                var v = -(vertexPosition.x * GeometryUtilities.MeterToWorldUnit);
                var floorOffset = new Vector2(y / GeometryUtilities.WorldUnitIncrementsPerWorldUnit,
                                              -x / GeometryUtilities.WorldUnitIncrementsPerWorldUnit);
                meshUVs[i] = new Vector2(u, v) + floorOffset;
            }

            return meshUVs;
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
