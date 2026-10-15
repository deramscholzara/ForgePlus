using RuntimeCore.Entities.Geometry;
using UnityEngine;

namespace RuntimeCore.Constraints
{
    // Keeps a surface at its offset from the platform it follows. Only in frames the platform moves in (and as it starts):
    // it's disabled otherwise, so it costs nothing while the platform is still.
    public class PlatformConstraint : MonoBehaviour
    {
        public Transform Parent;
        public Vector3 WorldOffsetFromParent = Vector3.zero;

        private LevelEntity_Platform watchedPlatform;

        public void Watch(LevelEntity_Platform platform)
        {
            Unwatch();

            watchedPlatform = platform;
            Parent = platform.transform;
            platform.Moved += RequestUpdate;
        }

        public void ApplyConstraint()
        {
            if (!Parent)
            {
                return;
            }

            // Only as its platform moves, as Surface Cache Global Illumination updates every moved surface
            var position = Parent.position + WorldOffsetFromParent;
            if (transform.position != position)
            {
                transform.position = position;
            }
        }

        private void RequestUpdate()
        {
            if (this)
            {
                enabled = true;
            }
        }

        private void LateUpdate()
        {
            ApplyConstraint();

            enabled = false;
        }

        private void OnDestroy()
        {
            Unwatch();
        }

        private void Unwatch()
        {
            if (watchedPlatform)
            {
                watchedPlatform.Moved -= RequestUpdate;
            }

            watchedPlatform = null;
        }
    }
}
