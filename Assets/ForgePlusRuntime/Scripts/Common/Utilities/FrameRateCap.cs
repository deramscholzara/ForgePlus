using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus
{
    // Caps the frame rate at about 60 while staying in sync with the display: VSync presents on every refresh at 60 Hz
    // or less, and on every whole-number multiple of refreshes needed to stay at or above 60 on faster displays (every
    // 2nd at 120 or 144 Hz). Application.targetFrameRate only applies where VSync is off (such as when a driver forces
    // it off)
    [AutoStaticsCleanup]
    public static partial class FrameRateCap
    {
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            var refreshRate = (float)Screen.currentResolution.refreshRateRatio.value;

            // A little slack, so 59.94 Hz counts as 60
            var refreshesPerFrame = refreshRate > 0f ? Mathf.FloorToInt((refreshRate / TargetFrameRate) + 0.05f) : 1;

            QualitySettings.vSyncCount = Mathf.Clamp(refreshesPerFrame, 1, 4);
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
