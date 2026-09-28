using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    // Trims a side next to a platform to what Aleph One would draw for the platform's current height, every frame.
    // Runs after PlatformConstraint has moved the surface, as the trimmed geometry is relative to its transform.
    [DefaultExecutionOrder(1000)]
    public class PlatformSideClipping : MonoBehaviour
    {
        private static bool clippingEnabled = true;

        [System.NonSerialized]
        public RuntimeSurfaceGeometryModule_Side Module;

        // When disabled, surfaces cover the platform's whole travel, and only their textures follow it
        public static bool ClippingEnabled
        {
            get
            {
                return clippingEnabled;
            }
            set
            {
                clippingEnabled = value;
            }
        }

        private void LateUpdate()
        {
            Module?.UpdatePlatformClipping(clippingEnabled);
        }
    }
}
