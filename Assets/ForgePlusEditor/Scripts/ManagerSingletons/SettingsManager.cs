using ForgePlus.History;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.Sound;
using ForgePlus.UI;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForgePlus.ApplicationGeneral
{
    // What Save Merged does with the loaded map file's checksum
    public enum MergedSaveChecksums
    {
        // Asks each time, in a dialog that can remember the choice
        Ask,
        Keep,
        Regenerate,
    }

    // Early, so it's ready before the settings UI binds to it
    [DefaultExecutionOrder(-100)]
    public class SettingsManager : SingletonMonoBehaviour<SettingsManager>
    {
        public VolumeProfile effectsDisabledVolumeProfile;
        public Camera mainCamera;

        private const string PlayerPrefsSettingsKey_FullScreen = "Settings_FullScreen";
        private const string PlayerPrefsSettingsKey_MinimumLight = "Settings_MinimumLight";
        private const string PlayerPrefsSettingsKey_AmbientOcclusion = "Settings_AmbientOcclusion";
        private const string PlayerPrefsSettingsKey_Bloom = "Settings_Bloom";
        private const string PlayerPrefsSettingsKey_ColorAdjustment = "Settings_ColorAdjustment";
        private const string PlayerPrefsSettingsKey_Vignette = "Settings_Vignette";
        private const string PlayerPrefsSettingsKey_ClipPlatformSides = "Settings_ClipPlatformSides";
        private const string PlayerPrefsSettingsKey_ShowInvalidSides = "Settings_ShowInvalidSides";
        private const string PlayerPrefsSettingsKey_MergedSaveChecksum = "Settings_MergedSaveChecksum";
        private const string PlayerPrefsSettingsKey_SoundDirection = "Settings_SoundDirection";
        private const string PlayerPrefsSettingsKey_SoundVolume = "Settings_SoundVolume";
        private const string PlayerPrefsSettingsKey_PlayLevelAudio = "Settings_PlayLevelAudio";
        private const string PlayerPrefsSettingsKey_ImproperFractions = "Settings_ImproperFractions";
        private const string PlayerPrefsSettingsKey_UndoSteps = "Settings_UndoSteps";
        private const string PlayerPrefsSettingsKey_PointClickAreaSteps = "Settings_PointClickAreaSteps";
        private const string PlayerPrefsSettingsKey_PreventInvalidGeometry = "Settings_PreventInvalidGeometry";

        // A point's click area grows from its drawn size (100%) to twice that (200%), in steps of a ninth (a pixel each)
        public const int MaximumPointClickAreaSteps = 9;

        private static readonly int minimumLightPropertyId = Shader.PropertyToID("_GlobalMinimumLight");

        // The name of the setting (its property) that changed, which SettingsViewModel's properties share
        public event Action<string> OnSettingChanged;

        private event Action OnObjectVisibilityChanged_Sender;
        public event Action OnObjectVisibilityChanged
        {
            add
            {
                OnObjectVisibilityChanged_Sender += value;
                value.Invoke();
            }
            remove
            {
                OnObjectVisibilityChanged_Sender -= value;
            }
        }

        // Which geometry the level shows (and can be clicked), as the visualization options set it; also when the mode
        // changes (which may force some on)
        private event Action OnGeometryVisibilityChanged_Sender;
        public event Action OnGeometryVisibilityChanged
        {
            add
            {
                OnGeometryVisibilityChanged_Sender += value;
                value.Invoke();
            }
            remove
            {
                OnGeometryVisibilityChanged_Sender -= value;
            }
        }

        // These last for the session only (they aren't saved)
        private bool objectIconsEnabled = true;
        private bool spritePreviewsEnabled = true;
        private bool pointsEnabled = true;
        private bool mediaEnabled = true;
        private bool diagnosticVisualsEnabled = true;
        private bool simpleVisualsEnabled = false;
        private bool showLightingEnabled = true;

        public bool IsFullScreen
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_FullScreen, 1) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_FullScreen, value ? 1 : 0);

                if (Screen.fullScreen != value)
                {
                    Screen.fullScreen = value;
                }

                OnSettingChanged?.Invoke(nameof(IsFullScreen));
            }
        }

        public float MinimumLight
        {
            get
            {
                return PlayerPrefs.GetFloat(PlayerPrefsSettingsKey_MinimumLight, 0.025f);
            }
            set
            {
                PlayerPrefs.SetFloat(PlayerPrefsSettingsKey_MinimumLight, value);

                ApplyMinimumLight();

                OnSettingChanged?.Invoke(nameof(MinimumLight));
            }
        }

        public bool AmbientOcclusionEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_AmbientOcclusion, 1) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_AmbientOcclusion, value ? 1 : 0);

                if (effectsDisabledVolumeProfile.TryGet<ScreenSpaceAmbientOcclusionVolumeOverride>(out var profileComponent))
                {
                    profileComponent.active = !value;
                    mainCamera.UpdateVolumeStack();
                }

                OnSettingChanged?.Invoke(nameof(AmbientOcclusionEnabled));
            }
        }

        public bool BloomEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_Bloom, 1) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_Bloom, value ? 1 : 0);

                ApplyBloomAndColorAdjustment();

                OnSettingChanged?.Invoke(nameof(BloomEnabled));
            }
        }

        public bool ColorCorrectionEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_ColorAdjustment, 1) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_ColorAdjustment, value ? 1 : 0);

                ApplyBloomAndColorAdjustment();

                OnSettingChanged?.Invoke(nameof(ColorCorrectionEnabled));
            }
        }

        public bool VignetteEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_Vignette, 1) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_Vignette, value ? 1 : 0);

                if (effectsDisabledVolumeProfile.TryGet<Vignette>(out var profileComponent))
                {
                    profileComponent.active = !value;
                    mainCamera.UpdateVolumeStack();
                }

                OnSettingChanged?.Invoke(nameof(VignetteEnabled));
            }
        }

        public bool ClipPlatformSidesEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_ClipPlatformSides, 1) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_ClipPlatformSides, value ? 1 : 0);

                PlatformSideClipping.ClippingEnabled = value;

                OnSettingChanged?.Invoke(nameof(ClipPlatformSidesEnabled));
            }
        }

        // Placeholder sides (with no side data) can't be clicked once hidden, so hiding them deselects any
        public bool ShowInvalidSidesEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_ShowInvalidSides, 1) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_ShowInvalidSides, value ? 1 : 0);

                if (!value && SelectionManager.Instance)
                {
                    foreach (var selection in SelectionManager.Instance.Selection)
                    {
                        if (selection is LevelEntity_Side side && side.NativeObject == null)
                        {
                            SelectionManager.Instance.DeselectAll();
                            break;
                        }
                    }
                }

                LevelEntity_Side.PlaceholdersAreVisible = value;

                OnSettingChanged?.Invoke(nameof(ShowInvalidSidesEnabled));
            }
        }

        // World distances as 1024ths alone ("6153/1024") rather than with whole world units ("6 and 9/1024"). Fields show
        // the change themselves, and the inspectors' texts are refreshed.
        public bool ImproperFractionsEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_ImproperFractions, 0) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_ImproperFractions, value ? 1 : 0);

                if (WorldDistances.UsesImproperFractions != value)
                {
                    WorldDistances.UsesImproperFractions = value;
                    InspectorPanel.Instance.RefreshAllInspectors();
                }

                OnSettingChanged?.Invoke(nameof(ImproperFractionsEnabled));
            }
        }

        // How many changes can be undone (each kept change takes memory, more for bigger changes)
        public int UndoSteps
        {
            get
            {
                return Math.Clamp(PlayerPrefs.GetInt(PlayerPrefsSettingsKey_UndoSteps, UndoHistory.DefaultCapacity), UndoHistory.MinimumCapacity, UndoHistory.MaximumCapacity);
            }
            set
            {
                var steps = Math.Clamp(value, UndoHistory.MinimumCapacity, UndoHistory.MaximumCapacity);
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_UndoSteps, steps);

                UndoHistory.Capacity = steps;

                OnSettingChanged?.Invoke(nameof(UndoSteps));
            }
        }

        // How many ninths of a point's drawn size its click area adds (PointHandles), from 0 (100%) to 9 (200%)
        public int PointClickAreaSteps
        {
            get
            {
                return Math.Clamp(PlayerPrefs.GetInt(PlayerPrefsSettingsKey_PointClickAreaSteps, 0), 0, MaximumPointClickAreaSteps);
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_PointClickAreaSteps, Math.Clamp(value, 0, MaximumPointClickAreaSteps));

                OnSettingChanged?.Invoke(nameof(PointClickAreaSteps));
            }
        }

        // Moving a point stops short of where it would make a polygon invalid (PointEditing), unless Allow Invalid Move
        // (shift) is held while dragging its handle
        public bool PreventInvalidGeometryEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_PreventInvalidGeometry, 1) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_PreventInvalidGeometry, value ? 1 : 0);

                OnSettingChanged?.Invoke(nameof(PreventInvalidGeometryEnabled));
            }
        }

        // The click area's size, as a multiple of a point's drawn size
        public float PointClickAreaScale
        {
            get
            {
                return 1f + (float) PointClickAreaSteps / MaximumPointClickAreaSteps;
            }
        }

        public MergedSaveChecksums MergedSaveChecksum
        {
            get
            {
                var value = PlayerPrefs.GetInt(PlayerPrefsSettingsKey_MergedSaveChecksum, (int) MergedSaveChecksums.Ask);

                return Enum.IsDefined(typeof(MergedSaveChecksums), value) ? (MergedSaveChecksums) value : MergedSaveChecksums.Ask;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_MergedSaveChecksum, (int) value);

                OnSettingChanged?.Invoke(nameof(MergedSaveChecksum));
            }
        }

        public bool ObjectIconsEnabled
        {
            get
            {
                return objectIconsEnabled;
            }
            set
            {
                objectIconsEnabled = value;

                ApplyObjectVisibility();

                OnSettingChanged?.Invoke(nameof(ObjectIconsEnabled));
            }
        }

        // Object mode always shows icons, since they're how objects are selected there
        public bool ObjectIconsForcedOn
        {
            get
            {
                return ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Objects;
            }
        }

        public bool SpritePreviewsEnabled
        {
            get
            {
                return spritePreviewsEnabled;
            }
            set
            {
                spritePreviewsEnabled = value;

                ApplyObjectVisibility();

                OnSettingChanged?.Invoke(nameof(SpritePreviewsEnabled));
            }
        }

        // Shown in every mode when on; always shown in Sounds mode
        public bool SoundDirectionEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_SoundDirection, 0) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_SoundDirection, value ? 1 : 0);

                OnSettingChanged?.Invoke(nameof(SoundDirectionEnabled));
            }
        }

        // Shown in every mode when on; always shown in Sounds mode
        public bool SoundVolumeEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_SoundVolume, 0) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_SoundVolume, value ? 1 : 0);

                OnSettingChanged?.Invoke(nameof(SoundVolumeEnabled));
            }
        }

        // Plays what the game would play at the camera (LevelSoundPlayback)
        public bool PlayLevelAudioEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_PlayLevelAudio, 0) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_PlayLevelAudio, value ? 1 : 0);

                if (LevelSoundPlayback.Instance)
                {
                    LevelSoundPlayback.Instance.IsListening = value;
                }

                OnSettingChanged?.Invoke(nameof(PlayLevelAudioEnabled));
            }
        }

        // Sounds mode always shows sounds' directions and volumes
        public bool SoundDisplaysForcedOn
        {
            get
            {
                return ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Sounds;
            }
        }

        // Points (polygons' corners) are only shown in Geometry mode
        public bool PointsEnabled
        {
            get
            {
                return pointsEnabled;
            }
            set
            {
                pointsEnabled = value;

                ApplyGeometryVisibility();

                OnSettingChanged?.Invoke(nameof(PointsEnabled));
            }
        }

        public bool PointsAreShown
        {
            get
            {
                return pointsEnabled && ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry;
            }
        }

        // Hidden media surfaces can't be clicked either
        public bool MediaEnabled
        {
            get
            {
                return mediaEnabled;
            }
            set
            {
                mediaEnabled = value;

                ApplyGeometryVisibility();

                OnSettingChanged?.Invoke(nameof(MediaEnabled));
            }
        }

        // Every surface is drawn with the grid texture instead of its own (SimpleVisuals), in every mode
        public bool SimpleVisualsEnabled
        {
            get
            {
                return simpleVisualsEnabled;
            }
            set
            {
                simpleVisualsEnabled = value;

                SimpleVisuals.SetEnabled(value, SurfaceBatchingManager.Instance.SurfaceMaterialsInUse);
                ApplyBloomAndColorAdjustment();

                OnSettingChanged?.Invoke(nameof(SimpleVisualsEnabled));
            }
        }

        // Surfaces (textured, or the grid of Simple Visuals) are lit by their lights; otherwise they're drawn at full
        // brightness. Diagnostic overlays are never lit.
        public bool ShowLightingEnabled
        {
            get
            {
                return showLightingEnabled;
            }
            set
            {
                showLightingEnabled = value;

                ApplyMinimumLight();

                OnSettingChanged?.Invoke(nameof(ShowLightingEnabled));
            }
        }

        // Each is drawn while its setting is on, except while Simple Visuals is (whose grid they'd only blur and tint). The
        // effects-disabled profile's override of each turns it off.
        private void ApplyBloomAndColorAdjustment()
        {
            if (effectsDisabledVolumeProfile.TryGet<Bloom>(out var bloom))
            {
                bloom.active = !(BloomEnabled && !simpleVisualsEnabled);
            }

            if (effectsDisabledVolumeProfile.TryGet<SplitToning>(out var colorAdjustment))
            {
                colorAdjustment.active = !(ColorCorrectionEnabled && !simpleVisualsEnabled);
            }

            // The camera takes it up itself once the render pipeline has started (which it may not have, as ForgePlus starts)
            if (VolumeManager.instance.isInitialized)
            {
                mainCamera.UpdateVolumeStack();
            }
        }

        // Surfaces are lit from this up to full brightness (the shaders' lerp from it), so full brightness everywhere
        // while lighting isn't shown
        private void ApplyMinimumLight()
        {
            Shader.SetGlobalFloat(minimumLightPropertyId, showLightingEnabled ? MinimumLight : 1f);
        }

        // Problem geometry is marked in the level view (DiagnosticVisuals), in every mode
        public bool DiagnosticVisualsEnabled
        {
            get
            {
                return diagnosticVisualsEnabled;
            }
            set
            {
                diagnosticVisualsEnabled = value;

                ApplyGeometryVisibility();

                OnSettingChanged?.Invoke(nameof(DiagnosticVisualsEnabled));
            }
        }

        // Media mode always shows media, since it's what the mode edits
        public bool MediaForcedOn
        {
            get
            {
                return ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Media;
            }
        }

        public bool MediaIsShown
        {
            get
            {
                return mediaEnabled || MediaForcedOn;
            }
        }

        private void ApplyObjectVisibility()
        {
            LevelEntity_MapObject.SoundSourceIconsAreVisible = SoundDisplaysForcedOn;
            LevelEntity_MapObject.IconsAreVisible = objectIconsEnabled || ObjectIconsForcedOn;
            LevelEntity_MapObject.SpritePreviewsAreVisible = spritePreviewsEnabled;

            OnObjectVisibilityChanged_Sender?.Invoke();
        }

        private void ApplyGeometryVisibility()
        {
            LevelEntity_Media.SurfacesAreVisible = MediaIsShown;
            DiagnosticVisuals.Refresh();

            OnGeometryVisibilityChanged_Sender?.Invoke();
        }

        private void OnPrimaryModeChanged(ModeManager.PrimaryModes primaryMode)
        {
            ApplyObjectVisibility();
            ApplyGeometryVisibility();
        }

        private void Start()
        {
            IsFullScreen = IsFullScreen;
            MinimumLight = MinimumLight;
            ClipPlatformSidesEnabled = ClipPlatformSidesEnabled;
            ShowInvalidSidesEnabled = ShowInvalidSidesEnabled;
            ImproperFractionsEnabled = ImproperFractionsEnabled;
            UndoSteps = UndoSteps;

            ModeManager.Instance.OnPrimaryModeChanged += OnPrimaryModeChanged;

            // The profile keeps what it was last set to (such as off, while Simple Visuals was on when ForgePlus closed)
            ApplyBloomAndColorAdjustment();
        }
    }
}
