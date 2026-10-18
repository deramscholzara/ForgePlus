using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
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
                return PolygonsEnabled && SidesEnabled && PointsEnabled && LinesEnabled && MediaEnabled && ObjectIconsEnabled;
            }
            set
            {
                if (value)
                {
                    PolygonsEnabled = true;
                    SidesEnabled = true;
                    PointsEnabled = true;
                    LinesEnabled = true;
                    MediaEnabled = true;
                    ObjectIconsEnabled = true;
                }

                Notify(nameof(ShowEverythingEnabled));
            }
        }

        [CreateProperty]
        public bool PolygonsEnabled
        {
            get
            {
                return SettingsManager.Instance.PolygonsEnabled;
            }
            set
            {
                if (value != SettingsManager.Instance.PolygonsEnabled)
                {
                    SettingsManager.Instance.PolygonsEnabled = value;
                }
            }
        }

        [CreateProperty]
        public bool SidesEnabled
        {
            get
            {
                return SettingsManager.Instance.SidesEnabled;
            }
            set
            {
                if (value != SettingsManager.Instance.SidesEnabled)
                {
                    SettingsManager.Instance.SidesEnabled = value;
                }
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

        // The Culling Simulation dropdown's choices, in the order of CullingSimulation.Modes (Off, Camera, Reference Point)
        [CreateProperty]
        public System.Collections.Generic.List<string> CullingSimulationModeChoices
        {
            get
            {
                if (cullingSimulationModeChoices == null)
                {
                    cullingSimulationModeChoices = new System.Collections.Generic.List<string>();
                    foreach (CullingSimulation.Modes mode in Enum.GetValues(typeof(CullingSimulation.Modes)))
                    {
                        cullingSimulationModeChoices.Add(Strings.Get(Strings.Common, $"ViewOptions.CullingSimulation.{mode}.Text"));
                    }
                }

                return cullingSimulationModeChoices;
            }
        }

        private System.Collections.Generic.List<string> cullingSimulationModeChoices;

        // As the chosen choice's text
        [CreateProperty]
        public string CullingSimulationMode
        {
            get
            {
                return CullingSimulationModeChoices[(int) SettingsManager.Instance.CullingSimulationMode];
            }
            set
            {
                var index = CullingSimulationModeChoices.IndexOf(value);
                if (index >= 0 && (CullingSimulation.Modes) index != SettingsManager.Instance.CullingSimulationMode)
                {
                    SettingsManager.Instance.CullingSimulationMode = (CullingSimulation.Modes) index;
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

        // In pixels
        [CreateProperty]
        public int PointSnapDistance
        {
            get
            {
                return SettingsManager.Instance.PointSnapDistance;
            }
            set
            {
                SettingsManager.Instance.PointSnapDistance = value;
            }
        }

        // As whole pixels ("5 px")
        [CreateProperty]
        public string PointSnapDistanceText
        {
            get
            {
                return Strings.Get(Strings.Menu, "SettingsTab.PointSnapDistance.Value", SettingsManager.Instance.PointSnapDistance);
            }
        }

        [CreateProperty]
        public bool GridEnabled
        {
            get
            {
                return SettingsManager.Instance.GridEnabled;
            }
            set
            {
                SettingsManager.Instance.GridEnabled = value;
            }
        }

        // Points and lines are only shown in Geometry mode
        [CreateProperty]
        public bool IsGeometryMode
        {
            get
            {
                return LevelManipulation.ModeManager.Instance.PrimaryMode == LevelManipulation.ModeManager.PrimaryModes.Geometry;
            }
        }

        [CreateProperty]
        public int OrthographicGridOpacityPercent
        {
            get
            {
                return SettingsManager.Instance.OrthographicGridOpacityPercent;
            }
            set
            {
                SettingsManager.Instance.OrthographicGridOpacityPercent = value;
            }
        }

        // The UI scale's step on its slider (0 for 100%, up to 5 for 350%)
        [CreateProperty]
        public int UIScaleStep
        {
            get
            {
                return (SettingsManager.Instance.UIScalePercent - SettingsManager.MinimumUIScalePercent) / SettingsManager.UIScaleStepPercent;
            }
            set
            {
                SettingsManager.Instance.UIScalePercent = SettingsManager.MinimumUIScalePercent + value * SettingsManager.UIScaleStepPercent;
            }
        }

        // "150%"
        [CreateProperty]
        public string UIScaleText
        {
            get
            {
                return Strings.Get(Strings.Menu, "SettingsTab.UIScale.Value", SettingsManager.Instance.UIScalePercent);
            }
        }

        // "40%"
        [CreateProperty]
        public string OrthographicGridOpacityText
        {
            get
            {
                return Strings.Get(Strings.Menu, "SettingsTab.OrthographicGridOpacity.Value", SettingsManager.Instance.OrthographicGridOpacityPercent);
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

        [CreateProperty]
        public bool SurfaceCacheGlobalIlluminationEnabled
        {
            get
            {
                return SettingsManager.Instance.SurfaceCacheGlobalIlluminationEnabled;
            }
            set
            {
                SettingsManager.Instance.SurfaceCacheGlobalIlluminationEnabled = value;
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

            if (setting == nameof(PointSnapDistance))
            {
                Notify(nameof(PointSnapDistanceText));
            }

            if (setting == nameof(OrthographicGridOpacityPercent))
            {
                Notify(nameof(OrthographicGridOpacityText));
            }

            if (setting == nameof(SettingsManager.UIScalePercent))
            {
                Notify(nameof(UIScaleStep));
                Notify(nameof(UIScaleText));
            }
        }

        // Also when the mode changes (which may force media on)
        private void OnGeometryVisibilityChanged()
        {
            Notify(nameof(IsGeometryMode));
            Notify(nameof(ShowEverythingEnabled));
            Notify(nameof(PolygonsEnabled));
            Notify(nameof(SidesEnabled));
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
            Notify(nameof(ShowEverythingEnabled));
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
