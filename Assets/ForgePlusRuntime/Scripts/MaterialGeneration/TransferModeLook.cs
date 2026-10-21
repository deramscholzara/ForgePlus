using System;
using AlephOne;
using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;

namespace RuntimeCore.Materials
{
    // How a transfer mode draws a surface, which its materials hold (SurfaceMaterials): a motion of the texture (chosen by
    // the face shaders' _TRANSFER_MOTION keyword, at the speeds in _TransferModeParameters), static in place of the
    // texture (_TRANSFER_STATIC), and the texture's scale (_TransferModeScale). Transfer modes that draw the same way
    // (such as Pulsate and Wobble) have equal looks, so their surfaces share materials. Landscapes have their own shader,
    // and modes not drawn here (such as the fades) draw as Normal.
    public readonly struct TransferModeLook : IEquatable<TransferModeLook>
    {
        public enum Motions
        {
            None,
            Slide,
            Wobble,
            Wander,
        }

        private static readonly string[] motionKeywords =
        {
            "_TRANSFER_MOTION_NONE",
            "_TRANSFER_MOTION_SLIDE",
            "_TRANSFER_MOTION_WOBBLE",
            "_TRANSFER_MOTION_WANDER",
        };

        private const string StaticKeyword = "_TRANSFER_STATIC";

        private static readonly int parametersPropertyId = Shader.PropertyToID("_TransferModeParameters");
        private static readonly int scalePropertyId = Shader.PropertyToID("_TransferModeScale");

        public readonly Motions Motion;

        // Slide speeds (x horizontal, y vertical), wobble speed (z) and wander speed (w)
        public readonly Vector4 Parameters;

        // 0 for the texture's own scale, so the default look is Normal
        private readonly float scale;

        public readonly bool IsStatic;

        private TransferModeLook(Motions motion, Vector4 parameters, float scale = 0f, bool isStatic = false)
        {
            Motion = motion;
            Parameters = parameters;
            this.scale = scale;
            IsStatic = isStatic;
        }

        public float Scale => scale > 0f ? scale : 1f;

        public static TransferModeLook For(short transferMode)
        {
            switch (transferMode)
            {
                case map._xfer_static:
                    return new TransferModeLook(Motions.None, Vector4.zero, isStatic: true);
                case map._xfer_2x:
                    return new TransferModeLook(Motions.None, Vector4.zero, scale: 2f);
                case map._xfer_4x:
                    return new TransferModeLook(Motions.None, Vector4.zero, scale: 4f);
            }

            switch ((TransferModes)transferMode)
            {
                case TransferModes.Pulsate:
                case TransferModes.Wobble:
                    return new TransferModeLook(Motions.Wobble, new Vector4(0f, 0f, 2f, 0f));
                case TransferModes.WobbleFast:
                    return new TransferModeLook(Motions.Wobble, new Vector4(0f, 0f, 20f, 0f));
                case TransferModes.HorizontalSlide:
                    return new TransferModeLook(Motions.Slide, new Vector4(-1f / 8f, 0f, 0f, 0f));
                case TransferModes.HorizontalSlideFast:
                    return new TransferModeLook(Motions.Slide, new Vector4(-2f / 8f, 0f, 0f, 0f));
                case TransferModes.VerticalSlide:
                    return new TransferModeLook(Motions.Slide, new Vector4(0f, 1f / 8f, 0f, 0f));
                case TransferModes.VerticalSlideFast:
                    return new TransferModeLook(Motions.Slide, new Vector4(0f, 2f / 8f, 0f, 0f));
                case TransferModes.Wander:
                    return new TransferModeLook(Motions.Wander, new Vector4(0f, 0f, 0f, 1f));
                case TransferModes.WanderFast:
                    return new TransferModeLook(Motions.Wander, new Vector4(0f, 0f, 0f, 2f));
                default:
                    return default;
            }
        }

        // Exactly one motion keyword is on, as shader_feature enum keywords have no defined variant with none
        public void ApplyTo(Material material)
        {
            for (var i = 0; i < motionKeywords.Length; i++)
            {
                if (i == (int)Motion)
                {
                    material.EnableKeyword(motionKeywords[i]);
                }
                else
                {
                    material.DisableKeyword(motionKeywords[i]);
                }
            }

            if (IsStatic)
            {
                material.EnableKeyword(StaticKeyword);
            }
            else
            {
                material.DisableKeyword(StaticKeyword);
            }

            material.SetVector(parametersPropertyId, Parameters);
            material.SetFloat(scalePropertyId, Scale);
        }

        public bool Equals(TransferModeLook other)
        {
            return Motion == other.Motion && Parameters == other.Parameters && scale == other.scale && IsStatic == other.IsStatic;
        }

        public override bool Equals(object obj)
        {
            return obj is TransferModeLook other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Motion, Parameters, scale, IsStatic);
        }
    }
}
