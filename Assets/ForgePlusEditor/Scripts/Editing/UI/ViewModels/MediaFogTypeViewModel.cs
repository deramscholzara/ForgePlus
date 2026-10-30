using ForgePlus.ApplicationGeneral;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // One media type's fog settings (SettingsManager) for the Settings tab: its color and its density
    public class MediaFogTypeViewModel : BindableObject, IDisposable
    {
        private readonly short mediaType;
        private readonly string settingName;

        public MediaFogTypeViewModel(short mediaType)
        {
            this.mediaType = mediaType;
            settingName = SettingsManager.MediaFogSettingName(mediaType);

            SettingsManager.Instance.OnSettingChanged += OnSettingChanged;
        }

        // "Color (water)"
        [CreateProperty]
        public string ColorLabel
        {
            get
            {
                return Strings.Get(Strings.Menu, "SettingsTab.MediaFogColor.Control.Label", AlephOneNames.MediaType(mediaType));
            }
        }

        // "Density (water)"
        [CreateProperty]
        public string DensityLabel
        {
            get
            {
                return Strings.Get(Strings.Menu, "SettingsTab.MediaFogDensity.Control.Label", AlephOneNames.MediaType(mediaType));
            }
        }

        // As hexadecimal ("#0000FF"); typing one that isn't a color leaves the color as it was (and shows it again)
        [CreateProperty]
        public string ColorText
        {
            get
            {
                return "#" + ColorUtility.ToHtmlStringRGB(SettingsManager.Instance.GetMediaFogColor(mediaType));
            }
            set
            {
                var text = value?.Trim() ?? string.Empty;
                if (!text.StartsWith("#"))
                {
                    text = "#" + text;
                }

                if (text.Length == 7 && ColorUtility.TryParseHtmlString(text, out var color))
                {
                    SettingsManager.Instance.SetMediaFogColor(mediaType, color);
                }
                else
                {
                    Notify(nameof(ColorText));
                }
            }
        }

        [CreateProperty]
        public StyleColor SwatchColor
        {
            get
            {
                return new StyleColor(SettingsManager.Instance.GetMediaFogColor(mediaType));
            }
        }

        // The slider's position, in thousandths per world unit
        [CreateProperty]
        public int DensityThousandths
        {
            get
            {
                return SettingsManager.Instance.GetMediaFogDensityThousandths(mediaType);
            }
            set
            {
                if (value != SettingsManager.Instance.GetMediaFogDensityThousandths(mediaType))
                {
                    SettingsManager.Instance.SetMediaFogDensityThousandths(mediaType, value);
                }
            }
        }

        // "0.125"
        [CreateProperty]
        public string DensityText
        {
            get
            {
                return Strings.Get(Strings.Menu, "SettingsTab.MediaFogDensity.Value", SettingsManager.Instance.GetMediaFogDensityThousandths(mediaType) / 1000f);
            }
        }

        public void Dispose()
        {
            var settingsManager = SettingsManager.Instance;
            if (settingsManager)
            {
                settingsManager.OnSettingChanged -= OnSettingChanged;
            }
        }

        private void OnSettingChanged(string setting)
        {
            if (setting == settingName)
            {
                Notify(nameof(ColorText));
                Notify(nameof(SwatchColor));
                Notify(nameof(DensityThousandths));
                Notify(nameof(DensityText));
            }
        }
    }
}
