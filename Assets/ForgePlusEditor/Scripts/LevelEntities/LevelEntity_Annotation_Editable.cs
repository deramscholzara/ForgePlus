#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.Palette;
using RuntimeCore.Entities.Geometry;

namespace RuntimeCore.Entities
{
    public partial class LevelEntity_Annotation
    {
        // The annotation being edited in annotations mode, if one is selected
        public static LevelEntity_Annotation SelectedAnnotation
        {
            get
            {
                return SelectionManager.Instance.SelectedObject as LevelEntity_Annotation;
            }
        }

        // Links the selected annotation (if any) to the polygon, and shows the change in its inspectors
        public static void LinkSelectedAnnotation(LevelEntity_Polygon polygon)
        {
            var annotation = SelectedAnnotation;
            if (!annotation)
            {
                return;
            }

            annotation.SetPolygon(polygon);
            Inspector_Base.RefreshInspectorsOf(annotation);
        }

        // Clicking a polygon in the level links the selected annotation to it, or, if it's already linked to it,
        // deselects the annotation (as clicking nothing does)
        public static void ClickPolygon(LevelEntity_Polygon polygon)
        {
            var annotation = SelectedAnnotation;
            if (!annotation)
            {
                return;
            }

            if (annotation.LinkedPolygon == polygon)
            {
                SelectionManager.Instance.DeselectAll();
            }
            else
            {
                LinkSelectedAnnotation(polygon);
            }
        }

        // Keeps only the characters Aleph One can draw, as many as fit its text
        public void SetText(string text)
        {
            NativeObject.SetText(text);

            RefreshLabel();
        }

        public void SetLocation(world_point2d location)
        {
            NativeObject.location = location;

            RefreshPosition();
        }

        // The annotation is shown at its polygon's height, and while it's selected, its polygon is highlighted with it
        // (and shown in the polygon palette)
        public void SetPolygon(LevelEntity_Polygon polygon)
        {
            var isSelected = SelectionManager.Instance.GetIsSelected(this);

            if (isSelected)
            {
                DisplayPolygonSelectionState(false);
            }

            NativeObject.polygon_index = polygon.NativeIndex;

            if (isSelected)
            {
                DisplayPolygonSelectionState(true);
                PaletteManager.Instance.SelectSwatchForPolygon(polygon);
            }

            RefreshPosition();
        }
    }
}
#endif
