using ForgePlus.LevelManipulation;
using ForgePlus.Palette;
using RuntimeCore.Entities;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Every polygon, by index, for choosing the selected annotation's polygon (grayed out while no annotation is
    // selected), with that polygon's swatch on
    public class PolygonPalettePanel : PalettePanel
    {
        protected override string SwatchTemplateName
        {
            get
            {
                return "SwatchPolygon";
            }
        }

        protected override string Header
        {
            get
            {
                return "Polygons";
            }
        }

        protected override bool ScrollsToSelection
        {
            get
            {
                return true;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            SelectionManager.Instance.OnSelectionChanged += ShowAvailability;
            ShowAvailability();
        }

        protected override void OnUnloading()
        {
            base.OnUnloading();

            var selectionManager = SelectionManager.Instance;
            if (selectionManager)
            {
                selectionManager.OnSelectionChanged -= ShowAvailability;
            }
        }

        protected override bool Shows(PaletteManager.Swatch swatch)
        {
            return swatch.Kind == PaletteManager.SwatchKinds.Polygon;
        }

        protected override void FillSwatch(TemplateContainer instance, PaletteManager.Swatch swatch)
        {
            instance.Q<Label>("index").text = swatch.Polygon.NativeIndex.ToString();
        }

        private void ShowAvailability()
        {
            SwatchesContainer.SetEnabled(LevelEntity_Annotation.SelectedAnnotation != null);
        }
    }
}
