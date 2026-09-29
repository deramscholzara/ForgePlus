using Unity.Jobs.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.Jobs
{
    // Leaves 30% of the machine's threads for everything else
    [AutoStaticsCleanup]
    public static partial class JobWorkerSettings
    {
        private const float WorkerShareOfThreads = 0.7f;

        private static int originalWorkerCount = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            originalWorkerCount = JobsUtility.JobWorkerCount;

            JobsUtility.JobWorkerCount = Mathf.Clamp(Mathf.FloorToInt(SystemInfo.processorCount * WorkerShareOfThreads), 1, JobsUtility.JobWorkerMaximumCount);

            // In the Editor this is when Play mode ends, so the Editor gets its own worker count back
            Application.quitting += Restore;
        }

        private static void Restore()
        {
            Application.quitting -= Restore;

            if (originalWorkerCount > 0)
            {
                JobsUtility.JobWorkerCount = originalWorkerCount;
            }
        }
    }
}
