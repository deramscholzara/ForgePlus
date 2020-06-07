using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace RuntimeCore.Entities
{
    // TODO: Should inherit from LevelEntity_Base, and should have a separate EditableSurface component
    public class LevelEntity_Annotation : EditableSurface_Base, ISelectionDisplayable, IInspectable
    {
        private static LevelEntity_Annotation prefab;

        public short NativeIndex { get; set; }
        public Annotation NativeObject { get; set; }

        public LevelEntity_Level ParentLevel { private get; set; }

        [SerializeField]
        private TextMeshPro label = null;

        [SerializeField]
        private BoxCollider selectionCollider = null;

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
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<Inspector_Annotation>("Inspectors/Inspector - Annotation");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public async void RefreshLabel()
        {
            label.text = NativeObject.Text;

            // Wait two frames so the content size fitter has time to update to the new text size
            await Task.Yield();
            await Task.Yield();

            var labelTransform = label.transform as RectTransform;
            selectionCollider.size = new Vector3(labelTransform.sizeDelta.x, labelTransform.sizeDelta.y, 0.01f);
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
