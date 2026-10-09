using ForgePlus.History;
using RuntimeCore.Common;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    // The inspectors for the current selection, shown in the inspector column while it's loaded
    public class InspectorPanel : OnDemandSingleton<InspectorPanel>
    {
        private readonly List<Inspector_Base> inspectors = new List<Inspector_Base>();

        private VisualElement container;

        // Where the inspectors are shown (null while the inspector column isn't loaded)
        public void SetContainer(VisualElement newContainer)
        {
            foreach (var inspector in inspectors)
            {
                inspector.Unload();
            }

            container = newContainer;

            if (container != null)
            {
                foreach (var inspector in inspectors)
                {
                    inspector.Load(container);
                }
            }
        }

        public void AddInspector(Inspector_Base inspector)
        {
            inspectors.Add(inspector);

            if (container != null)
            {
                inspector.Load(container);
            }
        }

        public void RefreshAllInspectors()
        {
            LevelHistory.MarkEdited();

            foreach (var inspector in inspectors)
            {
                inspector.RefreshValuesInInspector();
            }
        }

        public void ClearAllInspectors()
        {
            foreach (var inspector in inspectors)
            {
                if (inspector is IDestructionPreparable)
                {
                    (inspector as IDestructionPreparable).PrepareForDestruction();
                }

                inspector.Unload();
            }

            inspectors.Clear();
        }
    }
}
