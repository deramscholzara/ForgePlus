using AlephOne;
using System;
using UnityEngine;

namespace ForgePlus.Sound
{
    // A sound playing as Aleph One plays a 2D one (SoundPlayer::SetUpALSourceIdle): at a volume, panned between left
    // and right by its left and right volumes (each up to MAXIMUM_SOUND_VOLUME), with changes of more than a tenth of
    // full volume (and starting and stopping softly) taking 300 milliseconds.
    // A sound that carries on (an ambient one) plays another clip as each ends, with no gap between them, as a soft
    // rewind does (SoundPlayer::Rewind): the next is scheduled on a second source as the one before it starts.
    public class LevelSoundVoice
    {
        private const float SmoothVolumeTransitionThreshold = 0.1f;
        private const float SmoothVolumeTransitionTime = 0.3f;

        // How far ahead a carrying-on sound's first clip is scheduled, so its start (and so its end) is exact
        private const double ScheduleLead = 0.05;

        private readonly GameObject holder;

        // The playing source, and (for a sound that carries on) the one its next clip is scheduled on
        private readonly AudioSource[] sources = new AudioSource[2];
        private int current;

        // For a sound that carries on, the clip it plays next (or null for none)
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

        // The sound playing (NONE while it's free)
        public short SoundIndex { get; private set; } = cstypes.NONE;

        public bool IsPlaying
        {
            get
            {
                return SoundIndex != cstypes.NONE;
            }
        }

        // How loud it's playing, or going to (1 for MAXIMUM_SOUND_VOLUME)
        public float Volume
        {
            get
            {
                return targetVolume;
            }
        }

        // Whether it's going quiet, to stop
        public bool IsStopping
        {
            get
            {
                return isStopping;
            }
        }

        // The pan (-1 for left, to 1 for right) Aleph One plays at for the left and right volumes: a position up to
        // 30 degrees to either side, from the average of the angles whose cosine and sine are the left and right gains
        public static float Pan(short leftVolume, short rightVolume)
        {
            var leftGain = Mathf.Min(leftVolume * 1f / SoundManagerEnums.MAXIMUM_SOUND_VOLUME, 1f);
            var rightGain = Mathf.Min(rightVolume * 1f / SoundManagerEnums.MAXIMUM_SOUND_VOLUME, 1f);

            var pan = (Mathf.Acos(Mathf.Max(leftGain, 0f)) + Mathf.Asin(Mathf.Max(rightGain, 0f))) / Mathf.PI;
            pan = 2f * pan - 1f;
            pan *= 0.5f;

            return Mathf.Clamp(Mathf.Asin(pan) / (Mathf.PI / 6f), -1f, 1f);
        }

        // Plays the clip once, or, with nextClip, carries on with the clip it gives each time one ends (until it gives
        // none); from silence when it starts softly
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

        // Its volume (1 for MAXIMUM_SOUND_VOLUME) and pan
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

        // Keeps playing (rather than going quiet to stop), such as for a sound that's heard again
        public void KeepPlaying()
        {
            isStopping = false;
        }

        // Goes quiet, then stops (or stops now)
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

        // Each frame: carries on its volume's change, moves on to its next clip once the last has ended (scheduling the
        // one after it), and frees it once it's done
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

        // The clip after the playing one, from when the playing one ends (or now, if that's passed)
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

        // Its playing source is playing (or scheduled to), or its next clip is about to (as one hands over to the other)
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
