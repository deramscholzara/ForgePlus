using AlephOne;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using RuntimeCore.Entities;
using System.Collections.Generic;

namespace ForgePlus.UI
{
    // The level's geometry problems (GeometryValidation), and objects and annotations outside the polygons they're in
    public static class GeometryErrors
    {
        public static IEnumerable<LevelError> Find(LevelEntity_Level level)
        {
            var data = level.Level;

            foreach (var issue in GeometryValidation.CheckLevel(data))
            {
                yield return ErrorFor(level, issue);
            }

            for (short objectIndex = 0; objectIndex < data.SavedObjectList.Count; objectIndex++)
            {
                var mapObject = data.SavedObjectList[objectIndex];
                var location = new world_point2d(mapObject.location.x, mapObject.location.y);

                if (!ObjectPlacement.IsInside(data, mapObject.polygon_index, location) && map.world_point_to_polygon_index(data, location) == cstypes.NONE &&
                    mapObject.polygon_index >= 0 && mapObject.polygon_index < data.PolygonList.Count)
                {
                    var index = objectIndex;
                    yield return new LevelError(Get("Errors.Geometry.ObjectOutsidePolygons", objectIndex, mapObject.polygon_index),
                                                () => PutObjectInPolygon(index), isWarning: true, show: () => LevelFocus.ShowObject(index));
                }
            }

            for (short annotationIndex = 0; annotationIndex < data.MapAnnotationList.Count; annotationIndex++)
            {
                var annotation = data.MapAnnotationList[annotationIndex];
                if (!ObjectPlacement.IsInside(data, annotation.polygon_index, annotation.location) &&
                    annotation.polygon_index >= 0 && annotation.polygon_index < data.PolygonList.Count)
                {
                    var index = annotationIndex;
                    yield return new LevelError(Get("Errors.Geometry.AnnotationOutsidePolygon", annotationIndex, annotation.polygon_index),
                                                () => PutAnnotationInPolygon(index), isWarning: true, show: () => LevelFocus.ShowAnnotation(index));
                }
            }
        }

        // A short description, for showing while a point is dragged
        public static string ShortDescription(GeometryIssue issue)
        {
            return Describe(issue, ".Short");
        }

        private static LevelError ErrorFor(LevelEntity_Level level, GeometryIssue issue)
        {
            var description = Describe(issue, string.Empty);
            System.Action fix = null;
            System.Action show = null;

            switch (issue.Kind)
            {
                case GeometryIssueKind.InsideOutPolygon:
                case GeometryIssueKind.PolygonWithoutArea:
                case GeometryIssueKind.CrossingEdges:
                    show = () => LevelFocus.ShowPolygon(issue.Polygon);
                    break;
                case GeometryIssueKind.ConcaveCorner:
                case GeometryIssueKind.PointNearLine:
                    show = () => LevelFocus.ShowPoint(issue.Point);
                    break;
                case GeometryIssueKind.RedundantStraightCorner:
                    show = () => LevelFocus.ShowPoint(issue.Point);

                    if (GeometryValidation.IsRemovableStraightCorner(level.Level, issue.Point))
                    {
                        fix = () => GeometryRepair.RemoveStraightCorner(LevelEntity_Level.Instance, issue.Point);
                    }

                    break;
                case GeometryIssueKind.ZeroLengthLine:
                    show = () => LevelFocus.ShowPoint(issue.Point);

                    if (LineCollapse.CanCollapse(level.Level, issue.Line))
                    {
                        fix = () => GeometryRepair.CollapseLine(LevelEntity_Level.Instance, issue.Line);
                    }
                    else
                    {
                        description += " " + Get("Errors.Geometry.ZeroLengthLine.CantCollapse");
                    }

                    break;
                case GeometryIssueKind.LineTooLong:
                    show = () => LevelFocus.ShowLine(issue.Line);
                    break;
                case GeometryIssueKind.TooManyMapIndexes:
                case GeometryIssueKind.MapIndexesAlephOneOnly:
                    show = () => LevelFocus.ShowLevel();
                    break;
            }

            return new LevelError(description, fix, issue.IsWarning, show);
        }

        private static string Describe(GeometryIssue issue, string suffix)
        {
            var key = "Errors.Geometry." + issue.Kind + suffix;

            switch (issue.Kind)
            {
                case GeometryIssueKind.ConcaveCorner:
                case GeometryIssueKind.RedundantStraightCorner:
                    return Get(key, issue.Polygon, issue.Point);
                case GeometryIssueKind.CrossingEdges:
                    return Get(key, issue.Polygon, issue.Line, issue.OtherLine);
                case GeometryIssueKind.ZeroLengthLine:
                    var segment = issue.Segments[0];
                    return Get(key, issue.Line, segment.A, segment.B);
                case GeometryIssueKind.LineTooLong:
                    return Get(key, issue.Line);
                case GeometryIssueKind.PointNearLine:
                    return Get(key, issue.Point, issue.Line, issue.Polygon);
                case GeometryIssueKind.TooManyMapIndexes:
                case GeometryIssueKind.MapIndexesAlephOneOnly:
                    return Get(key, issue.Count.ToString("N0"));
                default:
                    return Get(key, issue.Polygon);
            }
        }

        private static void PutObjectInPolygon(short objectIndex)
        {
            var level = LevelEntity_Level.Instance;
            var mapObject = level.MapObjects[objectIndex];
            var data = mapObject.NativeObject;

            var location = new world_point2d(data.location.x, data.location.y);
            var polygonIndex = data.polygon_index;
            ObjectPlacement.PutInPolygon(level.Level, ref location, ref polygonIndex);

            mapObject.MoveTo(location, polygonIndex, data.location.z);
        }

        private static void PutAnnotationInPolygon(short annotationIndex)
        {
            var level = LevelEntity_Level.Instance;
            var annotation = level.Annotations[annotationIndex];
            var data = annotation.NativeObject;

            var location = data.location;
            var polygonIndex = data.polygon_index;
            ObjectPlacement.PutInPolygon(level.Level, ref location, ref polygonIndex);

            data.location = location;
            data.polygon_index = polygonIndex;
            annotation.RefreshPosition();
        }

        private static string Get(string key, params object[] args)
        {
            return Strings.Get(Strings.Common, key, args);
        }
    }
}
