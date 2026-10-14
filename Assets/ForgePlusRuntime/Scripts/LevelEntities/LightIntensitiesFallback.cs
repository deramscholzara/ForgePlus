using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace RuntimeCore.Entities
{
    // Full-bright light intensities, bound as _LightIntensities whenever no level's lights are (in Edit mode, in
    // material and Shader Graph previews, before a level opens, and after Play mode ends), so shaders that read it are
    // never drawn without it (which D3D12 skips, with a warning). LevelEntity_Light binds its own while a level's
    // lights exist, and binds this again when it releases them.
    [NoAutoStaticsCleanup]
    public static class LightIntensitiesFallback
    {
        // As many as the smallest buffer of a level's lights (reads past its end give 0, which is unlit)
        private const int Capacity = 256;
        private const float FullBright = 1f;

        // Kept across Play sessions (it's never changed), and released before the scripts reload
        private static GraphicsBuffer buffer;

        public static void Bind()
        {
            if (buffer == null || !buffer.IsValid())
            {
                buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Capacity, sizeof(float)) { name = "Light Intensities (Fallback)" };

                var intensities = new float[Capacity];
                for (var i = 0; i < intensities.Length; i++)
                {
                    intensities[i] = FullBright;
                }

                buffer.SetData(intensities);
            }

            Shader.SetGlobalBuffer(LevelEntity_Light.lightIntensitiesGlobalPropertyId, buffer);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BindOnStart()
        {
            Bind();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void BindInEditor()
        {
            Bind();

            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Release;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Release;
            UnityEditor.EditorApplication.quitting -= Release;
            UnityEditor.EditorApplication.quitting += Release;
        }

        private static void Release()
        {
            buffer?.Release();
            buffer = null;
        }
#endif
    }
}
