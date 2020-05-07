using ForgePlus.Inspection;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPLine : FPInteractiveSurfaceBase, IFPManipulatable<Line>, IFPSelectionDisplayable, IFPInspectable
    {
        public short Index { get; set; }
        public Line WelandObject { get; set; }
        public FPSide ClockwiseSide;
        public FPSide CounterclockwiseSide;

        public FPLevel FPLevel { private get; set; }

        public override void OnValidatedPointerClick(PointerEventData eventData)
        {
            // TODO: Implement this
            throw new System.NotImplementedException();
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

            // TODO: Set Line selectability (enable scene-clickable element)
        }

        public void DisplaySelectionState(bool state)
        {
            // TODO: Display selection state of the line itself - not just the side "corners"
            //       (maybe put some sort of line "tube" at the top and bottom.
            // TODO: Create a selection utilities class for instantiating and arranging selection corners to vertices (needs a shader that renders on top of everything else, in a new render pass, too)
            //       Not really needed for MapObjects, since they'll just use stripes effect, but it'll be important for geometry (specifically, polygons & sides (selecting a side also displays line info))
            Debug.Log($"LINE: Display Selection of \"{name}\"", this);
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<InspectorFPLine>("Inspectors/Inspector - FPLine");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void GenerateSurfaces()
        {
            ClockwiseSide = FPSide.GenerateSurfaces(FPLevel, isClockwise: true, WelandObject);
            if (ClockwiseSide)
            {
                ClockwiseSide.transform.SetParent(transform);
            }

            CounterclockwiseSide = FPSide.GenerateSurfaces(FPLevel, isClockwise: false, WelandObject);
            if (CounterclockwiseSide)
            {
                CounterclockwiseSide.transform.SetParent(transform);
            }
        }
    }
}
