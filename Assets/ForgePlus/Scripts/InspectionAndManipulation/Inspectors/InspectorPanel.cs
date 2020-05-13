using ForgePlus.LevelManipulation;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.Inspection
{
    public class InspectorPanel : SingletonMonoBehaviour<InspectorPanel>
    {
        private readonly List<InspectorBase> inspectors= new List<InspectorBase>();

        [SerializeField]
        private Transform inspectorsParent = null;

        public void AddInspector(InspectorBase inspector)
        {
            inspectors.Add(inspector as InspectorBase);
            inspector.transform.SetParent(inspectorsParent, worldPositionStays: false);
        }

        public void RefreshAllInspectors()
        {
            foreach (var inspector in inspectors)
            {
                inspector.RefreshValuesInInspector();
            }
        }

        public void ClearAllInspectors()
        {
            foreach (var inspector in inspectors)
            {
                if (inspector is IFPDestructionPreparable)
                {
                    (inspector as IFPDestructionPreparable).PrepareForDestruction();
                }

                Destroy(inspector.gameObject);
            }

            inspectors.Clear();
        }
    }
}
