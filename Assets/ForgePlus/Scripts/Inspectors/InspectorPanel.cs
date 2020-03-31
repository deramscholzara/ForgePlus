using System.Collections;
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

        public void ClearAllInspectors()
        {
            foreach (var inspector in inspectors)
            {
                Destroy(inspector.gameObject);
            }

            inspectors.Clear();
        }
    }
}
