using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgePlus.ApplicationGeneral
{
    // The Inputs tab's changes to ForgePlusInput's bindings (overrides of the actions asset's own, which stays as it is),
    // kept in PlayerPrefs
    [AutoStaticsCleanup]
    public static partial class InputBindings
    {
        private const string PlayerPrefsKey_BindingOverrides = "Inputs_BindingOverrides";

        // A key or button pressed for a new binding is the binding, not its hotkey, until the frame after it's pressed
        // (or after escape cancels)
        private const int FramesBlockedAfterRebinding = 1;

        // As a binding changes (or they're all reset)
        public static event Action OnChanged;

        private static InputActionRebindingExtensions.RebindingOperation rebinding = null;
        // Negative before any rebinding has ended
        private static int rebindingEndedFrame = -1;

        public static bool IsRebinding
        {
            get
            {
                return rebinding != null ||
                       rebindingEndedFrame >= 0 && Time.frameCount - rebindingEndedFrame <= FramesBlockedAfterRebinding;
            }
        }

        // The saved overrides, in place of any the asset has (ForgePlusInput, as it finds the asset's maps)
        public static void Load(InputActionAsset asset)
        {
            asset.RemoveAllBindingOverrides();

            var json = PlayerPrefs.GetString(PlayerPrefsKey_BindingOverrides, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                asset.LoadBindingOverridesFromJson(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"The saved input bindings could not be loaded, so the defaults are used: {exception.Message}");
                asset.RemoveAllBindingOverrides();
            }
        }

        // Waits for a key or mouse button, which becomes the binding (a composite's part, for one of its parts). Escape
        // cancels. onEnded is called either way.
        public static void Rebind(InputAction action, int bindingIndex, Action onEnded)
        {
            CancelRebinding();

            var wasEnabled = action.enabled;
            action.Disable();

            rebinding = action.PerformInteractiveRebinding(bindingIndex)
                .WithExpectedControlType("Button")
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithControlsHavingToMatchPath("<Mouse>")
                .WithControlsExcluding("<Keyboard>/anyKey")
                // The pointer's press, which is the left button's again
                .WithControlsExcluding("<Pointer>/press")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnApplyBinding((_, path) => action.ApplyBindingOverride(bindingIndex, EitherSidePath(path)))
                .OnComplete(_ => EndRebinding(action, wasEnabled, onEnded, changed: true))
                .OnCancel(_ => EndRebinding(action, wasEnabled, onEnded, changed: false))
                .Start();
        }

        public static void CancelRebinding()
        {
            rebinding?.Cancel();
        }

        public static bool HasOverride(InputAction action, int bindingIndex)
        {
            return !string.IsNullOrEmpty(action.bindings[bindingIndex].overridePath);
        }

        public static void Reset(InputAction action, int bindingIndex)
        {
            action.RemoveBindingOverride(bindingIndex);
            Save(action.actionMap.asset);
        }

        public static void ResetAll()
        {
            CancelRebinding();

            InputSystem.actions.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PlayerPrefsKey_BindingOverrides);
            PlayerPrefs.Save();

            OnChanged?.Invoke();
        }

        private static void EndRebinding(InputAction action, bool wasEnabled, Action onEnded, bool changed)
        {
            rebinding.Dispose();
            rebinding = null;
            rebindingEndedFrame = Time.frameCount;

            if (wasEnabled)
            {
                action.Enable();
            }

            if (changed)
            {
                Save(action.actionMap.asset);
            }

            onEnded?.Invoke();
        }

        private static void Save(InputActionAsset asset)
        {
            PlayerPrefs.SetString(PlayerPrefsKey_BindingOverrides, asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();

            OnChanged?.Invoke();
        }

        // A modifier key pressed on either side binds both sides' (as the defaults do), so Shift works as either Shift
        // (and the Inputs tab compares bindings this way, for their conflicts)
        public static string EitherSidePath(string path)
        {
            switch (path)
            {
                case "<Keyboard>/leftShift":
                case "<Keyboard>/rightShift":
                    return "<Keyboard>/shift";
                case "<Keyboard>/leftCtrl":
                case "<Keyboard>/rightCtrl":
                    return "<Keyboard>/ctrl";
                case "<Keyboard>/leftAlt":
                case "<Keyboard>/rightAlt":
                    return "<Keyboard>/alt";
                case "<Keyboard>/leftMeta":
                case "<Keyboard>/rightMeta":
                    return "<Keyboard>/meta";
                default:
                    return path;
            }
        }
    }
}
