using UnityEngine;

namespace RuntimeCore.Constraints
{
    public class PlatformConstraint : MonoBehaviour
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

        // If editing isn't possible, then the surface can just be hierarchically contstrained
#if NO_EDITING
        private void Start()
        {
            ApplyConstraint();
            transform.SetParent(Parent, worldPositionStays: true);
            Destroy(this);
        }
#endif
    }
}
