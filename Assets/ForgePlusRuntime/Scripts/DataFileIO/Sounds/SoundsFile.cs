using AlephOne;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using Random = System.Random;

namespace ForgePlus.DataFileIO
{
    // A Marathon 2/Infinity sounds file's definitions (which sounds a level can use), and their permutations as clips
    [NoAutoStaticsCleanup]
    public class SoundsFile : IFileLoadable
    {
        private static readonly Random random = new Random();

        private readonly M2SoundFile soundFile = new M2SoundFile();

        // By sound and permutation (null for one that couldn't be read)
        private readonly Dictionary<(short Sound, short Permutation), AudioClip> clips = new Dictionary<(short Sound, short Permutation), AudioClip>();

        public string Path { get; private set; }

        public SoundFile SoundFile
        {
            get
            {
                return soundFile;
            }
        }

        // For what Aleph One picks with local_random (permutations, and random sounds' volumes and pitches)
        public static ushort LocalRandom()
        {
            return (ushort) random.Next(ushort.MaxValue + 1);
        }

        public void Load(string fileName)
        {
            if (!soundFile.Open(new FileSpecifier(fileName)))
            {
                throw new IOException($"\"{fileName}\" is not a readable Marathon 2 or Infinity sounds file.");
            }

            Path = fileName;
        }

        // Aleph One plays nothing for a sound with no code or no permutations
        public bool CanPlay(short soundIndex)
        {
            var definition = Definition(soundIndex);

            return definition != null && definition.sound_code != cstypes.NONE && definition.permutations > 0;
        }

        // How it fades with distance (quiet, normal or loud), from the first source that has it, or NONE
        public short Behavior(short soundIndex)
        {
            for (var source = 0; source < soundFile.SourceCount(); source++)
            {
                var definition = soundFile.GetSoundDefinition(source, soundIndex);

                if (definition != null && definition.sound_code != cstypes.NONE)
                {
                    return definition.behavior_index;
                }
            }

            return cstypes.NONE;
        }

        // The one Aleph One plays (SoundManager::GetSoundDefinition), or null
        public SoundDefinition Definition(short soundIndex)
        {
            return SoundManager.GetSoundDefinition(soundFile, soundIndex);
        }

        // SoundManager::GetRandomSoundPermutation
        public short NextPermutation(short soundIndex)
        {
            return SoundManager.GetRandomSoundPermutation(Definition(soundIndex), LocalRandom);
        }

        // Made the first time it's asked for; null if it has none
        public AudioClip Clip(short soundIndex, short permutation)
        {
            if (clips.TryGetValue((soundIndex, permutation), out var clip))
            {
                return clip;
            }

            var definition = Definition(soundIndex);
            if (definition != null && definition.sound_code != cstypes.NONE && permutation >= 0 && permutation < definition.sounds.Count)
            {
                try
                {
                    clip = CreateClip($"Sound {soundIndex} ({permutation})", definition.sounds[permutation], soundFile.GetSoundData(definition, permutation));
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Sound {soundIndex} (permutation {permutation}) could not be read from \"{Path}\": {exception}");
                }
            }

            clips[(soundIndex, permutation)] = clip;

            return clip;
        }

        public void ReleaseClips()
        {
            foreach (var clip in clips.Values)
            {
                if (clip)
                {
                    UnityEngine.Object.Destroy(clip);
                }
            }

            clips.Clear();
        }

        // 8-bit samples are unsigned (128 is silence), and 16-bit ones are signed and big-endian (see
        // SoundHeader.LoadData); stereo ones alternate left and right
        private static AudioClip CreateClip(string name, SoundHeader header, byte[] data)
        {
            if (data == null || header.length <= 0)
            {
                return null;
            }

            var channels = header.stereo ? 2 : 1;
            var bytesPerSample = header.audio_format == SoundManagerEnums.AudioFormat._16_bit ? 2 : 1;
            var frameCount = data.Length / (bytesPerSample * channels);
            var frequency = Mathf.RoundToInt(header.rate / 65536f);

            if (frameCount <= 0 || frequency <= 0)
            {
                return null;
            }

            var samples = new float[frameCount * channels];
            if (bytesPerSample == 2)
            {
                for (var i = 0; i < samples.Length; i++)
                {
                    var sample = header.little_endian ?
                        (short) (data[i * 2] | (data[i * 2 + 1] << 8)) :
                        (short) ((data[i * 2] << 8) | data[i * 2 + 1]);

                    samples[i] = sample / 32768f;
                }
            }
            else
            {
                for (var i = 0; i < samples.Length; i++)
                {
                    samples[i] = (data[i] - 128) / 128f;
                }
            }

            var clip = AudioClip.Create(name, frameCount, channels, frequency, stream: false);
            clip.SetData(samples, 0);

            return clip;
        }
    }
}
