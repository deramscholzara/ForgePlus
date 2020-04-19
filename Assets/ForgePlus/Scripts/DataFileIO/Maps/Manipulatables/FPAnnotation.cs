using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using Weland;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.EventSystems;

namespace ForgePlus.LevelManipulation
{
    public class FPAnnotation : FPInteractiveSurfaceBase, IFPManipulatable<Annotation>, IFPSelectionDisplayable, IFPInspectable
    {
        private static FPAnnotation prefab;

        public short? Index { get; set; }
        public Annotation WelandObject { get; set; }

        public FPLevel FPLevel { private get; set; }

        [SerializeField]
        private TextMeshPro label = null;

        [SerializeField]
        private BoxCollider selectionCollider = null;

        public static FPAnnotation Prefab
        {
            get
            {
                if (!prefab)
                {
                    prefab = Resources.Load<FPAnnotation>("Annotations/FPAnnotation");
                }

                return prefab;
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (isSelectable)
            {
                SelectionManager.Instance.ToggleObjectSelection(this, multiSelect: false);
            }
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
            var inspectorPrefab = Resources.Load<InspectorFPAnnotation>("Inspectors/Inspector - FPAnnotation");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public async void RefreshLabel()
        {
            label.text = WelandObject.Text;

            // Wait two frames so the content size fitter has time to update to the new text size
            await Task.Yield();
            await Task.Yield();

            var labelTransform = label.transform as RectTransform;
            selectionCollider.size = new Vector3(labelTransform.sizeDelta.x, labelTransform.sizeDelta.y, 0.01f);
        }

        private void OnEnable()
        {
            if (WelandObject != null)
            {
                RefreshLabel();
            }
        }
    }
}
