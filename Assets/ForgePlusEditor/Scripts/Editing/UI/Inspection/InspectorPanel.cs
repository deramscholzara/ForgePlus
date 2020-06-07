using ForgePlus.LevelManipulation;
using RuntimeCore.Common;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.Inspection
{
    public class InspectorPanel : SingletonMonoBehaviour<InspectorPanel>
    {
        private readonly List<Inspector_Base> inspectors= new List<Inspector_Base>();

        [SerializeField]
        private Transform inspectorsParent = null;

        public void AddInspector(Inspector_Base inspector)
        {
            inspectors.Add(inspector as Inspector_Base);
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
                if (inspector is IDestructionPreparable)
                {
                    (inspector as IDestructionPreparable).PrepareForDestruction();
                }

                Destroy(inspector.gameObject);
            }

            inspectors.Clear();
        }
    }
}
