using ForgePlus.ApplicationGeneral;
using System;
using Unity.Properties;
using UnityEngine;

namespace ForgePlus.UI
{
    // SettingsManager's settings for the UI; each property has its setting's name, so a change notifies it directly
    public class SettingsViewModel : BindableObject, IDisposable
    {
        public SettingsViewModel()
        {
            SettingsManager.Instance.OnSettingChanged += Notify;
            SettingsManager.Instance.OnObjectVisibilityChanged += OnObjectVisibilityChanged;
        }

        [CreateProperty]
        public bool IsFullScreen
        {
            get
            {
                return SettingsManager.Instance.IsFullScreen;
            }
            set
            {
                SettingsManager.Instance.IsFullScreen = value;
            }
        }

        // The slider's position, which is the square root of the setting's (linear-space) value
        [CreateProperty]
        public float MinimumLight
        {
            get
            {
                return Mathf.Sqrt(SettingsManager.Instance.MinimumLight);
            }
            set
            {
                SettingsManager.Instance.MinimumLight = value * value;
            }
        }

        [CreateProperty]
        public bool AmbientOcclusionEnabled
        {
            get
            {
                return SettingsManager.Instance.AmbientOcclusionEnabled;
            }
            set
            {
                SettingsManager.Instance.AmbientOcclusionEnabled = value;
            }
        }

        [CreateProperty]
        public bool BloomEnabled
        {
            get
            {
                return SettingsManager.Instance.BloomEnabled;
            }
            set
            {
                SettingsManager.Instance.BloomEnabled = value;
            }
        }

        [CreateProperty]
        public bool ColorCorrectionEnabled
        {
            get
            {
                return SettingsManager.Instance.ColorCorrectionEnabled;
            }
            set
            {
                SettingsManager.Instance.ColorCorrectionEnabled = value;
            }
        }

        [CreateProperty]
        public bool VignetteEnabled
        {
            get
            {
                return SettingsManager.Instance.VignetteEnabled;
            }
            set
            {
                SettingsManager.Instance.VignetteEnabled = value;
            }
        }

        // The index of the choice, in the order of MergedSaveChecksums (Ask, Keep, Regenerate)
        [CreateProperty]
        public int MergedSaveChecksum
        {
            get
            {
                return (int) SettingsManager.Instance.MergedSaveChecksum;
            }
            set
            {
                if (value >= 0)
                {
                    SettingsManager.Instance.MergedSaveChecksum = (MergedSaveChecksums) value;
                }
            }
        }

        [CreateProperty]
        public bool ClipPlatformSidesEnabled
        {
            get
            {
                return SettingsManager.Instance.ClipPlatformSidesEnabled;
            }
            set
            {
                SettingsManager.Instance.ClipPlatformSidesEnabled = value;
            }
        }

        // Shown as on while Object mode forces icons on
        [CreateProperty]
        public bool ObjectIconsEnabled
        {
            get
            {
                return SettingsManager.Instance.ObjectIconsForcedOn || SettingsManager.Instance.ObjectIconsEnabled;
            }
            set
            {
                if (!SettingsManager.Instance.ObjectIconsForcedOn)
                {
                    SettingsManager.Instance.ObjectIconsEnabled = value;
                }
            }
        }

        [CreateProperty]
        public bool ObjectIconsEditable
        {
            get
            {
                return !SettingsManager.Instance.ObjectIconsForcedOn;
            }
        }

        [CreateProperty]
        public bool SpritePreviewsEnabled
        {
            get
            {
                return SettingsManager.Instance.SpritePreviewsEnabled;
            }
            set
            {
                SettingsManager.Instance.SpritePreviewsEnabled = value;
            }
        }

        public void Dispose()
        {
            var settingsManager = SettingsManager.Instance;
            if (settingsManager)
            {
                settingsManager.OnSettingChanged -= Notify;
                settingsManager.OnObjectVisibilityChanged -= OnObjectVisibilityChanged;
            }
        }

        private void OnObjectVisibilityChanged()
        {
            Notify(nameof(ObjectIconsEnabled));
            Notify(nameof(ObjectIconsEditable));
        }
    }
}
