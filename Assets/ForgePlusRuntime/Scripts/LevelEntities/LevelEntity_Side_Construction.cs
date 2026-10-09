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

            if (!TryGetFaceElevations(level.Level, line, isClockwise, out var elevations))
            {
                return null;
            }

            var dataExpectsFullSide = DataExpectsFullSide(side);
            var dataExpectsTop = !dataExpectsFullSide &&
                                 side != null &&
                                 (side.type == _high_side || side.type == _split_side);

            GetExposure(line, side, elevations, out var exposesTop, out var exposesMiddle, out var exposesBottom);

            #region Surface_Assembly
            LevelEntity_Side runtimeSide = null;

            if (exposesTop)
            {
                // Top is always Primary
                var sideDataSource = DataSources.Primary;

                var highHeight = elevations.HighestFacingCeiling;
                var lowHeight = elevations.LowestOpposingCeiling;

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
                var sideDataSource = (!elevations.HasOpposingPolygon) ? DataSources.Primary : DataSources.Transparent;

                var hasLayeredTransparentSide = side.HasLayeredTransparentSide(level.Level);

                // The opening across the platforms' travel, as the line's adjacent heights are only their saved state
                var openingCeiling = elevations.HasOpposingPolygon ? (short)Mathf.Min(elevations.HighestFacingCeiling, elevations.HighestOpposingCeiling) : elevations.HighestFacingCeiling;
                var openingFloor = elevations.HasOpposingPolygon ? (short)Mathf.Max(elevations.LowestFacingFloor, elevations.LowestOpposingFloor) : elevations.LowestFacingFloor;

                var highHeight = dataExpectsFullSide ? elevations.HighestFacingCeiling : openingCeiling;
                var lowHeight = dataExpectsFullSide ? elevations.LowestFacingFloor : openingFloor;

                var typeDescriptor = elevations.HasOpposingPolygon ? $"Transparent - HasTransparentSide - Source:{sideDataSource}" : $"Full - Unopposed - Source:{sideDataSource}";

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

                var highHeight = elevations.HighestOpposingFloor;
                var lowHeight = elevations.LowestFacingFloor;

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

        // Whether its polygons' heights expose any of a line's face (so it needs side data to be textured, lit and so on),
        // apart from what its side data says. A face that's only drawn because its side is full isn't exposed.
        public static bool IsFaceExposed(MapLevel level, short lineIndex, bool isClockwise)
        {
            var line = get_line_data(level, lineIndex);

            if (!TryGetFaceElevations(level, line, isClockwise, out var elevations))
            {
                return false;
            }

            GetExposure(line, side: null, elevations, out var exposesTop, out var exposesMiddle, out var exposesBottom);

            return exposesTop || exposesMiddle || exposesBottom;
        }

        // The heights a line's face spans, across its polygons' platforms' whole travel.
        // Note: A platform that goes both ways meets at the midpoint of its travel, which its extrema already
        //       account for (platforms.cpp: calculate_platform_extrema).
        private struct FaceElevations
        {
            public bool HasOpposingPolygon;
            public short LowestFacingFloor;
            public short HighestFacingCeiling;
            public short LowestOpposingFloor;
            public short HighestOpposingFloor;
            public short LowestOpposingCeiling;
            public short HighestOpposingCeiling;
        }

        // False when no polygon faces it
        private static bool TryGetFaceElevations(MapLevel level, line_data line, bool isClockwise, out FaceElevations elevations)
        {
            elevations = default;

            var facingPolygonIndex = line.GetPolygonOwner(isClockwise);

            if (facingPolygonIndex < 0)
            {
                return false;
            }

            var facingPolygon = get_polygon_data(level, facingPolygonIndex);
            facingPolygon.GetHeightRange(level, out elevations.LowestFacingFloor, out _, out _, out elevations.HighestFacingCeiling);

            var opposingPolygonIndex = line.GetPolygonOwner(!isClockwise);
            elevations.HasOpposingPolygon = opposingPolygonIndex >= 0;

            if (elevations.HasOpposingPolygon)
            {
                var opposingPolygon = get_polygon_data(level, opposingPolygonIndex);
                opposingPolygon.GetHeightRange(level,
                                               out elevations.LowestOpposingFloor,
                                               out elevations.HighestOpposingFloor,
                                               out elevations.LowestOpposingCeiling,
                                               out elevations.HighestOpposingCeiling);
            }
            else
            {
                elevations.LowestOpposingFloor = elevations.LowestFacingFloor;
                elevations.HighestOpposingFloor = elevations.LowestFacingFloor;
                elevations.LowestOpposingCeiling = elevations.HighestFacingCeiling;
                elevations.HighestOpposingCeiling = elevations.HighestFacingCeiling;
            }

            return true;
        }

        private static bool DataExpectsFullSide(side_data side)
        {
            return side != null && side.type == _full_side;
        }

        // Geometry-driven surface-exposure, which a full side's data overrides with its middle alone
        private static void GetExposure(line_data line, side_data side, FaceElevations elevations, out bool exposesTop, out bool exposesMiddle, out bool exposesBottom)
        {
            var dataExpectsFullSide = DataExpectsFullSide(side);

            exposesTop = !dataExpectsFullSide &&
                         elevations.HasOpposingPolygon &&
                         elevations.HighestFacingCeiling > elevations.LowestOpposingCeiling;

            exposesMiddle = (!elevations.HasOpposingPolygon ||
                            LINE_HAS_TRANSPARENT_SIDE(line) ||
                            dataExpectsFullSide) &&
                            elevations.HighestFacingCeiling > elevations.LowestFacingFloor &&
                            (elevations.HighestOpposingCeiling > elevations.LowestOpposingFloor || dataExpectsFullSide);

            exposesBottom = !dataExpectsFullSide &&
                            elevations.HasOpposingPolygon &&
                            elevations.HighestOpposingFloor > elevations.LowestFacingFloor;
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
