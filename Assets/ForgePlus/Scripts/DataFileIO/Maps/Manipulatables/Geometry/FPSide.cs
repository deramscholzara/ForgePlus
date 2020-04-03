using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Runtime.Constraints;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPSide : MonoBehaviour, IFPManipulatable<Side>, IFPSelectable, IFPInspectable
    {
        private enum SideDataSources
        {
            Primary,
            Secondary,
            Transparent,
        }

        public short? Index { get; set; }

        public Side WelandObject { get; set; }

        public GameObject TopSurface;
        public GameObject MiddleSurface;
        public GameObject BottomSurface;

        public FPLevel FPLevel { private get; set; }

        public void OnMouseUpAsButton()
        {
            Debug.LogError("FPSide components should never be directly selected, this shouldn't even have a collider to receive input.", this);
        }

        public void SetSelectability(bool enabled)
        {
            // Intentionally empty - Selectability is handled in FPSurfaceSide
        }

        public void DisplaySelectionState(bool state)
        {
            // TODO: Should actually display it here so it only shows the indicators on the selected side, not the line
            FPLevel.FPLines[WelandObject.LineIndex].DisplaySelectionState(state);
        }

        public void Inspect()
        {
            var prefab = Resources.Load<InspectorFPSide>("Inspectors/Inspector - FPSide");
            var inspector = Instantiate(prefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);

            FPLevel.FPLines[WelandObject.LineIndex].Inspect();
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

            bool touchesAnyPlatformAndIsTwoSided = false;
            Platform facingPlatform = null;
            Platform opposingPlatform = null;
            if (opposingPolygon != null)
            {
                if (facingPolygon.Type == PolygonType.Platform || opposingPolygon.Type == PolygonType.Platform)
                {
                    touchesAnyPlatformAndIsTwoSided = true;

                    if (facingPolygon.Type == PolygonType.Platform)
                    {
                        facingPlatform = GeometryUtilities.GetPlatformForPolygonIndex(fpLevel.Level, facingPolygonIndex);
                    }

                    if (opposingPolygon.Type == PolygonType.Platform)
                    {
                        opposingPlatform = GeometryUtilities.GetPlatformForPolygonIndex(fpLevel.Level, opposingPolygonIndex);
                    }
                }
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

            // Create our own, accurate variation on of Side.Type, since we
            // want to display possibly visible untextured side surfaces.
            var needsTop = hasOpposingPolygon && highestFacingCeiling > lowestOpposingCeiling;
            var needsMiddle = !hasOpposingPolygon || hasTransparentSide || (!line.Transparent && !touchesAnyPlatformAndIsTwoSided);
            var needsBottom = hasOpposingPolygon && lowestFacingFloor < highestOpposingFloor;

            GameObject sideRootGO = null, topSurface = null, middleSurface = null, bottomSurface = null;

            if (needsTop)
            {
                CreateSideRoot(ref sideRootGO, isClockwise, sideIndex);

                // Always Primary
                var sideDataSource = SideDataSources.Primary;

                short lowHeight = lowestOpposingCeiling;
                short highHeight = highestFacingCeiling;

                if (highHeight > lowHeight)
                {
                    topSurface = BuildWallSurface(fpLevel,
                                                  $"Side Top ({sideIndex}) (High - Source:{sideDataSource})",
                                                  lowHeight,
                                                  highHeight,
                                                  line.EndpointIndexes[0],
                                                  line.EndpointIndexes[1],
                                                  isClockwise,
                                                  sideRootGO.transform,
                                                  side,
                                                  side == null ? (short)0 : side.PrimaryTransferMode,
                                                  sideDataSource,
                                                  isOpaqueSurface: true);

                    var fpSurfaceSide = topSurface.AddComponent<FPSurfaceSide>();
                    fpSurfaceSide.parentFPSide = sideRootGO.GetComponent<FPSide>();
                    fpLevel.FPSurfaceSides.Add(fpSurfaceSide);

                    if (opposingPlatform != null && opposingPlatform.ComesFromCeiling)
                    {
                        topSurface.transform.position = new Vector3(0f, (float)(highHeight - lowHeight) / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);

                        ConstrainWallSurfaceToPlatform(fpLevel.FPCeilingFpPlatforms[opposingPolygonIndex], topSurface, isFloorPlatform: false);
                    }
                    else
                    {
                        topSurface.transform.position = new Vector3(0f, (float)highHeight / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);
                    }
                }
            }

            if (needsMiddle)
            {
                CreateSideRoot(ref sideRootGO, isClockwise, sideIndex);

                // Transparent if there's an opposing polygon, Primary otherwise (because it's a "Full", unopposed side)
                var sideDataSource = hasOpposingPolygon ? SideDataSources.Transparent : SideDataSources.Primary;
                var isOpaqueSurface = !hasOpposingPolygon || !isTransparent;

                var typeDescriptor = hasOpposingPolygon ? $"Transparent - HasTransparentSide - Source:{sideDataSource}" : $"Full - Unopposed - Source:{sideDataSource}";

                middleSurface = BuildWallSurface(fpLevel,
                                                 $"Side Middle ({sideIndex}) - ({typeDescriptor})",
                                                 line.HighestAdjacentFloor,
                                                 line.LowestAdjacentCeiling,
                                                 line.EndpointIndexes[0],
                                                 line.EndpointIndexes[1],
                                                 isClockwise,
                                                 sideRootGO.transform,
                                                 side,
                                                 transferMode: side == null ? (short)0 : (sideDataSource == SideDataSources.Primary ? side.PrimaryTransferMode : side.TransparentTransferMode),
                                                 sideDataSource: sideDataSource,
                                                 isOpaqueSurface: isOpaqueSurface);

                var fpSurfaceSide = middleSurface.AddComponent<FPSurfaceSide>();
                fpSurfaceSide.parentFPSide = sideRootGO.GetComponent<FPSide>();
                fpLevel.FPSurfaceSides.Add(fpSurfaceSide);

                middleSurface.transform.position = new Vector3(0f, (float)line.LowestAdjacentCeiling / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);
            }

            if (needsBottom)
            {
                CreateSideRoot(ref sideRootGO, isClockwise, sideIndex);

                // Secondary if there is a top section, Primary otherwise
                var sideDataSource = needsTop ? SideDataSources.Secondary : SideDataSources.Primary;

                short lowHeight = lowestFacingFloor;
                short highHeight = highestOpposingFloor;

                if (highHeight > lowHeight)
                {
                    bottomSurface = BuildWallSurface(fpLevel,
                                                     $"Side Bottom ({sideIndex}) (Low - Source:{sideDataSource})",
                                                     lowHeight,
                                                     highHeight,
                                                     line.EndpointIndexes[0],
                                                     line.EndpointIndexes[1],
                                                     isClockwise,
                                                     sideRootGO.transform,
                                                     side,
                                                     side == null ? (short)0 : (sideDataSource == SideDataSources.Primary ? side.PrimaryTransferMode : side.SecondaryTransferMode),
                                                     sideDataSource,
                                                     isOpaqueSurface: true);

                    var fpSurfaceSide = bottomSurface.AddComponent<FPSurfaceSide>();
                    fpSurfaceSide.parentFPSide = sideRootGO.GetComponent<FPSide>();
                    fpLevel.FPSurfaceSides.Add(fpSurfaceSide);

                    if (opposingPlatform != null && opposingPlatform.ComesFromFloor)
                    {
                        bottomSurface.transform.position = Vector3.zero;

                        ConstrainWallSurfaceToPlatform(fpLevel.FPFloorFpPlatforms[opposingPolygonIndex], bottomSurface, isFloorPlatform: true);
                    }
                    else
                    {
                        bottomSurface.transform.position = new Vector3(0f, (float)highHeight / GeometryUtilities.WorldUnitIncrementsPerMeter, 0f);
                    }
                }
            }

            if (sideRootGO)
            {
                var fpSide = sideRootGO.GetComponent<FPSide>();
                fpSide.Index = sideIndex;
                fpSide.WelandObject = side;
                fpLevel.FPSides[sideIndex] = fpSide;
                fpSide.TopSurface = topSurface;
                fpSide.MiddleSurface = middleSurface;
                fpSide.BottomSurface = bottomSurface;

                fpSide.FPLevel = fpLevel;

                return fpSide;
            }
            else
            {
                return null;
            }
        }

        private static void CreateSideRoot(ref GameObject sideRootGO, bool isClockwise, short sideIndex)
        {
            if (!sideRootGO)
            {
                sideRootGO = new GameObject(isClockwise ? $"Clockwise ({sideIndex})" : $"Counterclockwise ({sideIndex})", typeof(FPSide));
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
            Transform parent,
            Side side,
            short transferMode,
            SideDataSources sideDataSource,
            bool isOpaqueSurface)
        {
            short textureOffsetX = 0;
            short textureOffsetY = 0;
            ShapeDescriptor shapeDescriptor = ShapeDescriptor.Empty;
            Weland.Light light = null;
            short lightIndex = 0;

            if (side != null)
            {
                switch (sideDataSource)
                {
                    case SideDataSources.Primary:
                        textureOffsetX = side.Primary.X;
                        textureOffsetY = side.Primary.Y;
                        shapeDescriptor = side.Primary.Texture;
                        light = fpLevel.Level.Lights[side.PrimaryLightsourceIndex];
                        lightIndex = side.PrimaryLightsourceIndex;
                        break;
                    case SideDataSources.Secondary:
                        textureOffsetX = side.Secondary.X;
                        textureOffsetY = side.Secondary.Y;
                        shapeDescriptor = side.Secondary.Texture;
                        light = fpLevel.Level.Lights[side.SecondaryLightsourceIndex];
                        lightIndex = side.SecondaryLightsourceIndex;
                        break;
                    case SideDataSources.Transparent:
                        textureOffsetX = side.Transparent.X;
                        textureOffsetY = side.Transparent.Y;
                        shapeDescriptor = side.Transparent.Texture;
                        light = fpLevel.Level.Lights[side.TransparentLightsourceIndex];
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

            var sideGameObject = new GameObject(name);
            sideGameObject.transform.SetParent(parent);

            GeometryUtilities.BuildRendererObject(
                sideGameObject,
                vertices,
                triangles,
                uvs,
                shapeDescriptor,
                fpLevel.FPLights[lightIndex],
                lightIndex,
                transferMode,
                transferModesVertexColors,
                isOpaqueSurface);

            return sideGameObject;
        }

        private static void ConstrainWallSurfaceToPlatform(FPPlatform fpPlatform, GameObject wallSurface, bool isFloorPlatform)
        {
            var constraint = wallSurface.AddComponent<PositionConstraint>();
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
