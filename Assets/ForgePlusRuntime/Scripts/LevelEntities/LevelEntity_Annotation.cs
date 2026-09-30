using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities.Geometry;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;
using ForgePlus.UI;
using AlephOne;
using ForgePlus.Extensions;

namespace RuntimeCore.Entities
{
    // An annotation's text is shown as a name tag at its location (a WorldLabels label), which is clicked to select it
    // TODO: Should inherit from LevelEntity_Base, and should have a separate EditableSurface component
    public partial class LevelEntity_Annotation : EditableSurface_Base, ISelectionDisplayable, IInspectable
    {
        public short NativeIndex { get; set; }
        public map_annotation NativeObject { get; set; }

        public LevelEntity_Level ParentLevel { private get; set; }

        private Label label;

        // The polygon whose appearance on Aleph One's map shows the annotation (null if its index isn't a polygon's)
        public LevelEntity_Polygon LinkedPolygon
        {
            get
            {
                return ParentLevel.Polygons.TryGetValue(NativeObject.polygon_index, out var polygon) ? polygon : null;
            }
        }

        public override void OnValidatedPointerClick(WorldPointerEventData eventData)
        {
            SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false);
        }

        public override void OnValidatedBeginDrag(WorldPointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedDrag(WorldPointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedEndDrag(WorldPointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void SetSelectability(bool enabled)
        {
            base.SetSelectability(enabled);

            gameObject.SetActive(enabled);
        }

        public void DisplaySelectionState(bool state)
        {
            label?.EnableInClassList("fp-world-label--selected", state);

            DisplayPolygonSelectionState(state);
        }

        // The polygon that shows the annotation on Aleph One's map (once it's on the map) is highlighted with it:
        // its floor, ceiling and sides
        private void DisplayPolygonSelectionState(bool state)
        {
            var polygon = LinkedPolygon;
            if (!polygon)
            {
                return;
            }

            polygon.DisplaySelectionState(state);

            var polygonData = polygon.NativeObject;
            for (var i = 0; i < polygonData.vertex_count; i++)
            {
                if (ParentLevel.Sides.TryGetValue(polygonData.side_indexes[i], out var side) && side != null)
                {
                    side.DisplaySelectionState(state);
                }
            }
        }

        public void Inspect()
        {
            var inspector = new Inspector_Annotation(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void RefreshLabel()
        {
            if (label != null && NativeObject != null)
            {
                label.text = NativeObject.GetText();
            }
        }

        // At its location, halfway between its polygon's floor and ceiling
        public void RefreshPosition()
        {
            var height = 0f;
            var polygon = LinkedPolygon;
            if (polygon)
            {
                height = (polygon.NativeObject.floor_height + polygon.NativeObject.ceiling_height) / 2f / GeometryUtilities.WorldUnitIncrementsPerMeter;
            }

            transform.position = new Vector3(NativeObject.location.x / GeometryUtilities.WorldUnitIncrementsPerMeter, height, -NativeObject.location.y / GeometryUtilities.WorldUnitIncrementsPerMeter);
        }

        // The name tag is only shown while the annotation is (in Annotations mode)
        private void OnEnable()
        {
            var ui = ForgePlusUI.Instance;
            if (ui && ui.WorldLabels != null && label == null)
            {
                label = ui.WorldLabels.Add(transform, () => SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false));
                label.EnableInClassList("fp-world-label--selected", SelectionManager.Instance.GetIsSelected(this));
            }

            RefreshLabel();
        }

        private void OnDisable()
        {
            if (label != null)
            {
                var ui = ForgePlusUI.Instance;
                if (ui && ui.WorldLabels != null)
                {
                    ui.WorldLabels.Remove(label);
                }

                label = null;
            }
        }
    }
}
