using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.ApplicationGeneral
{
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
        private CanvasGroup blockerCanvasGroup = null;

        [SerializeField]
        private float unblockedAlpha = 0f;

        [SerializeField]
        private float blockedAlpha = 0.8f;

        private int currentBlockingCount = 0;

        private float fadePosition = 0f;

        private CancellationTokenSource fadeCTS;

        public async void Block()
        {
            currentBlockingCount++;
            if (currentBlockingCount > 1)
            {
                return;
            }

            fadeCTS?.Cancel();

            OnChanged?.Invoke(true);

            blockerCanvasGroup.gameObject.SetActive(true);
            blockerCanvasGroup.blocksRaycasts = true;

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
            currentBlockingCount--;
            if (currentBlockingCount > 0)
            {
                return;
            }

            fadeCTS?.Cancel();

            OnChanged?.Invoke(false);

            blockerCanvasGroup.blocksRaycasts = false;

            fadeCTS = new CancellationTokenSource();
            try
            {
                await Fade(FadeDirection.Out, fadeCTS.Token);
            }
            catch
            {
                return;
            }

            blockerCanvasGroup.gameObject.SetActive(false);
        }

        private async Task Fade(FadeDirection direction, CancellationToken cancellationToken)
        {
            var deltaDuration = fadeDuration * (direction == FadeDirection.In ? 1f - fadePosition : fadePosition);
            var endTime = Time.realtimeSinceStartup + deltaDuration;

            while (Time.realtimeSinceStartup < endTime)
            {
                await Task.Yield();

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

                blockerCanvasGroup.alpha = Mathf.Lerp(unblockedAlpha, blockedAlpha, fadePosition);
            }

            fadePosition = Mathf.Clamp01(fadePosition);
        }

        private void Start()
        {
            if (fadeCTS == null)
            {
                Unblock();
            }
        }
    }
}
