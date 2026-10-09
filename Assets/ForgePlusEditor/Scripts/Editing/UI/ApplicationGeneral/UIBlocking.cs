using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.ApplicationGeneral
{
    // Fades in the UI's input blocker (which stops clicks reaching the UI and the level) while something is blocking input
    [RequireComponent(typeof(UIDocument))]
    public class UIBlocking : SingletonMonoBehaviour<UIBlocking>
    {
        public Action<bool> OnChanged;

        private enum FadeDirection
        {
            In,
            Out,
        }

        [SerializeField]
        private float fadeDuration = 1f / 3f;

        [SerializeField]
        private float unblockedOpacity = 0f;

        [SerializeField]
        private float blockedOpacity = 0.8f;

        private int currentBlockingCount = 0;
        private float fadePosition = 0f;
        private CancellationTokenSource fadeCTS;
        private VisualElement blocker;

        // The document's tree is built when it's enabled, so it's found when it's first needed
        private VisualElement Blocker
        {
            get
            {
                if (blocker == null)
                {
                    blocker = GetComponent<UIDocument>().rootVisualElement.Q("input-blocker");
                }

                return blocker;
            }
        }

        // While anything (such as a dialog, or loading) blocks input
        public bool IsBlocking
        {
            get
            {
                return currentBlockingCount > 0;
            }
        }

        public async void Block()
        {
            currentBlockingCount++;
            if (currentBlockingCount > 1)
            {
                return;
            }

            fadeCTS?.Cancel();

            OnChanged?.Invoke(true);

            Blocker.style.display = DisplayStyle.Flex;
            Blocker.pickingMode = PickingMode.Position;

            fadeCTS = new CancellationTokenSource();
            try
            {
                await Fade(FadeDirection.In, fadeCTS.Token);
            }
            catch
            {
                return;
            }
        }

        public async void Unblock()
        {
            // Unblocking more than was blocked would leave later blocks (such as a dialog's) uncounted
            if (currentBlockingCount == 0)
            {
                Debug.LogWarning("UIBlocking was unblocked more times than it was blocked.");
                return;
            }

            currentBlockingCount--;
            if (currentBlockingCount > 0)
            {
                return;
            }

            fadeCTS?.Cancel();

            OnChanged?.Invoke(false);

            Blocker.pickingMode = PickingMode.Ignore;

            fadeCTS = new CancellationTokenSource();
            try
            {
                await Fade(FadeDirection.Out, fadeCTS.Token);
            }
            catch
            {
                return;
            }

            Blocker.style.display = DisplayStyle.None;
        }

        private async Awaitable Fade(FadeDirection direction, CancellationToken cancellationToken)
        {
            var deltaDuration = fadeDuration * (direction == FadeDirection.In ? 1f - fadePosition : fadePosition);
            var endTime = Time.realtimeSinceStartup + deltaDuration;

            while (Time.realtimeSinceStartup < endTime)
            {
                await Awaitable.NextFrameAsync();

                if (!Application.isPlaying)
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                var timeRemaining = endTime - Time.realtimeSinceStartup;
                fadePosition = timeRemaining / fadeDuration;

                if (direction == FadeDirection.In)
                {
                    fadePosition = 1f - fadePosition;
                }

                Blocker.style.opacity = Mathf.Lerp(unblockedOpacity, blockedOpacity, fadePosition);
            }

            fadePosition = Mathf.Clamp01(fadePosition);
        }
    }
}
