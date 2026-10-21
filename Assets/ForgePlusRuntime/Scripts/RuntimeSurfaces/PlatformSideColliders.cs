using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    // Sides beside a moving platform are reshaped each frame it moves, but only clicking (raycasts) uses their colliders,
    // and reassigning a collider's mesh has PhysX cook it again. So their colliders take their new shapes just before
    // the next raycast (Flush), rather than each frame. With UpdatesEachFrame on, they take them as they're reshaped
    // instead (for when collisions need to follow platforms live, such as a simulation of play).
    [AutoStaticsCleanup]
    public static partial class PlatformSideColliders
    {
        private static bool updatesEachFrame;

        // Each collider waiting on its new shape, and the mesh it takes it from
        private static readonly Dictionary<MeshCollider, Mesh> pendingColliders = new Dictionary<MeshCollider, Mesh>();

        public static bool UpdatesEachFrame
        {
            get
            {
                return updatesEachFrame;
            }
            set
            {
                updatesEachFrame = value;

                if (value)
                {
                    Flush();
                }
            }
        }

        // After the mesh it takes its shape from changed
        public static void Reshape(MeshCollider collider, Mesh mesh)
        {
            if (updatesEachFrame)
            {
                Apply(collider, mesh);
            }
            else
            {
                pendingColliders[collider] = mesh;
            }
        }

        // Before anything raycasts against the level
        public static void Flush()
        {
            if (pendingColliders.Count == 0)
            {
                return;
            }

            foreach (var (collider, mesh) in pendingColliders)
            {
                if (collider && mesh)
                {
                    Apply(collider, mesh);
                }
            }

            pendingColliders.Clear();
        }

        private static void Apply(MeshCollider collider, Mesh mesh)
        {
            // Reassigning the mesh rebuilds the collider
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }
    }
}
