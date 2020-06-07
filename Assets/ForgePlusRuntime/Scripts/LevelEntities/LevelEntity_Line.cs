using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using UnityEngine;
using UnityEngine.EventSystems;
using Weland;

namespace RuntimeCore.Entities.Geometry
{
    // TODO: Should inherit from LevelEntity_Base, and should have a separate EditableSurface component
    public class LevelEntity_Line : EditableSurface_Base, ISelectionDisplayable, IInspectable
    {
        public short NativeIndex { get; set; }
        public Line NativeObject { get; set; }
        public LevelEntity_Side ClockwiseSide;
        public LevelEntity_Side CounterclockwiseSide;

        public LevelEntity_Level ParentLevel { private get; set; }

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
            var inspectorPrefab = Resources.Load<Inspector_Line>("Inspectors/Inspector - Line");
            var inspector = Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void GenerateSurfaces()
        {
            ClockwiseSide = LevelEntity_Side.AssembleEntity(ParentLevel, isClockwise: true, NativeObject);
            if (ClockwiseSide)
            {
                ClockwiseSide.transform.SetParent(transform);
            }

            CounterclockwiseSide = LevelEntity_Side.AssembleEntity(ParentLevel, isClockwise: false, NativeObject);
            if (CounterclockwiseSide)
            {
                CounterclockwiseSide.transform.SetParent(transform);
            }
        }
    }
}
