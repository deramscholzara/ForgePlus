using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using Weland;
using Weland.Extensions;

namespace RuntimeCore.Entities.Geometry
{
    public partial class LevelEntity_Side : LevelEntity_GeometryBase
    {
        public enum DataSources
        {
            Primary,
            Secondary,
            Transparent,
        }

        public new Side NativeObject => base.NativeObject as Side;

        public RuntimeSurfaceGeometry TopSurface { get; private set; }
        public RuntimeSurfaceGeometry MiddleSurface { get; private set; }
        public RuntimeSurfaceGeometry BottomSurface { get; private set; }

        public RuntimeSurfaceGeometry PrimarySurface { get; private set; }
        public RuntimeSurfaceGeometry SecondarySurface { get; private set; }
        public RuntimeSurfaceGeometry TransparentSurface { get; private set; }

        public short PrimaryHighElevation { get; private set; }
        public short SecondaryHighElevation { get; private set; }
        public short TransparentHighElevation { get; private set; }

        public short PrimaryLowElevation { get; private set; }
        public short SecondaryLowElevation { get; private set; }
        public short TransparentLowElevation { get; private set; }

        public static LevelEntity_Side AssembleEntity(LevelEntity_Level level, bool isClockwise, Line line)
        {
            var sideIndex = isClockwise ? line.ClockwisePolygonSideIndex : line.CounterclockwisePolygonSideIndex;

            // Note: A null-side may still be created as an untextured side
            var side = sideIndex < level.Level.Sides.Count && sideIndex >= 0 ? level.Level.Sides[sideIndex] : null;

            if (side == null)
            {
                return null; // TODO: should this stay this way?
            }

            var hasTransparentSide = (line.Flags & LineFlags.HasTransparentSide) != 0;

            var facingPolygonIndex = side.PolygonIndex;

            if (facingPolygonIndex < 0)
            {
                return null;
            }

            var opposingPolygonIndex = side.OpposingPolygonIndex(level.Level);

            var hasOpposingPolygon = side.HasOpposingPolygon(level.Level);

            var facingPolygon = level.Level.Polygons[facingPolygonIndex];
            var opposingPolygon = hasOpposingPolygon ? level.Level.Polygons[opposingPolygonIndex] : null;

            Platform facingPlatform;
            facingPlatform = GeometryUtilities.GetPlatformForPolygon(level.Level, facingPolygon);

            Platform opposingPlatform = null;
            if (opposingPolygon != null)
            {
                opposingPlatform = GeometryUtilities.GetPlatformForPolygon(level.Level, opposingPolygon);
            }

            var highestFacingCeiling = facingPolygon.CeilingHeight;
            if (facingPlatform != null && facingPlatform.ComesFromCeiling)
            {
                if (facingPlatform.ComesFromFloor)
                {
                    // Split platform
                    var roundedUpMidpoint = (short)Mathf.CeilToInt((facingPlatform.RuntimeMinimumHeight(level.Level) + facingPlatform.RuntimeMaximumHeight(level.Level)) * 0.5f);
                    highestFacingCeiling = (short)Mathf.Max(highestFacingCeiling, roundedUpMidpoint);
                }
                else
                {
                    highestFacingCeiling = (short)Mathf.Max(highestFacingCeiling, facingPlatform.RuntimeMaximumHeight(level.Level));
                }
            }

            var lowestFacingFloor = facingPolygon.FloorHeight;
            if (facingPlatform != null && facingPlatform.ComesFromFloor)
            {
                if (facingPlatform.ComesFromCeiling)
                {
                    // Split platform
                    var roundedDownMidpoint = (short)Mathf.FloorToInt((facingPlatform.RuntimeMinimumHeight(level.Level) + facingPlatform.RuntimeMaximumHeight(level.Level)) * 0.5f);
                    lowestFacingFloor = (short)Mathf.Min(lowestFacingFloor, roundedDownMidpoint);
                }
                else
                {
                    lowestFacingFloor = (short)Mathf.Min(lowestFacingFloor, facingPlatform.RuntimeMinimumHeight(level.Level));
                }
            }

            var lowestOpposingCeiling = hasOpposingPolygon ? opposingPolygon.CeilingHeight : highestFacingCeiling;
            var highestOpposingCeiling = hasOpposingPolygon ? opposingPolygon.CeilingHeight : highestFacingCeiling;
            if (opposingPlatform != null && opposingPlatform.ComesFromCeiling)
            {
                if (opposingPlatform.ComesFromFloor)
                {
                    // Split platform
                    var roundedDownMidpoint = (short)Mathf.FloorToInt((opposingPlatform.RuntimeMinimumHeight(level.Level) + opposingPlatform.RuntimeMaximumHeight(level.Level)) * 0.5f);
                    lowestOpposingCeiling = (short)Mathf.Min(lowestOpposingCeiling, roundedDownMidpoint);
                }
                else
                {
                    lowestOpposingCeiling = (short)Mathf.Min(lowestOpposingCeiling, opposingPlatform.RuntimeMinimumHeight(level.Level));
                }

                highestOpposingCeiling = opposingPlatform.RuntimeMaximumHeight(level.Level);
            }

            var highestOpposingFloor = hasOpposingPolygon ? opposingPolygon.FloorHeight : lowestFacingFloor;
            var lowestOpposingFloor = hasOpposingPolygon ? opposingPolygon.FloorHeight : lowestFacingFloor;
            if (opposingPlatform != null && opposingPlatform.ComesFromFloor)
            {
                if (opposingPlatform.ComesFromCeiling)
                {
                    // Split platform
                    var roundedUpMidpoint = (short)Mathf.CeilToInt((opposingPlatform.RuntimeMinimumHeight(level.Level) + opposingPlatform.RuntimeMaximumHeight(level.Level)) * 0.5f);
                    highestOpposingFloor = (short)Mathf.Max(highestOpposingFloor, roundedUpMidpoint);
                }
                else
                {
                    highestOpposingFloor = (short)Mathf.Max(highestOpposingFloor, opposingPlatform.RuntimeMaximumHeight(level.Level));
                }

                lowestOpposingFloor = opposingPlatform.RuntimeMinimumHeight(level.Level);
            }

            // Data-driven surface-exposure
            var dataExpectsFullSide = side != null && side.Type == SideType.Full && !side.Primary.Texture.IsEmpty();
            var dataExpectsTop = !dataExpectsFullSide && side != null && (side.Type == SideType.High || side.Type == SideType.Split);

            // Geometry-driven surface-exposure
            var exposesTop = !dataExpectsFullSide &&
                             hasOpposingPolygon &&
                             highestFacingCeiling > lowestOpposingCeiling;

            var exposesMiddle = (!hasOpposingPolygon ||
                                hasTransparentSide ||
                                dataExpectsFullSide) &&
                                highestFacingCeiling > lowestFacingFloor &&
                                (highestOpposingCeiling > lowestOpposingFloor || dataExpectsFullSide);

            var exposesBottom = !dataExpectsFullSide &&
                                hasOpposingPolygon &&
                                highestOpposingFloor > lowestFacingFloor;

            LevelEntity_Side runtimeSide = null;

            if (exposesTop)
            {
                // Top is always Primary
                var sideDataSource = DataSources.Primary;

                var highHeight = highestFacingCeiling;
                var lowHeight = lowestOpposingCeiling;

                CreateSideRoot(ref runtimeSide, isClockwise, sideIndex, side, level);

                runtimeSide.PrimaryHighElevation = highHeight;
                runtimeSide.PrimaryLowElevation = lowHeight;

                var surface = new GameObject($"Side Top ({sideIndex}) (High - Source:{sideDataSource})").AddComponent<RuntimeSurfaceGeometry>();
                surface.transform.SetParent(runtimeSide.transform);
                surface.InitializeRuntimeSurface(runtimeSide, sideDataSource);

                runtimeSide.PrimarySurface = surface;
                runtimeSide.TopSurface = surface;
            }

            if (exposesMiddle)
            {
                // Primary if there's no opposing polygon or it's explicitly "full", Transparent otherwise
                var sideDataSource = (!hasOpposingPolygon || dataExpectsFullSide) ? DataSources.Primary : DataSources.Transparent;

                var hasLayeredTransparentSide = side.HasLayeredTransparentSide(level.Level);
                var highHeight = dataExpectsFullSide ? highestFacingCeiling : line.LowestAdjacentCeiling;
                var lowHeight = dataExpectsFullSide ? lowestFacingFloor : line.HighestAdjacentFloor;

                var typeDescriptor = hasOpposingPolygon ? $"Transparent - HasTransparentSide - Source:{sideDataSource}" : $"Full - Unopposed - Source:{sideDataSource}";

                CreateSideRoot(ref runtimeSide, isClockwise, sideIndex, side, level);

                if (sideDataSource == DataSources.Primary)
                {
                    runtimeSide.PrimaryHighElevation = highHeight;
                    runtimeSide.PrimaryLowElevation = lowHeight;
                }

                if (sideDataSource == DataSources.Transparent || hasLayeredTransparentSide)
                {
                    runtimeSide.TransparentHighElevation = highHeight;
                    runtimeSide.TransparentLowElevation = lowHeight;
                }

                var surface = new GameObject($"Side Middle ({sideIndex}) - ({typeDescriptor})").AddComponent<RuntimeSurfaceGeometry>();
                surface.transform.SetParent(runtimeSide.transform);
                surface.InitializeRuntimeSurface(runtimeSide, sideDataSource);

                if (sideDataSource == DataSources.Primary)
                {
                    runtimeSide.PrimarySurface = surface;
                }

                if (sideDataSource == DataSources.Transparent || hasLayeredTransparentSide)
                {
                    runtimeSide.TransparentSurface = surface;
                }

                runtimeSide.MiddleSurface = surface;
            }

            if (exposesBottom)
            {
                // Secondary if there is an exposable or expected (in data) top section
                var sideDataSource = dataExpectsTop ? DataSources.Secondary : DataSources.Primary;

                var highHeight = highestOpposingFloor;
                var lowHeight = lowestFacingFloor;

                CreateSideRoot(ref runtimeSide, isClockwise, sideIndex, side, level);

                if (sideDataSource == DataSources.Primary)
                {
                    runtimeSide.PrimaryHighElevation = highHeight;
                    runtimeSide.PrimaryLowElevation = lowHeight;
                }
                else
                {
                    runtimeSide.SecondaryHighElevation = highHeight;
                    runtimeSide.SecondaryLowElevation = lowHeight;
                }

                var surface = new GameObject($"Side Bottom ({sideIndex}) (Low - Source:{sideDataSource})").AddComponent<RuntimeSurfaceGeometry>();
                surface.transform.SetParent(runtimeSide.transform);
                surface.InitializeRuntimeSurface(runtimeSide, sideDataSource);

                if (sideDataSource == DataSources.Primary)
                {
                    runtimeSide.PrimarySurface = surface;
                }
                else
                {
                    runtimeSide.SecondarySurface = surface;
                }

                runtimeSide.BottomSurface = surface;
            }

            return runtimeSide;
        }

        private static void CreateSideRoot(ref LevelEntity_Side runtimeSide, bool isClockwise, short sideIndex, Side side, LevelEntity_Level parentLevel)
        {
            if (!runtimeSide)
            {
                var sideGO = new GameObject(isClockwise ? $"Clockwise ({sideIndex})" : $"Counterclockwise ({sideIndex})");
                runtimeSide = sideGO.AddComponent<LevelEntity_Side>();

                runtimeSide.InitializeEntity(parentLevel, sideIndex, side);

                parentLevel.Sides[sideIndex] = runtimeSide;
            }
        }
    }
}
