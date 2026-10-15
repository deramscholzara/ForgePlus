using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace RuntimeCore.Entities.Geometry
{
    // Trims a side next to a platform to what Aleph One would draw for the platform's current height. Runs after
    // PlatformConstraint has moved the surface, as the trimmed geometry is relative to its transform. Only in frames that
    // need it (as it starts, as the platforms it's next to move, as its geometry changes, and as clipping is turned on or
    // off): it's disabled otherwise, so it costs nothing while they're still.
    [AutoStaticsCleanup]
    [DefaultExecutionOrder(1000)]
    public partial class PlatformSideClipping : MonoBehaviour
    {
        private static bool clippingEnabled = true;

        // Every side's, so turning clipping on or off reaches them all
        private static readonly HashSet<PlatformSideClipping> instances = new HashSet<PlatformSideClipping>();

        [System.NonSerialized]
        public RuntimeSurfaceGeometryModule_Side Module;

        private readonly List<LevelEntity_Platform> watchedPlatforms = new List<LevelEntity_Platform>();

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

                foreach (var instance in instances)
                {
                    instance.RequestUpdate();
                }
            }
        }

        // The platforms whose heights it's trimmed by
        public void Watch(IEnumerable<LevelEntity_Platform> platforms)
        {
            foreach (var platform in platforms)
            {
                if (platform && !watchedPlatforms.Contains(platform))
                {
                    watchedPlatforms.Add(platform);
                    platform.Moved += RequestUpdate;
                }
            }
        }

        // Trimmed again this frame
        public void RequestUpdate()
        {
            if (this)
            {
                enabled = true;
            }
        }

        private void Awake()
        {
            instances.Add(this);
        }

        private void LateUpdate()
        {
            Module?.UpdatePlatformClipping(clippingEnabled);

            enabled = false;
        }

        private void OnDestroy()
        {
            instances.Remove(this);

            foreach (var platform in watchedPlatforms)
            {
                if (platform)
                {
                    platform.Moved -= RequestUpdate;
                }
            }

            watchedPlatforms.Clear();
        }
    }
}
