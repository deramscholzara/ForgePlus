using ForgePlus.History;
using ForgePlus.DataFileIO;
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
        private const string PlayerPrefsSettingsKey_SurfaceCacheGlobalIllumination = "Settings_SurfaceCacheGlobalIllumination";
        private const string PlayerPrefsSettingsKey_SurfaceLightMultiplier = "Settings_SurfaceLightMultiplier";
        private const string PlayerPrefsSettingsKey_ClipPlatformSides = "Settings_ClipPlatformSides";
        private const string PlayerPrefsSettingsKey_ShowInvalidSides = "Settings_ShowInvalidSides";
        private const string PlayerPrefsSettingsKey_MergedSaveChecksum = "Settings_MergedSaveChecksum";
        private const string PlayerPrefsSettingsKey_SoundDirection = "Settings_SoundDirection";
        private const string PlayerPrefsSettingsKey_SoundVolume = "Settings_SoundVolume";
        private const string PlayerPrefsSettingsKey_PlayLevelAudio = "Settings_PlayLevelAudio";
        private const string PlayerPrefsSettingsKey_ImproperFractions = "Settings_ImproperFractions";
        private const string PlayerPrefsSettingsKey_UndoSteps = "Settings_UndoSteps";
        private const string PlayerPrefsSettingsKey_PointClickAreaSteps = "Settings_PointClickAreaSteps";
        private const string PlayerPrefsSettingsKey_PointSnapDistance = "Settings_PointSnapDistance";
        private const string PlayerPrefsSettingsKey_MediaFog = "Settings_MediaFog";

        // Followed by the media type's number
        private const string PlayerPrefsSettingsKeyPrefix_MediaFogColor = "Settings_MediaFogColor_";
        private const string PlayerPrefsSettingsKeyPrefix_MediaFogDensity = "Settings_MediaFogDensity_";

        // Media fog's density per world unit, in thousandths (so the slider's steps are exact), from none to one
        public const int MaximumMediaFogDensityThousandths = 1000;
        public static readonly int DefaultMediaFogDensityThousandths = Mathf.RoundToInt(MediaFog.DefaultDensityPerWorldUnit * 1000f);

        // No longer a setting (invalid moves are always prevented, unless Allow Invalid Move is held), so its saved value
        // is removed
        private const string RemovedPlayerPrefsSettingsKey_PreventInvalidGeometry = "Settings_PreventInvalidGeometry";
        private const string PlayerPrefsSettingsKey_OrthographicGridOpacity = "Settings_OrthographicGridOpacity";

        public const int DefaultOrthographicGridOpacityPercent = 40;

        // The light surfaces give Surface Cache Global Illumination to bounce, from none to ten times their own, in tenths
        public const int MaximumSurfaceLightMultiplierTenths = 100;
        public const int DefaultSurfaceLightMultiplierTenths = 10;

        // The UI's scale, from 100% to 350% (the range of Windows' display scaling) in steps of 50%
        private const string PlayerPrefsSettingsKey_UIScale = "Settings_UIScale";
        public const int MinimumUIScalePercent = 100;
        public const int MaximumUIScalePercent = 350;
        public const int UIScaleStepPercent = 50;

        // A point's click area grows from its drawn size (100%) to twice that (200%), in steps of a ninth (a pixel each)
        public const int MaximumPointClickAreaSteps = 9;

        // How near (in pixels on screen) a line drawn from a point has to come to another point's handle to end there
        public const int MinimumPointSnapDistance = 1;
        public const int MaximumPointSnapDistance = 25;
        public const int DefaultPointSnapDistance = 5;

        private static readonly int minimumLightPropertyId = Shader.PropertyToID("_GlobalMinimumLight");
        private static readonly int surfaceLightMultiplierPropertyId = Shader.PropertyToID("_SurfaceLightMultiplier");

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
        private bool polygonsEnabled = true;
        private bool sidesEnabled = true;
        private bool pointsEnabled = true;
        private bool linesEnabled = true;
        private bool mediaEnabled = true;
        private bool diagnosticVisualsEnabled = true;
        private bool simpleVisualsEnabled = false;
        private bool showLightingEnabled = true;
        private bool gridEnabled = true;

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

        // Off until it's turned on (it's costly, and its indirect lighting changes how levels look)
        public bool SurfaceCacheGlobalIlluminationEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_SurfaceCacheGlobalIllumination, 0) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_SurfaceCacheGlobalIllumination, value ? 1 : 0);

                ApplySurfaceCacheGlobalIllumination();

                OnSettingChanged?.Invoke(nameof(SurfaceCacheGlobalIlluminationEnabled));
            }
        }

        // Scales the light surfaces give Surface Cache Global Illumination to bounce (the face shaders' Meta pass emission),
        // in tenths, so the slider's steps are exact
        public int SurfaceLightMultiplierTenths
        {
            get
            {
                return Math.Clamp(PlayerPrefs.GetInt(PlayerPrefsSettingsKey_SurfaceLightMultiplier, DefaultSurfaceLightMultiplierTenths), 0, MaximumSurfaceLightMultiplierTenths);
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_SurfaceLightMultiplier, Math.Clamp(value, 0, MaximumSurfaceLightMultiplierTenths));

                ApplySurfaceLightMultiplier();

                OnSettingChanged?.Invoke(nameof(SurfaceLightMultiplierTenths));
            }
        }

        public float SurfaceLightMultiplier
        {
            get
            {
                return SurfaceLightMultiplierTenths / 10f;
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

        // In pixels on screen (LineDrawing)
        public int PointSnapDistance
        {
            get
            {
                return Math.Clamp(PlayerPrefs.GetInt(PlayerPrefsSettingsKey_PointSnapDistance, DefaultPointSnapDistance), MinimumPointSnapDistance, MaximumPointSnapDistance);
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_PointSnapDistance, Math.Clamp(value, MinimumPointSnapDistance, MaximumPointSnapDistance));

                OnSettingChanged?.Invoke(nameof(PointSnapDistance));
            }
        }

        // The click area's size, as a multiple of a point's drawn size
        // The grid under the level in the orthographic view (OrthographicGrid)
        public bool GridEnabled
        {
            get
            {
                return gridEnabled;
            }
            set
            {
                gridEnabled = value;

                OnSettingChanged?.Invoke(nameof(GridEnabled));
            }
        }

        // How see-through the grid under the level is in the orthographic view (OrthographicGrid), as a whole percentage
        public int OrthographicGridOpacityPercent
        {
            get
            {
                return Math.Clamp(PlayerPrefs.GetInt(PlayerPrefsSettingsKey_OrthographicGridOpacity, DefaultOrthographicGridOpacityPercent), 0, 100);
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_OrthographicGridOpacity, Math.Clamp(value, 0, 100));

                OnSettingChanged?.Invoke(nameof(OrthographicGridOpacityPercent));
            }
        }

        // As a whole percentage, on one of its steps (ForgePlusUI applies it)
        public int UIScalePercent
        {
            get
            {
                return SnappedUIScalePercent(PlayerPrefs.GetInt(PlayerPrefsSettingsKey_UIScale, MinimumUIScalePercent));
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_UIScale, SnappedUIScalePercent(value));

                OnSettingChanged?.Invoke(nameof(UIScalePercent));
            }
        }

        private static int SnappedUIScalePercent(int percent)
        {
            var steps = (int) Math.Round((float) (percent - MinimumUIScalePercent) / UIScaleStepPercent);

            return Math.Clamp(MinimumUIScalePercent + steps * UIScaleStepPercent, MinimumUIScalePercent, MaximumUIScalePercent);
        }

        public float OrthographicGridOpacity
        {
            get
            {
                return OrthographicGridOpacityPercent / 100f;
            }
        }

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

        // Floors and ceilings (in every mode): hidden ones aren't drawn, and clicks pass through them
        public bool PolygonsEnabled
        {
            get
            {
                return polygonsEnabled;
            }
            set
            {
                polygonsEnabled = value;

                ApplyGeometryVisibility();

                OnSettingChanged?.Invoke(nameof(PolygonsEnabled));
            }
        }

        // Sides (walls, and what fills openings, in every mode): hidden ones aren't drawn, and clicks pass through them
        public bool SidesEnabled
        {
            get
            {
                return sidesEnabled;
            }
            set
            {
                sidesEnabled = value;

                ApplyGeometryVisibility();

                OnSettingChanged?.Invoke(nameof(SidesEnabled));
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

        // Lines (drawn along polygons' edges, at their floors and ceilings) are only shown in Geometry mode. Points' poles
        // are the Points option's.
        public bool LinesEnabled
        {
            get
            {
                return linesEnabled;
            }
            set
            {
                linesEnabled = value;

                ApplyGeometryVisibility();

                OnSettingChanged?.Invoke(nameof(LinesEnabled));
            }
        }

        public bool LinesAreShown
        {
            get
            {
                return linesEnabled && ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry;
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

                // Surface Cache Global Illumination is off while it's on, so it's turned off before the shaders change,
                // and back on (if it's set to be) after they're back
                if (value)
                {
                    ApplySurfaceCacheGlobalIllumination();
                    SurfaceMaterials.SetSimpleVisualsEnabled(true);
                }
                else
                {
                    SurfaceMaterials.SetSimpleVisualsEnabled(false);
                    ApplySurfaceCacheGlobalIllumination();
                }

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

        // The effects-disabled profile's override turns it off (its Enabled is off, which also stops its per-frame work).
        // It's also off while Simple Visuals is on, keeping its setting for when Simple Visuals is turned off.
        private void ApplySurfaceCacheGlobalIllumination()
        {
            var isActive = SurfaceCacheGlobalIlluminationEnabled && !simpleVisualsEnabled;

            // Before it's on, so it takes surfaces in a few at a time
            SurfaceCacheAdmission.SetEnabled(isActive);

            if (effectsDisabledVolumeProfile.TryGet<SurfaceCacheGIVolumeOverride>(out var profileComponent))
            {
                profileComponent.active = !isActive;
            }

            // The camera takes it up itself once the render pipeline has started (which it may not have, as ForgePlus starts)
            if (VolumeManager.instance.isInitialized)
            {
                mainCamera.UpdateVolumeStack();
            }
        }

        // Surface Cache Global Illumination only takes it up as each material's Meta pass is rendered again
        private void ApplySurfaceLightMultiplier()
        {
            Shader.SetGlobalFloat(surfaceLightMultiplierPropertyId, SurfaceLightMultiplier);
            SurfaceMaterials.RefreshSurfaceCache();
        }

        // Surfaces are lit from this up to full brightness (the shaders' lerp from it), so full brightness everywhere
        // while lighting isn't shown
        private void ApplyMinimumLight()
        {
            Shader.SetGlobalFloat(minimumLightPropertyId, showLightingEnabled ? MinimumLight : 1f);
        }

        // Fog under media (MediaFog), drawn while media is shown
        public bool MediaFogEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(PlayerPrefsSettingsKey_MediaFog, 1) != 0;
            }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsSettingsKey_MediaFog, value ? 1 : 0);

                MediaFog.IsEnabled = value;

                OnSettingChanged?.Invoke(nameof(MediaFogEnabled));
            }
        }

        // What OnSettingChanged gives as a media type's fog color or density changes
        public static string MediaFogSettingName(short mediaType)
        {
            return $"MediaFog.{mediaType}";
        }

        // As Aleph One tints the view under the type's media, until it's changed
        public Color GetMediaFogColor(short mediaType)
        {
            var saved = PlayerPrefs.GetString(PlayerPrefsSettingsKeyPrefix_MediaFogColor + mediaType, string.Empty);

            return ColorUtility.TryParseHtmlString(saved, out var color) ? color : MediaFog.DefaultColors[mediaType];
        }

        public void SetMediaFogColor(short mediaType, Color color)
        {
            PlayerPrefs.SetString(PlayerPrefsSettingsKeyPrefix_MediaFogColor + mediaType, "#" + ColorUtility.ToHtmlStringRGB(color));

            ApplyMediaFogAppearance(mediaType);

            OnSettingChanged?.Invoke(MediaFogSettingName(mediaType));
        }

        public int GetMediaFogDensityThousandths(short mediaType)
        {
            return Math.Clamp(PlayerPrefs.GetInt(PlayerPrefsSettingsKeyPrefix_MediaFogDensity + mediaType, DefaultMediaFogDensityThousandths), 0, MaximumMediaFogDensityThousandths);
        }

        public void SetMediaFogDensityThousandths(short mediaType, int thousandths)
        {
            PlayerPrefs.SetInt(PlayerPrefsSettingsKeyPrefix_MediaFogDensity + mediaType, Math.Clamp(thousandths, 0, MaximumMediaFogDensityThousandths));

            ApplyMediaFogAppearance(mediaType);

            OnSettingChanged?.Invoke(MediaFogSettingName(mediaType));
        }

        private void ApplyMediaFogAppearance(short mediaType)
        {
            MediaFog.SetAppearance(mediaType, GetMediaFogColor(mediaType), GetMediaFogDensityThousandths(mediaType) / 1000f);
        }

        // Shows only what Aleph One would draw from the camera, or from a reference point (CullingSimulation), in every
        // mode. Off as ForgePlus starts (it isn't saved).
        public CullingSimulation.Modes CullingSimulationMode
        {
            get
            {
                return CullingSimulation.Mode;
            }
            set
            {
                CullingSimulation.Mode = value;

                OnSettingChanged?.Invoke(nameof(CullingSimulationMode));
            }
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

        // The settings kept between sessions (the Settings tab's, the display options', and the Save Merged dialog's
        // remembered choice) back to how they start, and every plugin back to how its Plugin.xml says to start. The files'
        // paths are kept, and the options kept only for the session are left as they are.
        public void ResetToDefaults()
        {
            foreach (var key in new[]
            {
                PlayerPrefsSettingsKey_FullScreen,
                PlayerPrefsSettingsKey_MinimumLight,
                PlayerPrefsSettingsKey_AmbientOcclusion,
                PlayerPrefsSettingsKey_Bloom,
                PlayerPrefsSettingsKey_ColorAdjustment,
                PlayerPrefsSettingsKey_Vignette,
                PlayerPrefsSettingsKey_SurfaceCacheGlobalIllumination,
                PlayerPrefsSettingsKey_SurfaceLightMultiplier,
                PlayerPrefsSettingsKey_ClipPlatformSides,
                PlayerPrefsSettingsKey_ShowInvalidSides,
                PlayerPrefsSettingsKey_MergedSaveChecksum,
                PlayerPrefsSettingsKey_SoundDirection,
                PlayerPrefsSettingsKey_SoundVolume,
                PlayerPrefsSettingsKey_PlayLevelAudio,
                PlayerPrefsSettingsKey_ImproperFractions,
                PlayerPrefsSettingsKey_UndoSteps,
                PlayerPrefsSettingsKey_PointClickAreaSteps,
                PlayerPrefsSettingsKey_PointSnapDistance,
                PlayerPrefsSettingsKey_OrthographicGridOpacity,
                PlayerPrefsSettingsKey_UIScale,
                PlayerPrefsSettingsKey_MediaFog,
            })
            {
                PlayerPrefs.DeleteKey(key);
            }

            for (short mediaType = 0; mediaType < AlephOne.media.NUMBER_OF_MEDIA_TYPES; mediaType++)
            {
                PlayerPrefs.DeleteKey(PlayerPrefsSettingsKeyPrefix_MediaFogColor + mediaType);
                PlayerPrefs.DeleteKey(PlayerPrefsSettingsKeyPrefix_MediaFogDensity + mediaType);

                ApplyMediaFogAppearance(mediaType);
                OnSettingChanged?.Invoke(MediaFogSettingName(mediaType));
            }

            // Each set to its default (what it reads without its key), which applies it as changing it would
            IsFullScreen = IsFullScreen;
            MinimumLight = MinimumLight;
            AmbientOcclusionEnabled = AmbientOcclusionEnabled;
            BloomEnabled = BloomEnabled;
            ColorCorrectionEnabled = ColorCorrectionEnabled;
            VignetteEnabled = VignetteEnabled;
            SurfaceCacheGlobalIlluminationEnabled = SurfaceCacheGlobalIlluminationEnabled;
            SurfaceLightMultiplierTenths = SurfaceLightMultiplierTenths;
            ClipPlatformSidesEnabled = ClipPlatformSidesEnabled;
            ShowInvalidSidesEnabled = ShowInvalidSidesEnabled;
            MergedSaveChecksum = MergedSaveChecksum;
            SoundDirectionEnabled = SoundDirectionEnabled;
            SoundVolumeEnabled = SoundVolumeEnabled;
            PlayLevelAudioEnabled = PlayLevelAudioEnabled;
            ImproperFractionsEnabled = ImproperFractionsEnabled;
            UndoSteps = UndoSteps;
            PointClickAreaSteps = PointClickAreaSteps;
            PointSnapDistance = PointSnapDistance;
            OrthographicGridOpacityPercent = OrthographicGridOpacityPercent;
            UIScalePercent = UIScalePercent;
            MediaFogEnabled = MediaFogEnabled;

            MmlLoading.Instance.ResetPluginStates();

            PlayerPrefs.Save();
        }

        private void ApplyGeometryVisibility()
        {
            LevelEntity_Media.SurfacesAreVisible = MediaIsShown;
            MediaFog.MediaIsShown = MediaIsShown;
            RuntimeSurfaceGeometry.SetKindVisibility(polygonsEnabled, sidesEnabled);
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
            PlayerPrefs.DeleteKey(RemovedPlayerPrefsSettingsKey_PreventInvalidGeometry);

            IsFullScreen = IsFullScreen;
            MinimumLight = MinimumLight;
            ClipPlatformSidesEnabled = ClipPlatformSidesEnabled;
            ShowInvalidSidesEnabled = ShowInvalidSidesEnabled;
            ImproperFractionsEnabled = ImproperFractionsEnabled;
            UndoSteps = UndoSteps;
            MediaFogEnabled = MediaFogEnabled;

            for (short mediaType = 0; mediaType < AlephOne.media.NUMBER_OF_MEDIA_TYPES; mediaType++)
            {
                ApplyMediaFogAppearance(mediaType);
            }

            ModeManager.Instance.OnPrimaryModeChanged += OnPrimaryModeChanged;

            // The profile keeps what it was last set to (such as off, while Simple Visuals was on when ForgePlus closed)
            ApplyBloomAndColorAdjustment();

            // Its saved setting (off by default) rather than what the profile was last left at
            ApplySurfaceCacheGlobalIllumination();
            ApplySurfaceLightMultiplier();
        }
    }
}
