using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities.Geometry;
using TMPro;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using AlephOne;
using ForgePlus.Extensions;

namespace RuntimeCore.Entities
{
    // TODO: Should inherit from LevelEntity_Base, and should have a separate EditableSurface component
    [AutoStaticsCleanup]
    public partial class LevelEntity_Annotation : EditableSurface_Base, ISelectionDisplayable, IInspectable
    {
        private static LevelEntity_Annotation prefab;

        public short NativeIndex { get; set; }
        public map_annotation NativeObject { get; set; }

        public LevelEntity_Level ParentLevel { private get; set; }

        [SerializeField]
        private TextMeshPro label = null;

        [SerializeField]
        private BoxCollider selectionCollider = null;

        // The polygon whose appearance on Aleph One's map shows the annotation (null if its index isn't a polygon's)
        public LevelEntity_Polygon LinkedPolygon
        {
            get
            {
                return ParentLevel.Polygons.TryGetValue(NativeObject.polygon_index, out var polygon) ? polygon : null;
            }
        }

        public static LevelEntity_Annotation Prefab
        {
            get
            {
                if (!prefab)
                {
                    prefab = Resources.Load<LevelEntity_Annotation>("Annotations/Annotation");
                }

                return prefab;
            }
        }

        public override void OnValidatedPointerClick(PointerEventData eventData)
        {
            SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false);
        }

        public override void OnValidatedBeginDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedDrag(PointerEventData eventData)
        {
            // Intentionally blank - for now
        }

        public override void OnValidatedEndDrag(PointerEventData eventData)
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
            if (state)
            {
                label.outlineWidth = 0.2f;

                gameObject.layer = SelectionManager.SelectionIndicatorLayer;
            }
            else
            {
                label.outlineWidth = 0f;

                gameObject.layer = SelectionManager.DefaultLayer;
            }

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

        public async void RefreshLabel()
        {
            label.text = NativeObject.GetText();

            // Wait two frames so the content size fitter has time to update to the new text size
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();

            var labelTransform = label.transform as RectTransform;
            selectionCollider.size = new Vector3(labelTransform.sizeDelta.x, labelTransform.sizeDelta.y, 0.01f);
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

        private void OnEnable()
        {
            if (NativeObject != null)
            {
                RefreshLabel();
            }
        }
    }
}
