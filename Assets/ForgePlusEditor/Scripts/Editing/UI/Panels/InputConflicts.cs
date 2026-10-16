using ForgePlus.ApplicationGeneral;
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.InputSystem;

namespace ForgePlus.UI
{
    // When an input does what it does: a key can be shared by inputs whose contexts don't overlap (such as a modifier held
    // while dragging a texture, and another held while dragging an object), but not by inputs whose do
    [Flags]
    public enum InputContexts
    {
        None = 0,

        // Whenever the level view has input (hotkeys, camera movement, the pointer's buttons), which overlaps every context
        Always = 1 << 0,

        // While the camera moves
        CameraMove = 1 << 1,

        // Clicking something in the level (to select it)
        WorldClick = 1 << 2,

        // Dragging from empty space to select by rectangle
        RectangleSelect = 1 << 3,

        // Dragging points, lines or polygons (or heights)
        GeometryDrag = 1 << 4,

        // Dragging objects (and sound sources)
        ObjectDrag = 1 << 5,

        // Geometry mode's Draw tool: drawing lines, extruding them, and adding points
        Draw = 1 << 6,

        // Clicking a surface in Textures mode
        TextureClick = 1 << 7,

        // Beginning to drag a surface's texture in Textures mode (what's held then sets up the whole drag)
        TextureDragStart = 1 << 8,

        // While dragging a surface's texture in Textures mode (what's pressed or let go mid-drag changes it)
        TextureDrag = 1 << 9,

        // Nudging the selected surface's texture in Textures mode
        TextureNudge = 1 << 10,

        // Dragging a field's label in the UI to change its value
        ValueDrag = 1 << 11,

        // Any press of Select in the level
        WorldPointer = WorldClick | RectangleSelect | GeometryDrag | ObjectDrag | Draw | TextureClick | TextureDragStart |
                       TextureDrag,
    }

    // Which of the inputs' bindings conflict: two bindings conflict when they're the same key or button (or the same
    // combination), and their inputs' contexts overlap. A lone key also conflicts with a combination it's the last key of
    // (pressing the combination presses it too), unless its input ignores the key while Ctrl or Command is held and the
    // combination has one of them.
    [NoAutoStaticsCleanup]
    public static class InputConflicts
    {
        // An input's name, its action, and how it's used
        public interface IInput
        {
            string Name { get; }
            InputAction Action { get; }
            InputContexts Contexts { get; }

            // Its keys do nothing while Ctrl or Command is held (as shortcuts' keys)
            bool IgnoredWithShortcutModifier { get; }
        }

        // One of an input's bindings: a key or button, or a combination's parts (its modifiers, then its last key)
        public class Binding
        {
            public readonly IInput Input;
            public readonly List<int> PartIndexes;

            public Binding(IInput input, List<int> partIndexes)
            {
                Input = input;
                PartIndexes = partIndexes;
            }
        }

        // Pairs that share a context, whose sharing a key is handled: Cancel Selection Box takes the key from Toggle Menu
        // while selecting by rectangle (ForgePlusUI), and Activate and Obstruct Platform's combinations take their last
        // key from Use Platform (PlatformSimulationHotkeys)
        private static readonly (string, string)[] AllowedPairs =
        {
            ("CancelSelectionBox", "ToggleMenu"),
            ("UsePlatform", "ActivatePlatform"),
            ("UsePlatform", "ObstructPlatform"),
        };

        // Each binding that conflicts, with the bindings it conflicts with
        public static Dictionary<Binding, List<Binding>> Find(IReadOnlyList<Binding> bindings)
        {
            var conflicts = new Dictionary<Binding, List<Binding>>();

            for (var i = 0; i < bindings.Count; i++)
            {
                for (var j = i + 1; j < bindings.Count; j++)
                {
                    var first = bindings[i];
                    var second = bindings[j];

                    if (first.Input == second.Input || !Overlaps(first.Input.Contexts, second.Input.Contexts) ||
                        IsAllowed(first.Input.Name, second.Input.Name) || !Collides(first, second))
                    {
                        continue;
                    }

                    Add(conflicts, first, second);
                    Add(conflicts, second, first);
                }
            }

            return conflicts;
        }

        private static bool Overlaps(InputContexts first, InputContexts second)
        {
            return (first & second) != 0 || (first & InputContexts.Always) != 0 || (second & InputContexts.Always) != 0;
        }

        private static bool IsAllowed(string first, string second)
        {
            foreach (var (allowedFirst, allowedSecond) in AllowedPairs)
            {
                if (first == allowedFirst && second == allowedSecond || first == allowedSecond && second == allowedFirst)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Collides(Binding first, Binding second)
        {
            var firstControls = Controls(first);
            var secondControls = Controls(second);
            if (firstControls == null || secondControls == null)
            {
                return false;
            }

            if (firstControls.Count == secondControls.Count)
            {
                // The same last key, with the same modifiers
                return firstControls[firstControls.Count - 1] == secondControls[secondControls.Count - 1] &&
                       new HashSet<string>(firstControls).SetEquals(secondControls);
            }

            if (firstControls.Count == 1)
            {
                return IsPressedByCombination(first.Input, firstControls[0], secondControls);
            }

            if (secondControls.Count == 1)
            {
                return IsPressedByCombination(second.Input, secondControls[0], firstControls);
            }

            return false;
        }

        // A lone key that's a combination's last key
        private static bool IsPressedByCombination(IInput input, string key, List<string> combination)
        {
            if (combination[combination.Count - 1] != key)
            {
                return false;
            }

            if (input.IgnoredWithShortcutModifier)
            {
                for (var i = 0; i < combination.Count - 1; i++)
                {
                    if (combination[i] == "<Keyboard>/ctrl" || combination[i] == "<Keyboard>/meta")
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Its controls' paths (a combination's last key last), with either side's modifier as both sides' (as rebinding
        // stores them), or null if any part is unbound
        private static List<string> Controls(Binding binding)
        {
            var bindings = binding.Input.Action.bindings;
            var controls = new List<string>();
            string lastKey = null;

            foreach (var partIndex in binding.PartIndexes)
            {
                var path = bindings[partIndex].effectivePath;
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                path = InputBindings.EitherSidePath(path);

                if (binding.PartIndexes.Count > 1 && string.Equals(bindings[partIndex].name, "binding", StringComparison.OrdinalIgnoreCase))
                {
                    lastKey = path;
                }
                else
                {
                    controls.Add(path);
                }
            }

            if (lastKey != null)
            {
                controls.Add(lastKey);
            }

            return controls;
        }

        private static void Add(Dictionary<Binding, List<Binding>> conflicts, Binding binding, Binding other)
        {
            if (!conflicts.TryGetValue(binding, out var list))
            {
                list = new List<Binding>();
                conflicts[binding] = list;
            }

            list.Add(other);
        }
    }
}
