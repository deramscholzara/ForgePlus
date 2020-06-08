using ForgePlus.LevelManipulation.Utilities;
using System;
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

        // TODO: I don't really like that this needs to be here, but not sure how to set things up differently yet.
        public short ParentLineIndex { get; private set; }
        public bool IsClockwise { get; private set; }

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

        public static LevelEntity_Side AssembleEntity(LevelEntity_Level level, bool isClockwise, short lineIndex)
        {
            var line = level.Level.Lines[lineIndex];

            var sideIndex = isClockwise ? line.ClockwisePolygonSideIndex : line.CounterclockwisePolygonSideIndex;

            // Note: A null-side may still be created as an untextured side
            var side = sideIndex < level.Level.Sides.Count && sideIndex >= 0 ? level.Level.Sides[sideIndex] : null;

            #region Facing_Elevations
            var facingPolygonIndex = isClockwise ? line.ClockwisePolygonOwner : line.CounterclockwisePolygonOwner;

            if (facingPolygonIndex < 0)
            {
                return null;
            }

            var facingPolygon = level.Level.Polygons[facingPolygonIndex];

            Platform facingPlatform;
            facingPlatform = GeometryUtilities.GetPlatformForPolygon(level.Level, facingPolygon);

            var highestFacingCeiling = facingPolygon.CeilingHeight;
            var lowestFacingFloor = facingPolygon.FloorHeight;

            if (facingPlatform != null)
            {
                if (facingPlatform.ComesFromFloor && facingPlatform.ComesFromCeiling)
                {
                    var roundedMidpoint = (short)Mathf.RoundToInt((facingPlatform.RuntimeMinimumHeight(level.Level) + facingPlatform.RuntimeMaximumHeight(level.Level)) * 0.5f);

                    highestFacingCeiling = (short)Mathf.Max(highestFacingCeiling, roundedMidpoint);
                    lowestFacingFloor = (short)Mathf.Min(lowestFacingFloor, roundedMidpoint);
                }
                else if (facingPlatform.ComesFromFloor)
                {
                    lowestFacingFloor = (short)Mathf.Min(lowestFacingFloor, facingPlatform.RuntimeMinimumHeight(level.Level));
                }
                else if (facingPlatform.ComesFromCeiling)
                {
                    highestFacingCeiling = (short)Mathf.Max(highestFacingCeiling, facingPlatform.RuntimeMaximumHeight(level.Level));
                }
            }
            #endregion Facing_Elevations

            #region Opposing_Elevations
            var opposingPolygonIndex = isClockwise ? line.CounterclockwisePolygonOwner : line.ClockwisePolygonOwner;
            var hasOpposingPolygon = opposingPolygonIndex >= 0;

            var opposingPolygon = hasOpposingPolygon ? level.Level.Polygons[opposingPolygonIndex] : null;

            Platform opposingPlatform = null;
            if (opposingPolygon != null)
            {
                opposingPlatform = GeometryUtilities.GetPlatformForPolygon(level.Level, opposingPolygon);
            }

            var lowestOpposingCeiling = hasOpposingPolygon ? opposingPolygon.CeilingHeight : highestFacingCeiling;
            var highestOpposingCeiling = lowestOpposingCeiling;

            var highestOpposingFloor = hasOpposingPolygon ? opposingPolygon.FloorHeight : lowestFacingFloor;
            var lowestOpposingFloor = highestOpposingFloor;

            if (opposingPlatform != null)
            {
                if (opposingPlatform.ComesFromFloor && opposingPlatform.ComesFromCeiling)
                {
                    var roundedMidpoint = (short)Mathf.RoundToInt((opposingPlatform.RuntimeMinimumHeight(level.Level) + opposingPlatform.RuntimeMaximumHeight(level.Level)) * 0.5f);

                    highestOpposingFloor = (short)Mathf.Max(highestOpposingFloor, roundedMidpoint);
                    lowestOpposingCeiling = (short)Mathf.Min(lowestOpposingCeiling, roundedMidpoint);
                }
                else if (opposingPlatform.ComesFromFloor)
                {
                    highestOpposingFloor = (short)Mathf.Max(highestOpposingFloor, opposingPlatform.RuntimeMaximumHeight(level.Level));
                    lowestOpposingFloor = opposingPlatform.RuntimeMinimumHeight(level.Level);
                }
                else if (opposingPlatform.ComesFromCeiling)
                {
                    highestOpposingCeiling = opposingPlatform.RuntimeMaximumHeight(level.Level);
                    lowestOpposingCeiling = (short)Mathf.Min(lowestOpposingCeiling, opposingPlatform.RuntimeMinimumHeight(level.Level));
                }
            }
            #endregion Opposing_Elevations

            #region Exposure_Determination
            // Data-driven surface-exposure
            var dataExpectsFullSide = side != null &&
                                      side.Type == SideType.Full &&
                                      !side.Primary.Texture.IsEmpty();
            var dataExpectsTop = !dataExpectsFullSide &&
                                 side != null &&
                                 (side.Type == SideType.High || side.Type == SideType.Split);

            // Geometry-driven surface-exposure
            var exposesTop = !dataExpectsFullSide &&
                             hasOpposingPolygon &&
                             highestFacingCeiling > lowestOpposingCeiling;

            var exposesMiddle = (!hasOpposingPolygon ||
                                (line.Flags & LineFlags.HasTransparentSide) != 0 ||
                                dataExpectsFullSide) &&
                                highestFacingCeiling > lowestFacingFloor &&
                                (highestOpposingCeiling > lowestOpposingFloor || dataExpectsFullSide);

            var exposesBottom = !dataExpectsFullSide &&
                                hasOpposingPolygon &&
                                highestOpposingFloor > lowestFacingFloor;
            #endregion Exposure_Determination

            #region Surface_Assembly
            LevelEntity_Side runtimeSide = null;

            if (exposesTop)
            {
                // Top is always Primary
                var sideDataSource = DataSources.Primary;

                var highHeight = highestFacingCeiling;
                var lowHeight = lowestOpposingCeiling;

                CreateSideRoot(ref runtimeSide, isClockwise, sideIndex, side, level, lineIndex);

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

                CreateSideRoot(ref runtimeSide, isClockwise, sideIndex, side, level, lineIndex);

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

                CreateSideRoot(ref runtimeSide, isClockwise, sideIndex, side, level, lineIndex);

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
            #endregion Surface_Assembly

            return runtimeSide;
        }

        protected override void AssembleEntity()
        {
            if (!ParentLevel)
            {
                throw new Exception("Level Entities must be initialized before being assembled.");
            }
        }

        private static void CreateSideRoot(ref LevelEntity_Side runtimeSide, bool isClockwise, short sideIndex, Side side, LevelEntity_Level parentLevel, short parentLineIndex)
        {
            if (!runtimeSide)
            {
                var sideGO = new GameObject(isClockwise ? $"Clockwise ({sideIndex})" : $"Counterclockwise ({sideIndex})");
                runtimeSide = sideGO.AddComponent<LevelEntity_Side>();
                runtimeSide.ParentLineIndex = parentLineIndex;
                runtimeSide.IsClockwise = isClockwise;

                runtimeSide.InitializeEntity(parentLevel, sideIndex, side);

                parentLevel.Sides[sideIndex] = runtimeSide;
            }
        }
    }
}
