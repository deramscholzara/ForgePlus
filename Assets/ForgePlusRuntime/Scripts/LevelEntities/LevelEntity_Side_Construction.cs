using System;
using UnityEngine;
using AlephOne;
using static AlephOne.csmacros;
using static AlephOne.map;
using ForgePlus.Extensions;

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

        public enum Sections
        {
            Top,
            Middle,
            Bottom,
        }

        public new side_data NativeObject => base.NativeObject as side_data;

        // TODO: I don't really like that this needs to be here, but not sure how to set things up differently yet.
        public short ParentLineIndex { get; private set; }
        public bool IsClockwise { get; private set; }

        // Its line's polygon on the side's side
        public LevelEntity_Polygon FacingPolygon
        {
            get
            {
                var facingPolygonIndex = get_line_data(ParentLevel.Level, ParentLineIndex).GetPolygonOwner(IsClockwise);

                return ParentLevel.Polygons.TryGetValue(facingPolygonIndex, out var polygon) ? polygon : null;
            }
        }

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
            var line = get_line_data(level.Level, lineIndex);

            var sideIndex = line.GetSideIndex(isClockwise);

            // Note: A null-side may still be created as an untextured side
            var side = GetMemberWithBounds(level.Level.SideList, sideIndex, level.Level.SideList.Count);

            #region Facing_Elevations
            var facingPolygonIndex = line.GetPolygonOwner(isClockwise);

            if (facingPolygonIndex < 0)
            {
                return null;
            }

            // Span the platforms' whole travel.
            // Note: A platform that goes both ways meets at the midpoint of its travel, which its extrema already
            //       account for (platforms.cpp: calculate_platform_extrema).
            var facingPolygon = get_polygon_data(level.Level, facingPolygonIndex);
            facingPolygon.GetHeightRange(level.Level, out var lowestFacingFloor, out _, out _, out var highestFacingCeiling);
            #endregion Facing_Elevations

            #region Opposing_Elevations
            var opposingPolygonIndex = line.GetPolygonOwner(!isClockwise);
            var hasOpposingPolygon = opposingPolygonIndex >= 0;

            var lowestOpposingFloor = lowestFacingFloor;
            var highestOpposingFloor = lowestFacingFloor;
            var lowestOpposingCeiling = highestFacingCeiling;
            var highestOpposingCeiling = highestFacingCeiling;

            if (hasOpposingPolygon)
            {
                var opposingPolygon = get_polygon_data(level.Level, opposingPolygonIndex);
                opposingPolygon.GetHeightRange(level.Level, out lowestOpposingFloor, out highestOpposingFloor, out lowestOpposingCeiling, out highestOpposingCeiling);
            }
            #endregion Opposing_Elevations

            #region Exposure_Determination
            // Data-driven surface-exposure
            var dataExpectsFullSide = side != null &&
                                      side.type == _full_side;
            var dataExpectsTop = !dataExpectsFullSide &&
                                 side != null &&
                                 (side.type == _high_side || side.type == _split_side);

            // Geometry-driven surface-exposure
            var exposesTop = !dataExpectsFullSide &&
                             hasOpposingPolygon &&
                             highestFacingCeiling > lowestOpposingCeiling;

            var exposesMiddle = (!hasOpposingPolygon ||
                                LINE_HAS_TRANSPARENT_SIDE(line) ||
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
                surface.InitializeRuntimeSurface(runtimeSide, sideDataSource, Sections.Top);

                runtimeSide.PrimarySurface = surface;
                runtimeSide.TopSurface = surface;
            }

            if (exposesMiddle)
            {
                // Primary if there's no opposing polygon or it's explicitly "full", Transparent otherwise
                var sideDataSource = (!hasOpposingPolygon) ? DataSources.Primary : DataSources.Transparent;

                var hasLayeredTransparentSide = side.HasLayeredTransparentSide(level.Level);

                // The opening across the platforms' travel, as the line's adjacent heights are only their saved state
                var openingCeiling = hasOpposingPolygon ? (short)Mathf.Min(highestFacingCeiling, highestOpposingCeiling) : highestFacingCeiling;
                var openingFloor = hasOpposingPolygon ? (short)Mathf.Max(lowestFacingFloor, lowestOpposingFloor) : lowestFacingFloor;

                var highHeight = dataExpectsFullSide ? highestFacingCeiling : openingCeiling;
                var lowHeight = dataExpectsFullSide ? lowestFacingFloor : openingFloor;

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
                surface.InitializeRuntimeSurface(runtimeSide, sideDataSource, Sections.Middle);

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
                surface.InitializeRuntimeSurface(runtimeSide, sideDataSource, Sections.Bottom);

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

            if (side == null && runtimeSide)
            {
                runtimeSide.ApplyPlaceholderVisibility();
            }

            return runtimeSide;
        }

        protected override void AssembleEntity()
        {
            if (!ParentLevel)
            {
                throw new Exception("Level Entities must be initialized before being assembled.");
            }
        }

        private static void CreateSideRoot(ref LevelEntity_Side runtimeSide, bool isClockwise, short sideIndex, side_data side, LevelEntity_Level parentLevel, short parentLineIndex)
        {
            if (!runtimeSide)
            {
                var sideGO = new GameObject(isClockwise ? $"Clockwise ({sideIndex})" : $"Counterclockwise ({sideIndex})");
                runtimeSide = sideGO.AddComponent<LevelEntity_Side>();
                runtimeSide.ParentLineIndex = parentLineIndex;
                runtimeSide.IsClockwise = isClockwise;

                runtimeSide.InitializeEntity(parentLevel, sideIndex, side);

                if (side != null)
                {
                    parentLevel.Sides[sideIndex] = runtimeSide;
                }
                else
                {
                    // Placeholders have no side index to be found by
                    parentLevel.PlaceholderSides.Add(runtimeSide);
                }
            }
        }
    }
}
