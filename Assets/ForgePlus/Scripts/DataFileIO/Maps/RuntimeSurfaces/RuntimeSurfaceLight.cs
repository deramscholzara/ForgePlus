using ForgePlus.DataFileIO;
using System.Collections.Generic;
using UnityEngine;


namespace ForgePlus.LevelManipulation
{
    public class RuntimeSurfaceLight : MonoBehaviour
    {
        protected struct RuntimeSurfaceMaterialKey
        {
            public Material sourceMaterial;
            public FPLight sourceLight;
            public FPMedia sourceMedia;
        }

        private static readonly Dictionary<RuntimeSurfaceMaterialKey, Material> runtimeSurfaceMaterialInstances = new Dictionary<RuntimeSurfaceMaterialKey, Material>();
        private static readonly Dictionary<RuntimeSurfaceMaterialKey, List<GameObject>> runtimeSurfaceRendererBatches = new Dictionary<RuntimeSurfaceMaterialKey, List<GameObject>>();
        ////private static readonly List<GameObject> staticBatchables = new List<GameObject>(); // TODO: Can't tell if this is any better or worse - needs more experimentation
        private static bool levelEventsHaveRegistered = false;

        private readonly int lightIntensityPropertyId = Shader.PropertyToID("_LightIntensity");

        protected RuntimeSurfaceMaterialKey runtimeSurfaceMaterialKey = new RuntimeSurfaceMaterialKey();

        protected Material surfaceMaterial;

        private FPLight fPLight;
        private bool isStaticBatchable;

        private static void OnLevelOpened(string levelName)
        {
            foreach (var batch in runtimeSurfaceRendererBatches.Values)
            {
                var batchParent = new GameObject("Static Batch");
                batchParent.transform.SetParent(FPLevel.Instance.transform);

                StaticBatchingUtility.Combine(batch.ToArray(), batchParent);
            }

            // TODO: Can't tell if this is any better or worse - needs more experimentation
            ////var batchParent = new GameObject("Static Batch");
            ////batchParent.transform.SetParent(FPLevel.Instance.transform);

            ////StaticBatchingUtility.Combine(staticBatchables.ToArray(), batchParent);
        }

        private static void OnLevelClosed()
        {
            runtimeSurfaceMaterialInstances.Clear();
            runtimeSurfaceRendererBatches.Clear();
            ////staticBatchables.Clear(); // TODO: Can't tell if this is any better or worse - needs more experimentation
        }

        public void InitializeRuntimeSurface(FPLight fpLight, bool isStaticBatchable)
        {
            if (!levelEventsHaveRegistered)
            {
                levelEventsHaveRegistered = true;

                LevelData.OnLevelOpened += OnLevelOpened;
                LevelData.OnLevelClosed += OnLevelClosed;
            }

            this.fPLight = fpLight;
            this.isStaticBatchable = isStaticBatchable;

            var renderer = GetComponent<MeshRenderer>();

            runtimeSurfaceMaterialKey.sourceMaterial = renderer.sharedMaterial;
            runtimeSurfaceMaterialKey.sourceLight = fpLight;

            // RuntimeSurfaceLight.InitializeRuntimeSurface must not run before runtimeSurfaceMaterialKey is initialized
            if (runtimeSurfaceMaterialInstances.ContainsKey(runtimeSurfaceMaterialKey))
            {
                surfaceMaterial = runtimeSurfaceMaterialInstances[runtimeSurfaceMaterialKey];
            }
            else
            {
                surfaceMaterial = new Material(runtimeSurfaceMaterialKey.sourceMaterial);
                runtimeSurfaceMaterialInstances[runtimeSurfaceMaterialKey] = surfaceMaterial;
            }

            if (isStaticBatchable)
            {
                if (!runtimeSurfaceRendererBatches.ContainsKey(runtimeSurfaceMaterialKey))
                {
                    runtimeSurfaceRendererBatches[runtimeSurfaceMaterialKey] = new List<GameObject>();
                }

                runtimeSurfaceRendererBatches[runtimeSurfaceMaterialKey].Add(gameObject);
                ////staticBatchables.Add(gameObject); // TODO: Can't tell if this is any better or worse - needs more experimentation
            }

            renderer.sharedMaterial = surfaceMaterial;
        }

        protected virtual void Update()
        {
            surfaceMaterial.SetFloat(lightIntensityPropertyId, fPLight.CurrentIntensity);
        }
    }
}
