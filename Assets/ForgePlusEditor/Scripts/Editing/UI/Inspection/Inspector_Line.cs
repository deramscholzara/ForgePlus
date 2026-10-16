using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Localization;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    public class Inspector_Line : Inspector_Base<LevelEntity_Line>
    {
        public Inspector_Line(LevelEntity_Line line) : base(line)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Line";
            }
        }

        private line_data Line
        {
            get
            {
                return Entity.NativeObject;
            }
        }

        [CreateProperty]
        public string Id
        {
            get
            {
                return Entity.NativeIndex.ToString();
            }
        }

        // Between two polygons, a solid line is an invisible wall (it's still transparent)
        [CreateProperty]
        public bool Solid
        {
            get
            {
                return map.LINE_IS_SOLID(Line);
            }
            set
            {
                if (value != Solid)
                {
                    Edit(line => LineFlagsEditing.SetSolidity(LevelEntity_Level.Instance.Level, line.NativeIndex, value));
                }
            }
        }

        // A wall (with a polygon on one side only) is always solid, and a platform sets whether a line beside it is as it
        // moves (its variable elevation)
        [CreateProperty]
        public bool IsSolidEditable
        {
            get
            {
                return IsBetweenPolygons && !map.LINE_IS_VARIABLE_ELEVATION(Line);
            }
        }

        [CreateProperty]
        public string SolidCaption
        {
            get
            {
                if (!IsBetweenPolygons)
                {
                    return Strings.Get(Strings.Geometry, "Inspector.Line.SolidCaption.Wall");
                }

                if (map.LINE_IS_VARIABLE_ELEVATION(Line))
                {
                    return Strings.Get(Strings.Geometry, "Inspector.Line.SolidCaption.Platform");
                }

                return Solid ? Strings.Get(Strings.Geometry, "Inspector.Line.SolidCaption.InvisibleWall") : string.Empty;
            }
        }

        private bool IsBetweenPolygons
        {
            get
            {
                return Line.clockwise_polygon_owner != cstypes.NONE && Line.counterclockwise_polygon_owner != cstypes.NONE;
            }
        }

        [CreateProperty]
        public bool Transparent
        {
            get
            {
                return map.LINE_IS_TRANSPARENT(Line);
            }
        }

        // Aleph One only: projectiles pass through the line's transparent sides (rather than hitting a grate's texture)
        [CreateProperty]
        public bool Decorative
        {
            get
            {
                return Line.is_decorative();
            }
            set
            {
                Edit(line => line.NativeObject.set_decorative(value));
            }
        }

        // Only a line with a transparent side has anything for projectiles to pass through
        [CreateProperty]
        public bool IsDecorativeEditable
        {
            get
            {
                return map.LINE_HAS_TRANSPARENT_SIDE(Line) || Line.is_decorative();
            }
        }

        // A face with a polygon on it has a side to select (or add, if it has none). With two, the front one is on the
        // camera's side of the line (as the camera sees it), or if the camera's on the line, the one facing the
        // lower-numbered polygon.
        [CreateProperty]
        public bool IsFrontSideShown
        {
            get
            {
                return Line.clockwise_polygon_owner != cstypes.NONE || Line.counterclockwise_polygon_owner != cstypes.NONE;
            }
        }

        [CreateProperty]
        public bool IsBackSideShown
        {
            get
            {
                return IsBetweenPolygons;
            }
        }

        [CreateProperty]
        public string FrontSideText
        {
            get
            {
                var hasSide = Line.GetSideIndex(FrontIsClockwise) != cstypes.NONE;
                if (!IsBetweenPolygons)
                {
                    return Strings.Get(Strings.Geometry, hasSide ? "Inspector.Line.SelectSide" : "Inspector.Line.AddSide");
                }

                return Strings.Get(Strings.Geometry, hasSide ? "Inspector.Line.SelectFrontSide" : "Inspector.Line.AddFrontSide");
            }
        }

        [CreateProperty]
        public string BackSideText
        {
            get
            {
                var hasSide = Line.GetSideIndex(!FrontIsClockwise) != cstypes.NONE;

                return Strings.Get(Strings.Geometry, hasSide ? "Inspector.Line.SelectBackSide" : "Inspector.Line.AddBackSide");
            }
        }

        // Whether the front face is the line's clockwise one (where its clockwise polygon is)
        private bool FrontIsClockwise
        {
            get
            {
                if (Line.counterclockwise_polygon_owner == cstypes.NONE)
                {
                    return true;
                }

                if (Line.clockwise_polygon_owner == cstypes.NONE)
                {
                    return false;
                }

                var data = LevelEntity_Level.Instance.Level;
                var start = data.EndpointList[Line.endpoint_indexes[0]].vertex;
                var end = data.EndpointList[Line.endpoint_indexes[1]].vertex;
                var clockwiseCenter = data.PolygonList[Line.clockwise_polygon_owner].center;

                var camera = UnityEngine.Camera.main;
                if (camera && Line.length > 0)
                {
                    // In world units, as the level's points are
                    var cameraPosition = camera.transform.position * GeometryUtilities.WorldUnitIncrementsPerMeter;
                    var cameraSide = SideOfLine(start, end, cameraPosition.x, -cameraPosition.z);
                    var clockwiseSide = SideOfLine(start, end, clockwiseCenter.x, clockwiseCenter.y);

                    // Farther than this from the line, the camera's on a side of it
                    if (System.Math.Abs(cameraSide) / Line.length > OnLineDistance && clockwiseSide != 0d)
                    {
                        return System.Math.Sign(cameraSide) == System.Math.Sign(clockwiseSide);
                    }
                }

                return Line.clockwise_polygon_owner < Line.counterclockwise_polygon_owner;
            }
        }

        // In world units
        private const double OnLineDistance = 1d;

        // Positive on one side of the line, negative on the other (twice the area of the triangle)
        private static double SideOfLine(world_point2d start, world_point2d end, double x, double y)
        {
            return ((double) end.x - start.x) * (y - start.y) - ((double) end.y - start.y) * (x - start.x);
        }

        // Selects the side on the face (adding it first, with the addition and the selection as one action, if it has
        // none)
        private void SelectSide(bool isClockwise)
        {
            var level = LevelEntity_Level.Instance;
            var sideIndex = Line.GetSideIndex(isClockwise);
            var isAdded = false;

            if (sideIndex == cstypes.NONE)
            {
                sideIndex = SideEditing.AddSide(level, Entity.NativeIndex, isClockwise);
                isAdded = sideIndex != cstypes.NONE;
            }

            if (sideIndex == cstypes.NONE || !level.Sides.TryGetValue(sideIndex, out var side) || !side)
            {
                return;
            }

            if (isAdded)
            {
                SelectionHistory.JoinNextToLast();
            }

            SelectionManager.Instance.SelectObject(side, multiSelect: false);
        }

        // Which face is in front changes as the camera moves
        private bool shownFrontIsClockwise;

        protected override void OnLoaded()
        {
            base.OnLoaded();

            var frontSide = Root.Q("front-side");
            var frontButton = frontSide.Q<Button>();
            frontSide.BindShown(this, nameof(IsFrontSideShown));
            frontButton.Bind("text", this, nameof(FrontSideText));
            frontButton.clicked += () => SelectSide(FrontIsClockwise);

            var backSide = Root.Q("back-side");
            var backButton = backSide.Q<Button>();
            backSide.BindShown(this, nameof(IsBackSideShown));
            backButton.Bind("text", this, nameof(BackSideText));
            backButton.clicked += () => SelectSide(!FrontIsClockwise);

            shownFrontIsClockwise = FrontIsClockwise;
            Root.schedule.Execute(() =>
            {
                if (LevelEntity_Level.Instance && FrontIsClockwise != shownFrontIsClockwise)
                {
                    shownFrontIsClockwise = FrontIsClockwise;
                    RefreshValuesInInspector();
                }
            }).Every(200);
        }
    }
}
