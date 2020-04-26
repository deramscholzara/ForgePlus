using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Runtime.Constraints;
using System.Collections.Generic;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPSide : MonoBehaviour, IFPManipulatable<Side>, IFPSelectionDisplayable, IFPInspectable
    {
        private enum SideDataSources
        {
            Primary,
            Secondary,
            Transparent,
        }

        public short Index { get; set; }

        public Side WelandObject { get; set; }

        public GameObject TopSurface;
        public GameObject MiddleSurface;
        public GameObject BottomSurface;

        private List<GameObject> selectionVisualizationIndicators = new List<GameObject>(4);

        public FPLevel FPLevel { private get; set; }

        public void SetSelectability(bool enabled)
        {
            // Intentionally empty - Selectability is handled in FPSurfaceSide
        }

        public void DisplaySelectionState(bool state)
        {
            if (state)
            {
                bool collectedTopSurface = false;
                Vector3 topLeftWorldPosition = Vector3.zero;
                Vector3 topRightWorldPosition = Vector3.zero;
                Vector3 bottomRightWorldPosition = Vector3.zero;
                Vector3 bottomLeftWorldPosition = Vector3.zero;
                GameObject topParent = null;
                GameObject bottomParent = null;

                if (TopSurface)
                {
                    var localToWorldMatrix = TopSurface.transform.localToWorldMatrix;
                    var mesh = TopSurface.GetComponent<MeshCollider>().sharedMesh;

                    topLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[1]);
                    topRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[2]);

                    topParent = TopSurface;

                    bottomRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[3]);
                    bottomLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[0]);

                    bottomParent = TopSurface;

                    collectedTopSurface = true;
                }

                if (MiddleSurface)
                {
                    var localToWorldMatrix = MiddleSurface.transform.localToWorldMatrix;
                    var mesh = MiddleSurface.GetComponent<MeshCollider>().sharedMesh;

                    if (!collectedTopSurface)
                    {
                        topLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[1]);
                        topRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[2]);

                        topParent = MiddleSurface;

                        collectedTopSurface = true;
                    }

                    bottomRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[3]);
                    bottomLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[0]);

                    bottomParent = MiddleSurface;
                }

                if (BottomSurface)
                {
                    var localToWorldMatrix = BottomSurface.transform.localToWorldMatrix;
                    var mesh = BottomSurface.GetComponent<MeshCollider>().sharedMesh;

                    if (!collectedTopSurface)
                    {
                        topLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[1]);
                        topRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[2]);

                        topParent = BottomSurface;
                    }

                    bottomRightWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[3]);
                    bottomLeftWorldPosition = localToWorldMatrix.MultiplyPoint(mesh.vertices[0]);

                    bottomParent = BottomSurface;
                }

                selectionVisualizationIndicators.Add(GeometryUtilities.CreateSurfaceSelectionIndicator("Top-Left", topParent.transform, topLeftWorldPosition, topRightWorldPosition, bottomLeftWorldPosition));
                selectionVisualizationIndicators.Add(GeometryUtilities.CreateSurfaceSelectionIndicator("Top-Right", topParent.transform, topRightWorldPosition, bottomRightWorldPosition, topLeftWorldPosition));
                selectionVisualizationIndicators.Add(GeometryUtilities.CreateSurfaceSelectionIndicator("Bottom-Right", bottomParent.transform, bottomRightWorldPosition, bottomLeftWorldPosition, topRightWorldPosition));
                selectionVisualizationIndicators.Add(GeometryUtilities.CreateSurfaceSelectionIndicator("Bottom-Left", bottomParent.transform, bottomLeftWorldPosition, topLeftWorldPosition, bottomRightWorldPosition));
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
            var inspectorPrefab = SelectionManager.Instance.CurrentSceneSelectionFilter == SelectionManager.SceneSelectionFilters.Geometry ?
                                  Resources.Load<InspectorBase>("Inspectors/Inspector - FPSide") :
                                  Resources.Load<InspectorBase>("Inspectors/Inspector - FPSide Textures");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);

            if (SelectionManager.Instance.CurrentSceneSelectionFilter == SelectionManager.SceneSelectionFilters.Geometry)
            {
                FPLevel.FPLines[WelandObject.LineIndex].Inspect();
            }
        }

        public static FPSide GenerateSurfaces(FPLevel fpLevel, bool isClockwise, Line line)
        {
            var sideIndex = isClockwise ? line.ClockwisePolygonSideIndex : line.CounterclockwisePolygonSideIndex;

            // Note: A null-side may still be created as an untextured side
            var side = sideIndex < fpLevel.Level.Sides.Count && sideIndex >= 0 ? fpLevel.Level.Sides[sideIndex] : null;

            // Get the relevant (and usually accurate) Line Flags
            // Notes on all Line Flag options:
            //  - HasTransparentSide
            //      - The pass-through section (goes into another polygon) is texture-assignable.
            //      - It might not have a texture assigned, though.
            //      - It might not actually be "Transparent" (allows rendering into the next poly... a.k.a. portal can see through).
            //  - VariableElevation
            //      - One of the adjacent polygons is a platform.
            //  - Elevation
            //      - One adjacent polygon's floor is higher than the other.
            //      - If only the ceiling is lower, then this flag doesn't appear.
            //      - Really only used for map-drawing.
            //  - Landscape
            //      - It'll do the weird landscape-y skybox effect.
            //  - Transparent
            //      - The pass-through section can be seen through.
            //      - This is most "unseen" lines connecting polygons.
            //      - Makes textured HasTransparentSide lines behave transparently.
            //      - You won't actually see through it if it uses HasTransparentSide and has a fully opaque texture assigned.
            //  - Solid
            //      - You can't walk through it.
            var hasTransparentSide = (line.Flags & LineFlags.HasTransparentSide) != 0;
            var isTransparent = line.Transparent;

            var facingPolygonIndex = isClockwise ? line.ClockwisePolygonOwner : line.CounterclockwisePolygonOwner;

            if (facingPolygonIndex < 0)
            {
                return null;
            }

            var opposingPolygonIndex = !isClockwise ? line.ClockwisePolygonOwner : line.CounterclockwisePolygonOwner;

            var hasOpposingPolygon = opposingPolygonIndex >= 0;

            var facingPolygon = fpLevel.Level.Polygons[facingPolygonIndex];
            var opposingPolygon = hasOpposingPolygon ? fpLevel.Level.Polygons[opposingPolygonIndex] : null;

            Platform facingPlatform = null;
            Platform opposingPlatform = null;
            facingPlatform = GeometryUtilities.GetPlatformForPolygon(fpLevel.Level, facingPolygon);
            if (opposingPolygon != null)
            {
                opposingPlatform = GeometryUtilities.GetPlatformForPolygon(fpLevel.Level, opposingPolygon);
            }

            var highestFacingCeiling = facingPolygon.CeilingHeight;
            if (facingPlatform != null && facingPlatform.ComesFromCeiling)
            {
                if (facingPlatform.ComesFromFloor)
                {
                    var roundedUpMidpoint = (short)Mathf.CeilToInt((facingPlatform.MinimumHeight + facingPlatform.MaximumHeight) * 0.5f);
                    highestFacingCeiling = (short)Mathf.Max(highestFacingCeiling, roundedUpMidpoint);
                }
                else
                {
                    highestFacingCeiling = (short)Mathf.Max(highestFacingCeiling, facingPlatform.MaximumHeight);
                }
            }

            var lowestOpposingCeiling = hasOpposingPolygon ? opposingPolygon.CeilingHeight : highestFacingCeiling;
            if (opposingPlatform != null && opposingPlatform.ComesFromCeiling)
            {
                if (opposingPlatform.ComesFromFloor)
                {
                    var roundedDownMidpoint = (short)Mathf.FloorToInt((opposingPlatform.MinimumHeight + opposingPlatform.MaximumHeight) * 0.5f);
                    lowestOpposingCeiling = (short)Mathf.Min(lowestOpposingCeiling, roundedDownMidpoint);
                }
                else
                {
                    lowestOpposingCeiling = (short)Mathf.Min(lowestOpposingCeiling, opposingPlatform.MinimumHeight);
                }
            }

            var lowestFacingFloor = facingPolygon.FloorHeight;
            if (facingPlatform != null && facingPlatform.ComesFromFloor)
            {
                if (facingPlatform.ComesFromCeiling)
                {
                    var roundedDownMidpoint = (short)Mathf.FloorToInt((facingPlatform.MinimumHeight + facingPlatform.MaximumHeight) * 0.5f);
                    lowestFacingFloor = (short)Mathf.Min(lowestFacingFloor, roundedDownMidpoint);
                }
                else
                {
                    lowestFacingFloor = (short)Mathf.Min(lowestFacingFloor, facingPlatform.MinimumHeight);
                }
            }

            var highestOpposingFloor = hasOpposingPolygon ? opposingPolygon.FloorHeight : lowestFacingFloor;
            if (opposingPlatform != null && opposingPlatform.ComesFromFloor)
            {
                if (opposingPlatform.ComesFromCeiling)
                {
                    var roundedUpMidpoint = (short)Mathf.CeilToInt((opposingPlatform.MinimumHeight + opposingPlatform.MaximumHeight) * 0.5f);
                    highestOpposingFloor = (short)Mathf.Max(highestOpposingFloor, roundedUpMidpoint);
                }
                else
                {
                    highestOpposingFloor = (short)Mathf.Max(highestOpposingFloor, opposingPlatform.MaximumHeight);
                }
            }

            // Data-driven surface-exposure
            var dataExpectsFullSide = !hasTransparentSide && side != null && side.Type == SideType.Full && (ushort)side.Primary.Texture != (ushort)ShapeDescriptor.Empty;
            var dataExpectsTop = !dataExpectsFullSide && side != null && (side.Type == SideType.High || side.Type == SideType.Split);

            // Geometry-driven surface-exposure
            var exposesTop = !dataExpectsFullSide &&
                             hasOpposingPolygon &&
                             highestFacingCeiling > lowestOpposingCeiling;

            var exposesMiddle = !hasOpposingPolygon ||
                                hasTransparentSide ||
                                dataExpectsFullSide;

            var exposesBottom = !dataExpectsFullSide &&
                                hasOpposingPolygon &&
                                highestOpposingFloor > lowestFacingFloor;

            FPSide fpSide = null;

            if (exposesTop)
            {
                var isPartOfCeilingPlatform = opposingPlatform != null && opposingPlatform.ComesFromCeiling;
                short platformIndex = opposingPolygon.Permutation;

                // Top is always Primary
                var sideDataSource = SideDataSources.Primary;

                var highHeight = highestFacingCeiling;
                var lowHeight = lowestOpposingCeiling;

                CreateSideRoot(ref fpSide, isClockwise, sideIndex, side, fpLevel);

                var surface = BuildWallSurface(fpLevel,
                                                $"Side Top ({sideIndex}) (High - Source:{sideDataSource})",
                                                lowHeight,
                                                highHeight,
                                                line.EndpointIndexes[0],
                                                line.EndpointIndexes[1],
                                                isClockwise,
                                                fpSide,
                                                side == null ? (short)0 : side.PrimaryTransferMode,
                                                sideDataSource,
                                                isOpaqueSurface: true,
                                                facingPolygonIndex,
                                                isStaticBatchable: !isPartOfCeilingPlatform,
                                                fpPlatform: isPartOfCeilingPlatform ? fpLevel.FPCeilingFpPlatforms[platformIndex] : null);

                if (isPartOfCeilingPlatform)
                {
                    surface.transform.position = new Vector3(0f, (float)(highHeight - lowHeight) / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);

                    ConstrainWallSurfaceToPlatform(fpLevel.FPCeilingFpPlatforms[platformIndex], surface, isFloorPlatform: false);
                }
                else
                {
                    surface.transform.position = new Vector3(0f, (float)highHeight / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);
                }

                fpSide.TopSurface = surface;
            }

            if (exposesMiddle)
            {
                // Transparent if there's an opposing polygon, Primary otherwise (because it's a "Full", unopposed side)
                var sideDataSource = !hasOpposingPolygon || dataExpectsFullSide ? SideDataSources.Primary : SideDataSources.Transparent;
                var isOpaqueSurface = !hasOpposingPolygon || !isTransparent;

                var highHeight = dataExpectsFullSide ? highestFacingCeiling : line.LowestAdjacentCeiling;
                var lowHeight = dataExpectsFullSide ? lowestFacingFloor : line.HighestAdjacentFloor;

                var typeDescriptor = hasOpposingPolygon ? $"Transparent - HasTransparentSide - Source:{sideDataSource}" : $"Full - Unopposed - Source:{sideDataSource}";

                if (highestFacingCeiling > lowestFacingFloor &&
                    (lowestOpposingCeiling > highestOpposingFloor || dataExpectsFullSide))
                {
                    CreateSideRoot(ref fpSide, isClockwise, sideIndex, side, fpLevel);

                    var surface = BuildWallSurface(fpLevel,
                                                   $"Side Middle ({sideIndex}) - ({typeDescriptor})",
                                                   lowHeight,
                                                   highHeight,
                                                   line.EndpointIndexes[0],
                                                   line.EndpointIndexes[1],
                                                   isClockwise,
                                                   fpSide,
                                                   transferMode: side == null ? (short)0 : (sideDataSource == SideDataSources.Primary ? side.PrimaryTransferMode : side.TransparentTransferMode),
                                                   sideDataSource: sideDataSource,
                                                   isOpaqueSurface: isOpaqueSurface,
                                                   facingPolygonIndex,
                                                   isStaticBatchable: true,
                                                   fpPlatform: null);

                    surface.transform.position = new Vector3(0f, (float)highHeight / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);

                    fpSide.MiddleSurface = surface;
                }
            }

            if (exposesBottom)
            {
                var isPartOfFloorPlatform = opposingPlatform != null && opposingPlatform.ComesFromFloor;
                short platformIndex = opposingPolygon.Permutation;

                // Secondary if there is an exposable or expected (in data) top section
                var sideDataSource = dataExpectsTop ? SideDataSources.Secondary : SideDataSources.Primary;

                var highHeight = highestOpposingFloor;
                var lowHeight = lowestFacingFloor;

                CreateSideRoot(ref fpSide, isClockwise, sideIndex, side, fpLevel);

                var surface = BuildWallSurface(fpLevel,
                                                $"Side Bottom ({sideIndex}) (Low - Source:{sideDataSource})",
                                                lowHeight,
                                                highHeight,
                                                line.EndpointIndexes[0],
                                                line.EndpointIndexes[1],
                                                isClockwise,
                                                fpSide,
                                                side == null ? (short)0 : (sideDataSource == SideDataSources.Primary ? side.PrimaryTransferMode : side.SecondaryTransferMode),
                                                sideDataSource,
                                                isOpaqueSurface: true,
                                                facingPolygonIndex,
                                                isStaticBatchable: !isPartOfFloorPlatform,
                                                fpPlatform: isPartOfFloorPlatform ? fpLevel.FPFloorFpPlatforms[platformIndex] : null);

                if (isPartOfFloorPlatform)
                {
                    surface.transform.position = Vector3.zero;

                    ConstrainWallSurfaceToPlatform(fpLevel.FPFloorFpPlatforms[platformIndex], surface, isFloorPlatform: true);
                }
                else
                {
                    surface.transform.position = new Vector3(0f, (float)highHeight / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);
                }

                fpSide.BottomSurface = surface;
            }

            return fpSide;
        }

        private static void CreateSideRoot(ref FPSide fpSide, bool isClockwise, short sideIndex, Side side, FPLevel fpLevel)
        {
            if (!fpSide)
            {
                var fpSideGO = new GameObject(isClockwise ? $"Clockwise ({sideIndex})" : $"Counterclockwise ({sideIndex})");
                fpSide = fpSideGO.AddComponent<FPSide>();
                fpSide.Index = sideIndex;
                fpSide.WelandObject = side;
                fpSide.FPLevel = fpLevel;

                fpLevel.FPSides[sideIndex] = fpSide;
            }
        }

        private static GameObject BuildWallSurface(
            FPLevel fpLevel,
            string name,
            short lowHeight,
            short highHeight,
            int endpointIndexA,
            int endpointIndexB,
            bool isClockwiseSide,
            FPSide fpSide,
            short transferMode,
            SideDataSources sideDataSource,
            bool isOpaqueSurface,
            short facingPolygonIndex,
            bool isStaticBatchable,
            FPPlatform fpPlatform)
        {
            var side = fpSide.WelandObject;

            short textureOffsetX = 0;
            short textureOffsetY = 0;
            ShapeDescriptor shapeDescriptor = ShapeDescriptor.Empty;
            short lightIndex = 0;

            if (side != null)
            {
                switch (sideDataSource)
                {
                    case SideDataSources.Primary:
                        textureOffsetX = side.Primary.X;
                        textureOffsetY = side.Primary.Y;
                        shapeDescriptor = side.Primary.Texture;
                        lightIndex = side.PrimaryLightsourceIndex;
                        break;
                    case SideDataSources.Secondary:
                        textureOffsetX = side.Secondary.X;
                        textureOffsetY = side.Secondary.Y;
                        shapeDescriptor = side.Secondary.Texture;
                        lightIndex = side.SecondaryLightsourceIndex;
                        break;
                    case SideDataSources.Transparent:
                        textureOffsetX = side.Transparent.X;
                        textureOffsetY = side.Transparent.Y;
                        shapeDescriptor = side.Transparent.Texture;
                        lightIndex = side.TransparentLightsourceIndex;
                        break;
                }
            }

            #region Vertices
            var bottomPosition = (short)(lowHeight - highHeight);

            if (!isClockwiseSide)
            {
                var originalA = endpointIndexA;
                endpointIndexA = endpointIndexB;
                endpointIndexB = originalA;
            }

            var vertices = new Vector3[]
            {
                GeometryUtilities.GetMeshVertex(fpLevel.Level, endpointIndexA, bottomPosition),
                GeometryUtilities.GetMeshVertex(fpLevel.Level, endpointIndexA),
                GeometryUtilities.GetMeshVertex(fpLevel.Level, endpointIndexB),
                GeometryUtilities.GetMeshVertex(fpLevel.Level, endpointIndexB, bottomPosition)
            };
            #endregion Vertices

            #region Triangles
            var triangles = new int[6];

            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 2;
            triangles[4] = 3;
            triangles[5] = 0;
            #endregion Triangles

            #region UVs
            var uvs = new Vector2[vertices.Length];

            var wallDimensions = new Vector2((vertices[3] - vertices[0]).magnitude, vertices[1].y - vertices[0].y) * GeometryUtilities.MeterToWorldUnit;
            var offset = new Vector2(textureOffsetX, -textureOffsetY) / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;

            // Note: Marathon uses a Top-Left Origin, so that is compensated for here.
            var uvOrigin = Vector2.up + offset;
            var uvFar = new Vector2(uvOrigin.x + wallDimensions.x, uvOrigin.y - wallDimensions.y);

            uvs[0] = new Vector2(uvOrigin.x, uvFar.y);
            uvs[1] = uvOrigin;
            uvs[2] = new Vector2(uvFar.x, uvOrigin.y);
            uvs[3] = uvFar;
            #endregion UVs

            #region TransferModes_VertexColor
            var transferModesVertexColor = GeometryUtilities.GetTransferModeVertexColor(transferMode, isSideSurface: true);
            var transferModesVertexColors = new Color[]
            {
                transferModesVertexColor,
                transferModesVertexColor,
                transferModesVertexColor,
                transferModesVertexColor
            };
            #endregion TransferModes_VertexColor

            var surfaceGameObject = new GameObject(name);
            surfaceGameObject.transform.SetParent(fpSide.transform);

            GeometryUtilities.BuildRendererObject(
                surfaceGameObject,
                vertices,
                triangles,
                uvs,
                shapeDescriptor,
                fpLevel.FPLights[lightIndex],
                transferMode,
                transferModesVertexColors,
                isOpaqueSurface,
                isStaticBatchable);

            var fpSurfaceSide = surfaceGameObject.AddComponent<FPInteractiveSurfaceSide>();
            fpSurfaceSide.surfaceShapeDescriptor = shapeDescriptor;
            fpSurfaceSide.ParentFPSide = fpSide;
            fpSurfaceSide.FPLight = fpLevel.FPLights[lightIndex];
            fpSurfaceSide.FPPlatform = fpPlatform;

            var mediaIndex = fpLevel.FPPolygons[facingPolygonIndex].WelandObject.MediaIndex;
            fpSurfaceSide.FPMedia = mediaIndex >= 0 ? fpLevel.FPMedias[mediaIndex] : null;

            fpLevel.FPInteractiveSurfaceSides.Add(fpSurfaceSide);

            return surfaceGameObject;
        }

        private static void ConstrainWallSurfaceToPlatform(FPPlatform fpPlatform, GameObject wallSurface, bool isFloorPlatform)
        {
            var constraint = wallSurface.AddComponent<FPPositionConstraint>();
            constraint.Parent = fpPlatform.transform;

            if (isFloorPlatform)
            {
                constraint.WorldOffsetFromParent = Vector3.zero;
            }
            else
            {
                constraint.WorldOffsetFromParent = wallSurface.transform.position;
            }

            constraint.ApplyConstraint();

            fpPlatform.BeginRuntimeStyleBehavior();
        }
    }
}
