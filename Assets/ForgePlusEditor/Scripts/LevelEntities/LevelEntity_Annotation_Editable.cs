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
        public static LevelEntity_Annotation SelectedAnnotation
        {
            get
            {
                return SelectionManager.Instance.SelectedObject as LevelEntity_Annotation;
            }
        }

        public static void LinkSelectedAnnotation(LevelEntity_Polygon polygon)
        {
            var annotation = SelectedAnnotation;
            if (annotation)
            {
                annotation.LinkPolygon(polygon);
            }
        }

        // Clicking the polygon the selected annotation is already linked to deselects it, as clicking nothing does
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
                annotation.LinkPolygon(polygon);
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

        // Shown at its polygon's height, with its polygon highlighted while it's selected
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

        private void LinkPolygon(LevelEntity_Polygon polygon)
        {
            SetPolygon(polygon);
            Inspector_Base.RefreshInspectorsOf(this);
        }
    }
}
#endif
