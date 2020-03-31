using ForgePlus.LevelManipulation.Utilities;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Weland;


namespace ForgePlus.LevelManipulation
{
    public class FPMedia : IFPManipulatable<Media>, IFPDestructionPreparable
    {
        public short? Index { get; set; }
        public Media WelandObject { get; set; }

        public FPLevel FPLevel { private get; set; }

        public float CurrentHeight { get; private set; }

        private CancellationTokenSource synchronizationLoopCTS;

        public FPMedia(short index, Media media, FPLevel fpLevel)
        {
            Index = index;
            WelandObject = media;
            FPLevel = fpLevel;

            BeginRuntimeStyleBehavior();
        }

        public void PrepareForDestruction()
        {
            synchronizationLoopCTS?.Cancel();
            synchronizationLoopCTS = null;
        }

        public async void BeginRuntimeStyleBehavior()
        {
            synchronizationLoopCTS?.Cancel();

            synchronizationLoopCTS = new CancellationTokenSource();
            var cancellationToken = synchronizationLoopCTS.Token;

            while (!cancellationToken.IsCancellationRequested && Application.isPlaying)
            {
                var lowHeight = (float)WelandObject.Low / GeometryUtilities.WorldUnitIncrementsPerMeter;
                var highHeight = (float)WelandObject.High / GeometryUtilities.WorldUnitIncrementsPerMeter;

                var intensity = (float)FPLevel.FPLights[WelandObject.LightIndex].CurrentIntensity;
                intensity = Mathf.Max(intensity, (float)WelandObject.MinimumLightIntensity);

                var currentHeight = Mathf.Lerp(lowHeight, highHeight, intensity);

                CurrentHeight = currentHeight;

                await Task.Yield();
            }
        }
    }
}
