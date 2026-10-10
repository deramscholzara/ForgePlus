using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.Localization;
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
            SettingsManager.Instance.OnSettingChanged += OnSettingChanged;
            SettingsManager.Instance.OnObjectVisibilityChanged += OnObjectVisibilityChanged;
            SettingsManager.Instance.OnGeometryVisibilityChanged += OnGeometryVisibilityChanged;
            SoundsLoading.Instance.OnDataLoadCompleted += OnSoundsLoadCompleted;
        }

        // Geometry mode's visualization options. Show Everything is on while every other one is, and turning it on turns
        // them all on (turning it off does nothing, so it stays on).
        [CreateProperty]
        public bool ShowEverythingEnabled
        {
            get
            {
                return PointsEnabled && LinesEnabled && MediaEnabled;
            }
            set
            {
                if (value)
                {
                    PointsEnabled = true;
                    LinesEnabled = true;
                    MediaEnabled = true;
                }

                Notify(nameof(ShowEverythingEnabled));
            }
        }

        [CreateProperty]
        public bool PointsEnabled
        {
            get
            {
                return SettingsManager.Instance.PointsEnabled;
            }
            set
            {
                if (value != SettingsManager.Instance.PointsEnabled)
                {
                    SettingsManager.Instance.PointsEnabled = value;
                }
            }
        }

        [CreateProperty]
        public bool LinesEnabled
        {
            get
            {
                return SettingsManager.Instance.LinesEnabled;
            }
            set
            {
                if (value != SettingsManager.Instance.LinesEnabled)
                {
                    SettingsManager.Instance.LinesEnabled = value;
                }
            }
        }

        // Shown as on while Media mode forces media on
        [CreateProperty]
        public bool MediaEnabled
        {
            get
            {
                return SettingsManager.Instance.MediaIsShown;
            }
            set
            {
                if (!SettingsManager.Instance.MediaForcedOn && value != SettingsManager.Instance.MediaEnabled)
                {
                    SettingsManager.Instance.MediaEnabled = value;
                }
            }
        }

        [CreateProperty]
        public bool SimpleVisualsEnabled
        {
            get
            {
                return SettingsManager.Instance.SimpleVisualsEnabled;
            }
            set
            {
                if (value != SettingsManager.Instance.SimpleVisualsEnabled)
                {
                    SettingsManager.Instance.SimpleVisualsEnabled = value;
                }
            }
        }

        [CreateProperty]
        public bool ShowLightingEnabled
        {
            get
            {
                return SettingsManager.Instance.ShowLightingEnabled;
            }
            set
            {
                if (value != SettingsManager.Instance.ShowLightingEnabled)
                {
                    SettingsManager.Instance.ShowLightingEnabled = value;
                }
            }
        }

        [CreateProperty]
        public bool DiagnosticVisualsEnabled
        {
            get
            {
                return SettingsManager.Instance.DiagnosticVisualsEnabled;
            }
            set
            {
                if (value != SettingsManager.Instance.DiagnosticVisualsEnabled)
                {
                    SettingsManager.Instance.DiagnosticVisualsEnabled = value;
                }
            }
        }

        [CreateProperty]
        public bool MediaEditable
        {
            get
            {
                return !SettingsManager.Instance.MediaForcedOn;
            }
        }

        // The slider's position, in ninths of a point's size added to its click area
        [CreateProperty]
        public int PointClickAreaSteps
        {
            get
            {
                return SettingsManager.Instance.PointClickAreaSteps;
            }
            set
            {
                SettingsManager.Instance.PointClickAreaSteps = value;
            }
        }

        [CreateProperty]
        public bool PreventInvalidGeometryEnabled
        {
            get
            {
                return SettingsManager.Instance.PreventInvalidGeometryEnabled;
            }
            set
            {
                SettingsManager.Instance.PreventInvalidGeometryEnabled = value;
            }
        }

        // As a whole percentage of a point's size ("114%")
        [CreateProperty]
        public string PointClickAreaText
        {
            get
            {
                return Strings.Get(Strings.Menu, "SettingsTab.PointClickArea.Value", Mathf.RoundToInt(SettingsManager.Instance.PointClickAreaScale * 100f));
            }
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

        [CreateProperty]
        public int UndoSteps
        {
            get
            {
                return SettingsManager.Instance.UndoSteps;
            }
            set
            {
                SettingsManager.Instance.UndoSteps = value;
            }
        }

        [CreateProperty]
        public bool ImproperFractionsEnabled
        {
            get
            {
                return SettingsManager.Instance.ImproperFractionsEnabled;
            }
            set
            {
                SettingsManager.Instance.ImproperFractionsEnabled = value;
            }
        }

        [CreateProperty]
        public bool ShowInvalidSidesEnabled
        {
            get
            {
                return SettingsManager.Instance.ShowInvalidSidesEnabled;
            }
            set
            {
                SettingsManager.Instance.ShowInvalidSidesEnabled = value;
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

        // Shown as on while Sounds mode forces sounds' displays on
        [CreateProperty]
        public bool SoundDirectionEnabled
        {
            get
            {
                return SettingsManager.Instance.SoundDisplaysForcedOn || SettingsManager.Instance.SoundDirectionEnabled;
            }
            set
            {
                if (!SettingsManager.Instance.SoundDisplaysForcedOn)
                {
                    SettingsManager.Instance.SoundDirectionEnabled = value;
                }
            }
        }

        [CreateProperty]
        public bool SoundVolumeEnabled
        {
            get
            {
                return SettingsManager.Instance.SoundDisplaysForcedOn || SettingsManager.Instance.SoundVolumeEnabled;
            }
            set
            {
                if (!SettingsManager.Instance.SoundDisplaysForcedOn)
                {
                    SettingsManager.Instance.SoundVolumeEnabled = value;
                }
            }
        }

        [CreateProperty]
        public bool SoundDisplaysEditable
        {
            get
            {
                return !SettingsManager.Instance.SoundDisplaysForcedOn;
            }
        }

        [CreateProperty]
        public bool PlayLevelAudioEnabled
        {
            get
            {
                return SettingsManager.Instance.PlayLevelAudioEnabled;
            }
            set
            {
                SettingsManager.Instance.PlayLevelAudioEnabled = value;
            }
        }

        // Playing level audio needs a sounds file (Files tab) to play sounds from
        [CreateProperty]
        public bool PlayLevelAudioEditable
        {
            get
            {
                return SoundsLoading.Instance.IsLoaded;
            }
        }

        public void Dispose()
        {
            var settingsManager = SettingsManager.Instance;
            if (settingsManager)
            {
                settingsManager.OnSettingChanged -= OnSettingChanged;
                settingsManager.OnObjectVisibilityChanged -= OnObjectVisibilityChanged;
                settingsManager.OnGeometryVisibilityChanged -= OnGeometryVisibilityChanged;
            }

            SoundsLoading.Instance.OnDataLoadCompleted -= OnSoundsLoadCompleted;
        }

        private void OnSettingChanged(string setting)
        {
            Notify(setting);

            if (setting == nameof(PointClickAreaSteps))
            {
                Notify(nameof(PointClickAreaText));
            }
        }

        // Also when the mode changes (which may force media on)
        private void OnGeometryVisibilityChanged()
        {
            Notify(nameof(ShowEverythingEnabled));
            Notify(nameof(PointsEnabled));
            Notify(nameof(LinesEnabled));
            Notify(nameof(MediaEnabled));
            Notify(nameof(MediaEditable));
        }

        // Also when the mode changes (which may force icons, and sounds' displays, on)
        private void OnObjectVisibilityChanged()
        {
            Notify(nameof(ObjectIconsEnabled));
            Notify(nameof(ObjectIconsEditable));
            Notify(nameof(SoundDirectionEnabled));
            Notify(nameof(SoundVolumeEnabled));
            Notify(nameof(SoundDisplaysEditable));
        }

        private void OnSoundsLoadCompleted(bool isLoaded)
        {
            Notify(nameof(PlayLevelAudioEditable));
        }
    }
}
