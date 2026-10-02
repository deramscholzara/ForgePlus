using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.PolygonContainment;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using UnityEngine;
using static AlephOne.SoundManager;

namespace ForgePlus.Sound
{
    // Plays the open level's sounds, from the sounds file: one on its own (a preview, at a volume and pitch), and, while
    // listening, what the game would play where it is (in its tracker's polygon, facing its way): the polygon's ambient
    // sound (or its media's), its platform's while it moves, the sound sources it hears (by how far away and obstructed
    // each is), its random sound now and then, and platforms' sounds (as they start and stop moving, are obstructed, and
    // go into or out of media) from where they are. In no polygon, it hears none of them.
    // Runs after its tracker has found its polygon.
    [RequireComponent(typeof(PolygonContainmentTracker))]
    [DefaultExecutionOrder(PolygonContainmentTracker.ExecutionOrder + 100)]
    public class LevelSoundPlayback : MonoBehaviour
    {
        private const float TickDuration = 1f / map.TICKS_PER_SECOND;

        // The most ticks a slow frame catches up on (each of which may play a random sound)
        private const int MaximumTicksPerFrame = 10;

        // How much louder (than one playing) a sound must be to start it again (abortAmplitudeThreshold, in
        // OpenALManager.h), as a fraction of MAXIMUM_SOUND_VOLUME
        private const float AbortAmplitudeThreshold = SoundManagerEnums.MAXIMUM_SOUND_VOLUME / 6f / 256f;

        private readonly List<LevelSoundVoice> ambientVoices = new List<LevelSoundVoice>();
        private readonly List<LevelSoundVoice> oneShotVoices = new List<LevelSoundVoice>();
        private readonly ambient_sound_data[] ambientSounds = new ambient_sound_data[MAXIMUM_PROCESSED_AMBIENT_SOUNDS];
        private readonly System.Random random = new System.Random();

        private PolygonContainmentTracker tracker;
        private GameObject voicesHolder;
        private LevelSoundVoice previewVoice;

        private LevelSoundWorld soundWorld;
        private LevelEntity_Level worldLevel;
        private SoundsFile ambientSoundsFile;

        private Action<ambient_sound_data[]> addAmbientSources;
        private Action<short, short, short, int> directPlaySound;
        private Action<short, world_location3d> playSound;
        private Action<short, world_location3d> ignoreSound;

        private bool isListening;
        private float tickTime;

        // Whether it's listening, in a polygon, with a sounds file to play from (this frame)
        private bool isHearing;

        public static LevelSoundPlayback Instance { get; private set; }

        public PolygonContainmentTracker Tracker
        {
            get
            {
                return tracker;
            }
        }

        // Whether it plays what the game would play where it is
        public bool IsListening
        {
            get
            {
                return isListening;
            }
            set
            {
                if (isListening == value)
                {
                    return;
                }

                isListening = value;

                if (!isListening)
                {
                    StopListening(softly: true);
                }
            }
        }

        // Whether the ambient sound (an ambient sound image's, or a sound source's) has a sound in the sounds file
        public static bool CanPlayAmbientSound(short ambientSound)
        {
            var definition = get_ambient_sound_definition(ambientSound);

            return definition != null && CanPlaySound(definition.sound_index);
        }

        // Whether the random sound (a random sound image's) has a sound in the sounds file
        public static bool CanPlayRandomSound(short randomSound)
        {
            return CanPlaySound(RandomSoundIndexToSoundIndex(randomSound));
        }

        // Plays the ambient sound once, at the volume (as an ambient sound image, or a sound source next to the listener, plays)
        public void PreviewAmbientSound(short ambientSound, short volume)
        {
            var definition = get_ambient_sound_definition(ambientSound);
            if (definition != null)
            {
                Preview(definition.sound_index, volume, cstypes.FIXED_ONE);
            }
        }

        // Plays the random sound image's sound once, at a volume and pitch from its ranges (as the game picks them each
        // time it plays it: handle_random_sound_image, in map.cpp)
        public void PreviewRandomSound(random_sound_image_data image)
        {
            short volume = image.volume;
            int pitch = image.pitch;

            if (image.delta_volume != 0) volume += (short) (LocalRandom() % image.delta_volume);
            if (image.delta_pitch != 0) pitch += LocalRandom() % image.delta_pitch;

            Preview(RandomSoundIndexToSoundIndex(image.sound_index), volume, pitch);
        }

        // Plays the sound once (stopping the last one previewed), at the volume (from 0 to MAXIMUM_SOUND_VOLUME), and the
        // pitch (_fixed, FIXED_ONE being its own) as the game is asked to play it (SoundManager::CalculatePitchModifier)
        public void Preview(short soundIndex, short volume, int pitch)
        {
            var file = SoundsLoading.Instance.File;
            if (file == null)
            {
                return;
            }

            previewVoice.Stop(softly: false);

            if (TryGetClip(file, soundIndex, out var definition, out var clip))
            {
                previewVoice.SetVolume(volume * 1f / SoundManagerEnums.MAXIMUM_SOUND_VOLUME, pan: 0f);
                previewVoice.Play(soundIndex, clip, CalculatePitchModifier(definition, pitch), softStart: false);
            }
        }

        public void StopPreview()
        {
            previewVoice?.Stop(softly: false);
        }

        private static bool CanPlaySound(short soundIndex)
        {
            var file = SoundsLoading.Instance.File;
            var definition = file != null && soundIndex != cstypes.NONE ? file.Definition(soundIndex) : null;

            return definition != null && definition.sound_code != cstypes.NONE && definition.permutations > 0;
        }

        // A permutation of the sound (the next to play), as a clip
        private static bool TryGetClip(SoundsFile file, short soundIndex, out SoundDefinition definition, out AudioClip clip)
        {
            definition = soundIndex != cstypes.NONE ? file.Definition(soundIndex) : null;
            clip = null;

            if (definition == null || definition.sound_code == cstypes.NONE || definition.permutations <= 0)
            {
                return false;
            }

            clip = file.Clip(soundIndex, file.NextPermutation(soundIndex));

            return clip;
        }

        private void Awake()
        {
            Instance = this;

            tracker = GetComponent<PolygonContainmentTracker>();

            voicesHolder = new GameObject("Level Sounds");
            voicesHolder.transform.SetParent(transform, worldPositionStays: false);

            previewVoice = CreateVoice();

            for (var i = 0; i < ambientSounds.Length; i++)
            {
                ambientSounds[i] = new ambient_sound_data();
            }

            addAmbientSources = data => soundWorld.sound_add_ambient_sources_proc(data, ambientSoundsFile.SoundFile);
            directPlaySound = DirectPlaySound;
            playSound = PlaySound;
            ignoreSound = (soundIndex, source) => { };

            // Its sounds' clips go with the file they're from
            SoundsLoading.Instance.OnDataLoadCompleted += OnSoundsLoadCompleted;

            LevelEntity_Platform.OnRuntimeSound += OnPlatformSound;
        }

        private void OnDestroy()
        {
            SoundsLoading.Instance.OnDataLoadCompleted -= OnSoundsLoadCompleted;

            LevelEntity_Platform.OnRuntimeSound -= OnPlatformSound;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDisable()
        {
            StopListening(softly: false);
            StopPreview();
        }

        private void LateUpdate()
        {
            var levelEntity = LevelEntity_Level.Instance;
            var file = isListening ? SoundsLoading.Instance.File : null;

            if (levelEntity != worldLevel)
            {
                // Another level (or none) starts from silence
                StopListening(softly: false);

                worldLevel = levelEntity;
                soundWorld = levelEntity ? new LevelSoundWorld(levelEntity, LocalRandom) : null;
            }

            isHearing = file != null && soundWorld != null && tracker.PolygonIndex != cstypes.NONE;

            if (isHearing)
            {
                soundWorld.Listener = new world_location3d
                {
                    point = tracker.Location,
                    polygon_index = tracker.PolygonIndex,
                    yaw = Yaw(),
                };

                UpdateAmbientSounds(file);
            }

            // Platforms' places relative to media are kept (as they move) whether or not they're heard
            soundWorld?.update_platforms_for_media(isHearing ? playSound : ignoreSound);

            if (isHearing)
            {
                tickTime += Time.unscaledDeltaTime;
                for (var tick = 0; tickTime >= TickDuration; tick++)
                {
                    tickTime -= TickDuration;

                    if (tick < MaximumTicksPerFrame)
                    {
                        soundWorld.handle_random_sound_image(directPlaySound);
                    }
                }
            }
            else
            {
                StopAmbientSounds(softly: true);
                tickTime = 0f;
            }

            previewVoice.Update();
            UpdateVoices(ambientVoices);
            UpdateVoices(oneShotVoices);
        }

        // As UpdateAmbientSoundSources, in SoundManager.cpp: each ambient sound decided on plays (from silence, one
        // permutation after another), at its volume, and each that's no longer decided on goes quiet and stops
        private void UpdateAmbientSounds(SoundsFile file)
        {
            ambientSoundsFile = file;
            SoundManager.UpdateAmbientSoundSources(ambientSounds, addAmbientSources);

            foreach (var voice in ambientVoices)
            {
                if (voice.IsPlaying && !IsDecidedOn(voice.SoundIndex))
                {
                    voice.Stop(softly: true);
                }
            }

            foreach (var ambient in ambientSounds)
            {
                if (!map.SLOT_IS_USED(ambient.flags))
                {
                    continue;
                }

                var voice = ambientVoices.Find(playing => playing.SoundIndex == ambient.sound_index);
                var pan = LevelSoundVoice.Pan(ambient.variables.left_volume, ambient.variables.right_volume);
                var volume = ambient.variables.volume * 1f / SoundManagerEnums.MAXIMUM_SOUND_VOLUME;

                if (voice != null)
                {
                    voice.KeepPlaying();
                    voice.SetVolume(volume, pan);
                }
                else if (TryGetClip(file, ambient.sound_index, out var definition, out var clip))
                {
                    // Each time it ends, it carries on with the next permutation (as the game's soft rewind does)
                    var soundIndex = ambient.sound_index;
                    voice = FreeVoice(ambientVoices);
                    voice.Play(soundIndex, clip, CalculatePitchModifier(definition, cstypes.FIXED_ONE), softStart: true,
                        nextClip: () => TryGetClip(file, soundIndex, out _, out var nextClip) ? nextClip : null);
                    voice.SetVolume(volume, pan);
                }
            }
        }

        // As SoundManager::DirectPlaySound: from the direction (NONE for none), relative to the way the listener faces, at
        // the volume; or, with no direction, at full volume (as Aleph One plays a 2D sound that isn't panned:
        // SoundPlayer::SetUpALSourceIdle)
        private void DirectPlaySound(short soundIndex, short direction, short volume, int pitch)
        {
            if (direction != cstypes.NONE)
            {
                AngleAndVolumeToStereoVolume((short) (direction - soundWorld.Listener.yaw), volume, out var rightVolume, out var leftVolume);
                BufferSound(soundIndex, pitch, volume * 1f / SoundManagerEnums.MAXIMUM_SOUND_VOLUME, LevelSoundVoice.Pan(leftVolume, rightVolume), isPanning: true);
            }
            else
            {
                BufferSound(soundIndex, pitch, 1f, 0f, isPanning: false);
            }
        }

        // As SoundManager::PlaySound, from a source in the level (with 3D sounds off, as Aleph One's preferences have
        // them): as loud, and from the side, it is at the listener as it starts (CalculateInitialSoundVariables)
        private void PlaySound(short soundIndex, world_location3d source)
        {
            var file = SoundsLoading.Instance.File;
            if (soundIndex == cstypes.NONE || file == null || !isHearing)
            {
                return;
            }

            var variables = CalculateSoundVariables(file.SoundFile, soundIndex, source, soundWorld.Listener, soundWorld.sound_obstructed_proc);
            BufferSound(soundIndex, cstypes.FIXED_ONE, variables.volume * 1f / SoundManagerEnums.MAXIMUM_SOUND_VOLUME,
                LevelSoundVoice.Pan(variables.left_volume, variables.right_volume), isPanning: true);
        }

        // As SoundManager::BufferSound and ManageSound: a sound too quiet to hear isn't played, and one that's playing
        // already (unless it doesn't self-abort) starts again with the new one's volume rather than playing twice, if it
        // can be restarted, and it isn't panned or the new one is louder by more than the abort threshold
        // (UpdateExistingPlayer); otherwise the new one isn't played
        private void BufferSound(short soundIndex, int pitch, float gain, float pan, bool isPanning)
        {
            var file = SoundsLoading.Instance.File;
            if (soundIndex == cstypes.NONE || file == null || !TryGetClip(file, soundIndex, out var definition, out var clip) || gain <= 0f)
            {
                return;
            }

            LevelSoundVoice voice = null;
            if ((definition.flags & sound_definitions._sound_does_not_self_abort) == 0)
            {
                voice = oneShotVoices.Find(playing => playing.SoundIndex == soundIndex && !playing.IsStopping);

                if (voice != null &&
                    ((definition.flags & sound_definitions._sound_cannot_be_restarted) != 0 ||
                     !(!isPanning || gain + AbortAmplitudeThreshold > voice.Volume)))
                {
                    return;
                }
            }

            voice?.Stop(softly: false);
            voice = FreeVoice(oneShotVoices);
            voice.SetVolume(gain, pan);
            voice.Play(soundIndex, clip, CalculatePitchModifier(definition, pitch), softStart: false);
        }

        // As play_platform_sound (platforms.cpp), from the platform's polygon
        private void OnPlatformSound(LevelEntity_Platform platform, short soundCode, bool isExtending, bool isFullyContracted)
        {
            if (!isHearing || platform.ParentLevel != worldLevel)
            {
                return;
            }

            var soundIndex = platforms.get_platform_sound(worldLevel.Level, platform.NativeIndex, soundCode, isExtending, isFullyContracted);
            if (soundIndex != cstypes.NONE)
            {
                PlaySound(soundIndex, soundWorld.polygon_sound_source(platform.NativeObject.polygon_index));
            }
        }

        private bool IsDecidedOn(short soundIndex)
        {
            foreach (var ambient in ambientSounds)
            {
                if (map.SLOT_IS_USED(ambient.flags) && ambient.sound_index == soundIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private void StopListening(bool softly)
        {
            StopAmbientSounds(softly);

            foreach (var voice in oneShotVoices)
            {
                voice.Stop(softly);
            }

            soundWorld?.ResetRandomSoundImages();
            tickTime = 0f;
        }

        private void StopAmbientSounds(bool softly)
        {
            foreach (var voice in ambientVoices)
            {
                voice.Stop(softly);
            }
        }

        private void OnSoundsLoadCompleted(bool isLoaded)
        {
            StopListening(softly: false);
            StopPreview();
        }

        // The way it faces, as a Marathon angle (0 along x, a quarter circle along y)
        private short Yaw()
        {
            var forward = Vector3.Cross(transform.right, Vector3.up);

            return world.NORMALIZE_ANGLE(Mathf.RoundToInt(Mathf.Atan2(-forward.z, forward.x) * world.NUMBER_OF_ANGLES / (2f * Mathf.PI)));
        }

        private LevelSoundVoice FreeVoice(List<LevelSoundVoice> voices)
        {
            var voice = voices.Find(candidate => !candidate.IsPlaying);
            if (voice == null)
            {
                voice = CreateVoice();
                voices.Add(voice);
            }

            return voice;
        }

        private LevelSoundVoice CreateVoice()
        {
            return new LevelSoundVoice(voicesHolder);
        }

        private static void UpdateVoices(List<LevelSoundVoice> voices)
        {
            foreach (var voice in voices)
            {
                voice.Update();
            }
        }

        private ushort LocalRandom()
        {
            return (ushort) random.Next(ushort.MaxValue + 1);
        }
    }
}
