using AlephOne;
using System;
using UnityEngine;

namespace ForgePlus.Sound
{
    // A sound playing as Aleph One plays a 2D one (SoundPlayer::SetUpALSourceIdle). One that carries on (an ambient one)
    // schedules each next clip on a second source, with no gap, as a soft rewind does (SoundPlayer::Rewind).
    public class LevelSoundVoice
    {
        private const float SmoothVolumeTransitionThreshold = 0.1f;
        private const float SmoothVolumeTransitionTime = 0.3f;

        // So a carrying-on sound's first clip starts (and so ends) exactly when scheduled
        private const double ScheduleLead = 0.05;

        private readonly GameObject holder;

        // The playing source, and the one a carrying-on sound's next clip is scheduled on
        private readonly AudioSource[] sources = new AudioSource[2];
        private int current;

        private Func<AudioClip> nextClip;
        private double currentEnd;
        private double nextEnd;
        private bool isNextScheduled;

        private float pitch = 1f;
        private float pan;

        private float volume;
        private float targetVolume;
        private float transitionStartVolume;
        private float transitionStartTime;
        private bool isTransitioning;
        private bool isStopping;

        public LevelSoundVoice(GameObject holder)
        {
            this.holder = holder;
        }

        // NONE while it's free
        public short SoundIndex { get; private set; } = cstypes.NONE;

        public bool IsPlaying
        {
            get
            {
                return SoundIndex != cstypes.NONE;
            }
        }

        // The gain it's playing at, or going to
        public float Volume
        {
            get
            {
                return targetVolume;
            }
        }

        public bool IsStopping
        {
            get
            {
                return isStopping;
            }
        }

        // From 0 to MAXIMUM_SOUND_VOLUME, as 0 to 1
        public static float ToGain(short volume)
        {
            return volume * 1f / SoundManagerEnums.MAXIMUM_SOUND_VOLUME;
        }

        // -1 (left) to 1 (right), as Aleph One pans: up to 30 degrees to either side, from the average of the angles whose
        // cosine and sine are the left and right gains
        public static float Pan(short leftVolume, short rightVolume)
        {
            var leftGain = Mathf.Min(ToGain(leftVolume), 1f);
            var rightGain = Mathf.Min(ToGain(rightVolume), 1f);

            var pan = (Mathf.Acos(Mathf.Max(leftGain, 0f)) + Mathf.Asin(Mathf.Max(rightGain, 0f))) / Mathf.PI;
            pan = 2f * pan - 1f;
            pan *= 0.5f;

            return Mathf.Clamp(Mathf.Asin(pan) / (Mathf.PI / 6f), -1f, 1f);
        }

        // With nextClip, carries on with the clip it gives each time one ends (until it gives none)
        public void Play(short soundIndex, AudioClip clip, float pitch, bool softStart, Func<AudioClip> nextClip = null)
        {
            StopSources();

            SoundIndex = soundIndex;
            this.pitch = pitch;
            this.nextClip = nextClip;

            current = 0;
            var source = Prepare(current, clip);

            if (nextClip != null)
            {
                var start = AudioSettings.dspTime + ScheduleLead;
                source.PlayScheduled(start);
                currentEnd = start + Duration(clip);

                ScheduleNext();
            }
            else
            {
                source.Play();
            }

            isStopping = false;
            isTransitioning = false;
            volume = softStart ? 0f : targetVolume;
            Apply();
        }

        // Changes of more than a tenth of full volume take 300 milliseconds
        public void SetVolume(float newVolume, float pan)
        {
            this.pan = pan;
            foreach (var source in sources)
            {
                if (source)
                {
                    source.panStereo = pan;
                }
            }

            if (isStopping)
            {
                return;
            }

            SetTargetVolume(newVolume, immediately: !IsSounding());
        }

        // Such as for a sound that's heard again while going quiet
        public void KeepPlaying()
        {
            isStopping = false;
        }

        public void Stop(bool softly)
        {
            if (!IsPlaying)
            {
                return;
            }

            if (softly && IsSounding() && volume > 0f)
            {
                isStopping = true;
                SetTargetVolume(0f, immediately: false);
                return;
            }

            StopSources();

            SoundIndex = cstypes.NONE;
            nextClip = null;
            isStopping = false;
            isTransitioning = false;
            volume = targetVolume = 0f;
        }

        // Each frame: carries on its volume's change, moves on to its next clip, and frees it once it's done
        public void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (isNextScheduled && AudioSettings.dspTime >= currentEnd)
            {
                current = 1 - current;
                currentEnd = nextEnd;

                ScheduleNext();
            }

            if (isTransitioning)
            {
                var progress = Mathf.Min((Time.unscaledTime - transitionStartTime) / SmoothVolumeTransitionTime, 1f);
                volume = Mathf.Lerp(transitionStartVolume, targetVolume, progress);

                if (progress >= 1f)
                {
                    isTransitioning = false;
                }
            }

            Apply();

            if ((isStopping && !isTransitioning) || !IsSounding())
            {
                Stop(softly: false);
            }
        }

        // From when the playing one ends (or now, if that's passed)
        private void ScheduleNext()
        {
            isNextScheduled = false;

            var clip = nextClip?.Invoke();
            if (!clip)
            {
                return;
            }

            var start = Math.Max(currentEnd, AudioSettings.dspTime);
            Prepare(1 - current, clip).PlayScheduled(start);
            nextEnd = start + Duration(clip);
            isNextScheduled = true;
        }

        private AudioSource Prepare(int index, AudioClip clip)
        {
            var source = sources[index];
            if (!source)
            {
                source = holder.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                sources[index] = source;
            }

            source.clip = clip;
            source.loop = false;
            source.pitch = pitch;
            source.panStereo = pan;
            source.volume = Mathf.Clamp01(volume);

            return source;
        }

        // Its playing source is playing (or scheduled to), or its next clip is (as one hands over to the other)
        private bool IsSounding()
        {
            var source = sources[current];
            var next = sources[1 - current];

            return (source && source.isPlaying) || (isNextScheduled && next && next.isPlaying);
        }

        private double Duration(AudioClip clip)
        {
            return clip.samples / (double) clip.frequency / Math.Max(pitch, 0.01f);
        }

        private void StopSources()
        {
            foreach (var source in sources)
            {
                if (source)
                {
                    source.Stop();
                    source.clip = null;
                }
            }

            isNextScheduled = false;
        }

        private void SetTargetVolume(float newVolume, bool immediately)
        {
            if (immediately || (!isTransitioning && Mathf.Abs(newVolume - volume) < SmoothVolumeTransitionThreshold))
            {
                targetVolume = volume = newVolume;
                isTransitioning = false;
                Apply();
                return;
            }

            if (isTransitioning && Mathf.Approximately(newVolume, targetVolume))
            {
                return;
            }

            transitionStartVolume = volume;
            transitionStartTime = Time.unscaledTime;
            targetVolume = newVolume;
            isTransitioning = true;
        }

        private void Apply()
        {
            foreach (var source in sources)
            {
                if (source)
                {
                    source.volume = Mathf.Clamp01(volume);
                }
            }
        }
    }
}
