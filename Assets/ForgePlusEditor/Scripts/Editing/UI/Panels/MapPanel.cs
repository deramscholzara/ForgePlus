using ForgePlus.DataFileIO;
using ForgePlus.Inspection;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The map file's inspector, in the inspector column (the map file isn't in the level, so it isn't selected). It's
    // shown anew after saving, since the saved file is then the loaded one.
    public class MapPanel : UIPanel
    {
        private VisualElement container;
        private Inspector_Map inspector;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/Map";
            }
        }

        protected override void OnLoaded()
        {
            container = Root.Find<ScrollView>("inspectors").contentContainer;

            MapsLoading.Instance.OnSaveCompleted += ShowInspector;
            ShowInspector();
        }

        protected override void OnUnloading()
        {
            MapsLoading.Instance.OnSaveCompleted -= ShowInspector;

            inspector?.Unload();
            inspector = null;
        }

        private void ShowInspector()
        {
            inspector?.Unload();
            inspector = null;

            var mapsFile = MapsLoading.Instance.MapsFile;
            if (mapsFile != null)
            {
                inspector = new Inspector_Map(mapsFile);
                inspector.Load(container);
            }
        }
    }
}
