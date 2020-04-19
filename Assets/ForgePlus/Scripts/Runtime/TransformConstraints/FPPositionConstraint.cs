using UnityEngine;

namespace ForgePlus.Runtime.Constraints
{
    public class FPPositionConstraint : MonoBehaviour
    {
        public Transform Parent;
        public Vector3 WorldOffsetFromParent = Vector3.zero;

        public void ApplyConstraint()
        {
            if (Parent)
            {
                transform.position = Parent.position + WorldOffsetFromParent;
            }
        }

        private void LateUpdate()
        {
            ApplyConstraint();
        }
    }
}
