using System;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ForgePlus.EditorTools
{
    /// <summary>
    /// Maintains the global Player Settings Version as "YYYY.MM.DD.N", where N is the number of the
    /// next build that day. Each domain reload moves the date to today (resetting N to 0 when it
    /// changes), each successful build increments N, and every change is copied to each Build Profile
    /// that overrides Player Settings.
    /// </summary>
    [InitializeOnLoad]
    internal class BuildProfileVersionSync : IPostprocessBuildWithReport
    {
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
        private const string VersionPropertyName = "bundleVersion";
        private const string DateFormat = "yyyy.MM.dd";

        // BuildProfile only exposes its Player Settings override internally.
        private static readonly PropertyInfo ProfilePlayerSettingsProperty =
            typeof(BuildProfile).GetProperty("playerSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        static BuildProfileVersionSync()
        {
            if (ProfilePlayerSettingsProperty == null)
            {
                Debug.LogWarning($"{nameof(BuildProfileVersionSync)}: BuildProfile.playerSettings was not found in this Unity version; Build Profile versions will not be synced.");
            }

            // Deferred until the AssetDatabase is usable. Not delayCall, which doesn't run while the Editor is unfocused.
            EditorApplication.update += OnDomainReloaded;
        }

        public int callbackOrder => 0;

        // Only called for successful builds, so failed or cancelled builds don't use up a number.
        public void OnPostprocessBuild(BuildReport report)
        {
            var globalPlayerSettings = GetGlobalPlayerSettings();
            if (globalPlayerSettings == null)
            {
                return;
            }

            var version = globalPlayerSettings.FindProperty(VersionPropertyName).stringValue;
            var lastSeparator = version?.LastIndexOf('.') ?? -1;

            if (lastSeparator >= 0 &&
                int.TryParse(version.Substring(lastSeparator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var buildNumber))
            {
                SetVersion(globalPlayerSettings, version.Substring(0, lastSeparator + 1) + (buildNumber + 1).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                SetVersion(globalPlayerSettings, GetTodaysFirstVersion());
            }
        }

        private static void OnDomainReloaded()
        {
            EditorApplication.update -= OnDomainReloaded;

            var globalPlayerSettings = GetGlobalPlayerSettings();
            if (globalPlayerSettings == null)
            {
                return;
            }

            var version = globalPlayerSettings.FindProperty(VersionPropertyName).stringValue;
            var todaysFirstVersion = GetTodaysFirstVersion();
            var todaysPrefix = todaysFirstVersion.Substring(0, todaysFirstVersion.LastIndexOf('.') + 1);

            if (version == null || !version.StartsWith(todaysPrefix, StringComparison.Ordinal))
            {
                SetVersion(globalPlayerSettings, todaysFirstVersion);
            }
            else
            {
                SyncBuildProfiles(version);
            }
        }

        private static string GetTodaysFirstVersion()
        {
            return DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture) + ".0";
        }

        private static void SetVersion(SerializedObject globalPlayerSettings, string version)
        {
            globalPlayerSettings.FindProperty(VersionPropertyName).stringValue = version;
            globalPlayerSettings.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"{nameof(BuildProfileVersionSync)}: Version set to {version}.");

            SyncBuildProfiles(version);

            // Project settings can't be saved individually; persist them so the day's number survives an Editor restart.
            AssetDatabase.SaveAssets();
        }

        private static SerializedObject GetGlobalPlayerSettings()
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ProjectSettingsPath))
            {
                if (asset is PlayerSettings)
                {
                    return new SerializedObject(asset);
                }
            }

            return null;
        }

        private static void SyncBuildProfiles(string version)
        {
            if (ProfilePlayerSettingsProperty == null)
            {
                return;
            }

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(BuildProfile)}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);

                // Null when the profile doesn't override Player Settings.
                var profilePlayerSettings = profile != null ? ProfilePlayerSettingsProperty.GetValue(profile) as Object : null;
                if (profilePlayerSettings == null)
                {
                    continue;
                }

                var serializedSettings = new SerializedObject(profilePlayerSettings);
                var versionProperty = serializedSettings.FindProperty(VersionPropertyName);
                if (versionProperty == null || versionProperty.stringValue == version)
                {
                    continue;
                }

                versionProperty.stringValue = version;
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);

                Debug.Log($"{nameof(BuildProfileVersionSync)}: Set Version of '{path}' to {version}.");
            }
        }
    }
}
