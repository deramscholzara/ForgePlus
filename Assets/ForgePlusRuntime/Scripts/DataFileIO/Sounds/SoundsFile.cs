using AlephOne;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    // The sound definitions of a Marathon 2/Infinity sounds file, for knowing which sounds a level can use, and their
    // permutations as clips, for playing them
    public class SoundsFile : IFileLoadable
    {
        private readonly M2SoundFile soundFile = new M2SoundFile();

        // Each clip made so far, by sound and permutation (null for one that couldn't be read)
        private readonly Dictionary<(short Sound, short Permutation), AudioClip> clips = new Dictionary<(short Sound, short Permutation), AudioClip>();

        private readonly System.Random random = new System.Random();

        public string Path { get; private set; }

        public SoundFile SoundFile
        {
            get
            {
                return soundFile;
            }
        }

        public void Load(string fileName)
        {
            if (!soundFile.Open(new FileSpecifier(fileName)))
            {
                throw new IOException($"\"{fileName}\" is not a readable Marathon 2 or Infinity sounds file.");
            }

            Path = fileName;
        }

        // Whether the sound has something to play in any source (Aleph One falls back to the 8-bit source when the
        // 16-bit one has no permutations, and plays nothing for a sound with no code or no permutations)
        public bool HasSound(short soundIndex)
        {
            for (var source = 0; source < soundFile.SourceCount(); source++)
            {
                var definition = soundFile.GetSoundDefinition(source, soundIndex);

                if (definition != null && definition.sound_code != cstypes.NONE && definition.permutations > 0)
                {
                    return true;
                }
            }

            return false;
        }

        // How the sound fades with distance (its behavior: quiet, normal or loud), from the first source that has it,
        // or NONE if none does
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

        // The sound's definition that Aleph One plays it from: the 16-bit one, unless only the 8-bit one has it
        // (SoundManager::GetSoundDefinition), or null if there's none
        public SoundDefinition Definition(short soundIndex)
        {
            return SoundManager.GetSoundDefinition(soundFile, soundIndex);
        }

        // A permutation of the sound to play next, each playing once before any plays again
        // (SoundManager::GetRandomSoundPermutation)
        public short NextPermutation(short soundIndex)
        {
            return SoundManager.GetRandomSoundPermutation(Definition(soundIndex), LocalRandom);
        }

        // The permutation of the sound as a clip (made the first time it's asked for), or null if it has none
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

        // Destroys the clips made so far (as the file is closed)
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
            var bytesPerSample = header.audio_format == AudioFormat._16_bit ? 2 : 1;
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

        private ushort LocalRandom()
        {
            return (ushort) random.Next(ushort.MaxValue + 1);
        }
    }
}
