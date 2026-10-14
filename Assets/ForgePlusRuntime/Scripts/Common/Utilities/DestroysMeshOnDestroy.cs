using UnityEngine;

namespace ForgePlus.LevelManipulation.Utilities
{
    // Destroys its object's mesh as the object is destroyed (as a level closes, as surfaces are made again, and as Play mode
    // ends), as a component of its own so it outlives components that remove themselves
    public class DestroysMeshOnDestroy : MonoBehaviour
    {
        public Mesh Mesh { get; set; }

        private void OnDestroy()
        {
            if (!Mesh)
            {
                return;
            }

            // Outside Play mode (such as as it ends), only right away
            if (Application.isPlaying)
            {
                Destroy(Mesh);
            }
            else
            {
                DestroyImmediate(Mesh);
            }

            Mesh = null;
        }
    }
}
