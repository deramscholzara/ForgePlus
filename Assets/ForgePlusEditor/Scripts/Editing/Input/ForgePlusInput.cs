using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.InputSystem;

namespace ForgePlus.ApplicationGeneral
{
    // ForgePlus's input actions (ForgePlusEditor/Input/ForgePlus.inputactions, the project-wide actions asset),
    // which the Input System enables when it starts
    [AutoStaticsCleanup]
    public static partial class ForgePlusInput
    {
        public static class Camera
        {
            public static InputAction Strafe
            {
                get
                {
                    return Find("Camera/Strafe");
                }
            }

            public static InputAction Elevate
            {
                get
                {
                    return Find("Camera/Elevate");
                }
            }

            public static InputAction Advance
            {
                get
                {
                    return Find("Camera/Advance");
                }
            }

            public static InputAction Turbo
            {
                get
                {
                    return Find("Camera/Turbo");
                }
            }

            public static InputAction Look
            {
                get
                {
                    return Find("Camera/Look");
                }
            }

            public static InputAction EnableLook
            {
                get
                {
                    return Find("Camera/EnableLook");
                }
            }

            public static InputAction WheelMove
            {
                get
                {
                    return Find("Camera/WheelMove");
                }
            }

            public static InputAction FrameSelected
            {
                get
                {
                    return Find("Camera/FrameSelected");
                }
            }
        }

        public static class Editing
        {
            public static InputAction Select
            {
                get
                {
                    return Find("Editing/Select");
                }
            }

            public static InputAction LockX
            {
                get
                {
                    return Find("Editing/LockX");
                }
            }

            public static InputAction LockY
            {
                get
                {
                    return Find("Editing/LockY");
                }
            }

            public static InputAction NudgeUp
            {
                get
                {
                    return Find("Editing/NudgeUp");
                }
            }

            public static InputAction NudgeDown
            {
                get
                {
                    return Find("Editing/NudgeDown");
                }
            }

            public static InputAction NudgeLeft
            {
                get
                {
                    return Find("Editing/NudgeLeft");
                }
            }

            public static InputAction NudgeRight
            {
                get
                {
                    return Find("Editing/NudgeRight");
                }
            }

            public static InputAction InvertGridSnap
            {
                get
                {
                    return Find("Editing/InvertGridSnap");
                }
            }

            public static InputAction AlignToSelection
            {
                get
                {
                    return Find("Editing/AlignToSelection");
                }
            }

            public static InputAction TargetOuterLayer
            {
                get
                {
                    return Find("Editing/TargetOuterLayer");
                }
            }

            public static InputAction AlignContiguous
            {
                get
                {
                    return Find("Editing/AlignContiguous");
                }
            }

            public static InputAction KeepRelativeHeight
            {
                get
                {
                    return Find("Editing/KeepRelativeHeight");
                }
            }
        }

        public static class Interface
        {
            public static InputAction ToggleUI
            {
                get
                {
                    return Find("Interface/ToggleUI");
                }
            }

            public static InputAction ToggleMenu
            {
                get
                {
                    return Find("Interface/ToggleMenu");
                }
            }

            // Ctrl+Z (Command+Z), which redoes with Shift
            public static InputAction Undo
            {
                get
                {
                    return Find("Interface/Undo");
                }
            }

            // Ctrl+Y
            public static InputAction Redo
            {
                get
                {
                    return Find("Interface/Redo");
                }
            }
        }

        // Looked up once each, since they're read every frame
        private static Dictionary<string, InputAction> actionsByPath;

        // The action maps they were found in. Reimporting the actions asset (even during a Play session) replaces its maps,
        // leaving the found actions without input state, so they're found again.
        // The asset owns the map, so it isn't disposed (resetting actionsByPath is enough to find it again).
        [NoAutoStaticsCleanup]
        private static InputActionMap firstActionMap;

        private static InputAction Find(string actionPath)
        {
            var asset = InputSystem.actions;
            var currentFirstActionMap = asset.actionMaps.Count > 0 ? asset.actionMaps[0] : null;

            if (actionsByPath == null || firstActionMap != currentFirstActionMap)
            {
                actionsByPath = new Dictionary<string, InputAction>();
                firstActionMap = currentFirstActionMap;
            }

            if (!actionsByPath.TryGetValue(actionPath, out var action))
            {
                action = asset.FindAction(actionPath, throwIfNotFound: true);
                actionsByPath[actionPath] = action;
            }

            return action;
        }
    }
}
